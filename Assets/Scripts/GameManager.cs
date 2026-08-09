using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Loading,
    Playing,
    Paused,
    GameOver,
    Escaped
}

public class GameManager : MonoSingleton<GameManager>
{
    [Header("Game State")]
    public GameState currentState = GameState.MainMenu;
    private GameState _previousState = GameState.MainMenu;

    [Header("Scene References")]
    public string mainMenuSceneName = "MainMenuScene";
    public string gameplaySceneName = "TheBoilerDemo";

    [Header("UI References")]
    public LoadingScreenUI loadingScreenUI;

    [Header("Fade System")]
    public ScreenFader screenFader;

    // Events for state changes
    public System.Action<GameState> OnStateChanged;

    // Game data
    private float gameStartTime;
    private bool gamePaused = false;
    private bool isLoading = false;
    private bool isLoadingFromSave = false;
    private SaveData pendingSaveData = null;

    /// <summary>
    /// Defines valid state transitions for debugging and safety
    /// </summary>
    private static readonly Dictionary<GameState, HashSet<GameState>> ValidTransitions = new Dictionary<GameState, HashSet<GameState>>
{
    {
        GameState.MainMenu, new HashSet<GameState>
        {
            GameState.Loading  // Can only go to Loading from MainMenu
        }
    },
    {
        GameState.Loading, new HashSet<GameState>
        {
            GameState.MainMenu,   // Load failed or cancelled
            GameState.Playing     // Load successful
        }
    },
    {
        GameState.Playing, new HashSet<GameState>
        {
            GameState.Paused,     // Player paused
            GameState.GameOver,   // Timer expired or player died
            GameState.Escaped,    // Player won
            GameState.Loading     // Restarting or returning to menu
        }
    },
    {
        GameState.Paused, new HashSet<GameState>
        {
            GameState.Playing,    // Resumed
            GameState.MainMenu,   // Quit to menu (via Loading)
            GameState.Loading,    // Restarting
            GameState.GameOver    // Timer can expire while paused
        }
    },
    {
        GameState.GameOver, new HashSet<GameState>
        {
            GameState.Loading     // Restart or return to menu
        }
    },
    {
        GameState.Escaped, new HashSet<GameState>
        {
            GameState.Loading     // Restart or return to menu
        }
    }
};

    protected override void Awake()
    {
        base.Awake(); // THIS IS CRITICAL - calls MonoSingleton's Awake first
        GameLog.Log("GameManager Awake called");
    }

    public override void Init()
    {
        GameLog.Log("GameManager Init called");
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Subscribe to Locomotion events (will be set up in DelayedInitialization)
        // LocomotionController might not be ready yet during Awake

        StartCoroutine(DelayedInitialization());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Unsubscribe from timer events
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.OnTimerExpired -= HandleTimerExpired;
        }

        // Unsubscribe from Locomotion events
        if (LocomotionController.Instance != null)
        {
            LocomotionController.Instance.OnLocomotionReady -= OnLocomotionReady;
            LocomotionController.Instance.OnLocomotionFailed -= OnLocomotionFailed;
        }
    }

    private void Update()
    {
        // Handle pause input (customize for VR controllers)
        if (currentState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

    }

    public void SetGameState(GameState newState)
    {
        GameLog.Log($"[GameManager] SetGameState called: {currentState} → {newState}");
        GameLog.Log($"[GameManager] Call stack: {System.Environment.StackTrace}");

        if (currentState == newState) return;

        // Validate state transition
        if (!IsValidTransition(currentState, newState))
        {
            GameLog.LogWarning($"[GameManager] ⚠️ UNUSUAL STATE TRANSITION: {currentState} → {newState}");
            GameLog.LogWarning($"[GameManager] This may indicate a bug. Call stack logged above.");
            // Continue anyway - this is just a warning, not a blocker
        }

        _previousState = currentState; // Store as field, not local variable
        currentState = newState;

        GameLog.Log($"Game State changed from {_previousState} to {newState}");

        // Handle state-specific logic
        switch (newState)
        {
            case GameState.MainMenu:
                HandleMainMenuState();
                break;
            case GameState.Loading:
                HandleLoadingState();
                break;
            case GameState.Playing:
                HandlePlayingState();
                break;
            case GameState.Paused:
                HandlePausedState();
                break;
            case GameState.GameOver:
                HandleGameOverState();
                break;
            case GameState.Escaped:
                HandleEscapedState();
                break;
        }

        // Notify listeners
        OnStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// Validates if a state transition is logical
    /// </summary>
    private bool IsValidTransition(GameState from, GameState to)
    {
        // Same state is always valid (though SetGameState should prevent this)
        if (from == to)
        {
            return true;
        }

        // Check if transition is in the valid transitions map
        if (ValidTransitions.ContainsKey(from))
        {
            return ValidTransitions[from].Contains(to);
        }

        // If we don't have rules defined, log warning but allow it
        GameLog.LogWarning($"[GameManager] No transition rules defined for {from}");
        return true;
    }

    private IEnumerator DelayedInitialization()
    {
        GameLog.Log("DelayedInitialization started");

        // Wait a couple frames for all Awake/Start calls to complete
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // NOW subscribe to LocomotionController events (it should be ready now)
        if (LocomotionController.Instance != null)
        {
            LocomotionController.Instance.OnLocomotionReady += OnLocomotionReady;
            LocomotionController.Instance.OnLocomotionFailed += OnLocomotionFailed;
            GameLog.Log("[GameManager] ✓ Subscribed to LocomotionController events");
        }
        else
        {
            GameLog.LogError("[GameManager] LocomotionController.Instance is still null after delayed init!");
        }

        GameLog.Log("Proceeding with game initialization");

        // Initialize based on current scene
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            currentState = GameState.Loading;
            SetGameState(GameState.MainMenu);
        }
        else if (SceneManager.GetActiveScene().name == gameplaySceneName)
        {
            currentState = GameState.Loading;
            SetGameState(GameState.Playing);
        }
    }

    private void HandleMainMenuState()
    {
        Time.timeScale = 1f;
        gamePaused = false;

        // Hide loading screen
        if (loadingScreenUI != null)
        {
            loadingScreenUI.Hide();
        }

        // Delay menu music slightly to let loading music fade complete
        StartCoroutine(DelayedPlayMenuMusic());

        // Switch to Menu Mode using LocomotionController
        if (LocomotionController.Instance != null)
        {
            LocomotionController.Instance.SwitchToMenuMode();
        }
    }

    private IEnumerator DelayedPlayMenuMusic()
    {
        // Wait for loading music fade to complete
        yield return new WaitForSeconds(1f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }
    }

    private void HandleLoadingState()
    {
        Time.timeScale = 1f;
        isLoading = true;

        // Play loading music with fade in
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLoadingMusic(fadeIn: false);
        }

        // Show loading screen and reset progress to 0%
        if (loadingScreenUI != null)
        {
            loadingScreenUI.UpdateProgress(0f); // Reset to 0%
            loadingScreenUI.Show();
        }

        // LocomotionController handles disabling movement internally
        // No need to call PlayerController.DisableMovement() anymore

        // Pause timer during loading
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.PauseTimer();
        }
    }

    private void HandlePlayingState()
    {
        Time.timeScale = 1f;
        gamePaused = false;
        isLoading = false;

        if (gameStartTime == 0f)
        {
            gameStartTime = Time.time;
        }

        // Hide loading screen
        if (loadingScreenUI != null)
        {
            loadingScreenUI.Hide();
        }

        // Delay gameplay music slightly to let loading music fade complete
        StartCoroutine(DelayedPlayGameplayMusic());

        // Check if we're resuming from pause or starting fresh
        if (_previousState == GameState.Paused)
        {
            // RESUME FROM PAUSE - Just re-enable locomotion (with fallback)
            if (LocomotionController.Instance != null)
            {
                GameLog.Log("[GameManager] Resuming from pause - re-enabling locomotion");

                LocomotionController.Instance.SwitchToGameplayMode(
                    onSuccess: () =>
                    {
                        GameLog.Log("[GameManager] ✓ Quick resume successful - Starting timer");
                        InitializeGameTimer();
                    },
                    onFailure: () =>
                    {
                        GameLog.LogError("[GameManager] ✗ Resume failed (even with fallback)!");
                        ReturnToMainMenu();
                    }
                );
            }
            else
            {
                GameLog.LogError("[GameManager] LocomotionController.Instance is null!");
            }
        }
        else
        {
            // FRESH START - Full initialization with validation
            if (LocomotionController.Instance != null)
            {
                GameLog.Log("[GameManager] Starting fresh game - initializing locomotion");

                LocomotionController.Instance.InitializeForGameplay(
                    onSuccess: () =>
                    {
                        GameLog.Log("[GameManager] ✓ Locomotion ready - Starting timer");
                        InitializeGameTimer();
                    },
                    onFailure: () =>
                    {
                        GameLog.LogError("[GameManager] ✗ Locomotion initialization failed!");
                        // Return to menu on failure
                        ReturnToMainMenu();
                    }
                );
            }
            else
            {
                GameLog.LogError("[GameManager] LocomotionController.Instance is null!");
            }
        }
    }

    private IEnumerator DelayedPlayGameplayMusic()
    {
        // Wait for loading music fade to complete
        yield return new WaitForSeconds(1f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic(fadeIn: true);
        }
    }

    /// <summary>
    /// Initialize GameTimer with settings - Called after Locomotion System is ready
    /// </summary>
    private void InitializeGameTimer()
    {
        GameLog.Log($"[GameManager] InitializeGameTimer called - isLoadingFromSave: {isLoadingFromSave}");

        if (GameTimer.Instance != null)
        {
            GameLog.Log("[GameManager] GameTimer.Instance found, subscribing to OnTimerExpired event");

            // Subscribe to timer expired event (only once)
            GameTimer.Instance.OnTimerExpired -= HandleTimerExpired;
            GameTimer.Instance.OnTimerExpired += HandleTimerExpired;

            GameLog.Log("[GameManager] Successfully subscribed to OnTimerExpired event");

            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ApplyDifficultySettings();
                GameLog.Log("[GameManager] HealthEnergyManager difficulty settings re-synced after gameplay scene load");
            }

            // CRITICAL: Only apply settings if NOT loading from save
            if (!isLoadingFromSave && SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ApplyTimerSettings(skipIfRunning: isLoadingFromSave);
                GameLog.Log("[GameManager] Timer settings applied on gameplay start");
            }
            else if (isLoadingFromSave)
            {
                GameLog.Log("[GameManager] Skipping timer settings - loading from save (timer already restored)");
                // CRITICAL: Clear the flag NOW (after we've used it)
                isLoadingFromSave = false;
            }

            // Resume the timer
            GameTimer.Instance.ResumeTimer();
            GameLog.Log("[GameManager] Timer resumed");
        }
    }

    private void HandlePausedState()
    {
        gamePaused = true;

        // Switch to Menu Mode (disables locomotion)
        if (LocomotionController.Instance != null)
        {
            LocomotionController.Instance.SwitchToMenuMode();
        }

        // Pause the timer
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.PauseTimer();
            GameLog.Log("[GameManager] Timer paused");
        }
    }

    private void HandleGameOverState()
    {
        Time.timeScale = 1f;

        // Switch to Menu Mode so player can interact with Game Over UI
        if (LocomotionController.Instance != null)
        {
            LocomotionController.Instance.SwitchToMenuMode();
        }

        // Stop the timer
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StopTimer();
            GameLog.Log("[GameManager] Timer stopped - Game Over");
        }

        GameLog.Log("GAME OVER! Timer expired or player died.");
    }

    private void HandleEscapedState()
    {
        Time.timeScale = 1f;

        // Switch to Menu Mode
        if (LocomotionController.Instance != null)
        {
            LocomotionController.Instance.SwitchToMenuMode();
        }

        // Stop the timer and get final time
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StopTimer();

            string finalTime = GameTimer.Instance.GetFormattedElapsedTime();
            GameLog.Log($"Level completed! Time: {finalTime}");

            // TODO: Show Escaped UI with completion time
        }
        else
        {
            float completionTime = Time.time - gameStartTime;
            GameLog.Log($"Level completed in {completionTime:F2} seconds!");
        }
    }

    /// <summary>
    /// Called when the GameTimer countdown reaches zero
    /// </summary>
    private void HandleTimerExpired()
    {
        GameLog.Log("[GameManager] ⚠️ HandleTimerExpired() METHOD CALLED ⚠️");
        GameLog.Log("[GameManager] Timer expired event received - triggering Game Over");
        GameOver();
    }

    /// <summary>
    /// Called when Locomotion System successfully initializes
    /// </summary>
    private void OnLocomotionReady()
    {
        GameLog.Log("[GameManager] Locomotion System ready event received");
    }

    /// <summary>
    /// Called when Locomotion System fails to initialize
    /// </summary>
    private void OnLocomotionFailed()
    {
        GameLog.LogError("[GameManager] Locomotion System failed event received");
    }

    // Public methods for scene transitions
    public void StartGame()
    {
        // Reset all game systems for new game
        ResetGameSystems();

        StartCoroutine(LoadSceneWithFade(gameplaySceneName, GameState.Playing));
    }

    public void RestartGame()
    {
        gameStartTime = 0f;

        // Reset all game systems for new game
        ResetGameSystems();

        StartCoroutine(LoadSceneWithFade(gameplaySceneName, GameState.Playing));
    }

    /// <summary>
    /// Reset all game systems to starting values (for new game or restart)
    /// </summary>
    private void ResetGameSystems()
    {
        GameLog.Log("[GameManager] Resetting all game systems for new game");

        // Reset Health/Energy
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.ResetToStartingValues();
        }

        // Reset Timer
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.ResetTimer();
            GameLog.Log("[GameManager] Timer reset");
        }

        // CRITICAL: Clear save data AND delete Continue Slot (Slot 0)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearCurrentSaveData();

            // Delete old Continue Slot for fresh start
            if (SaveManager.Instance.SaveSlotExists(0))
            {
                SaveManager.Instance.DeleteSave(0);
                GameLog.Log("[GameManager] Deleted old Continue Slot for new game");
            }
        }

    }

    /// <summary>
    /// Load a saved game properly through GameManager's scene system
    /// </summary>
    public void LoadSavedGame(SaveData saveData)
    {
        GameLog.Log($"[GameManager] Loading saved game from scene: {saveData.currentSceneName}");

        // Store the save data to restore after scene loads
        pendingSaveData = saveData;
        isLoadingFromSave = true;

        // Use proper scene loading
        StartCoroutine(LoadSceneWithFade(saveData.currentSceneName, GameState.Playing));
    }

    public void ReturnToMainMenu()
    {
        gameStartTime = 0f;

        // Stop timer when returning to main menu
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StopTimer();
        }

        StartCoroutine(LoadSceneWithFade(mainMenuSceneName, GameState.MainMenu));
    }

    public void PlayerEscaped()
    {
        SetGameState(GameState.Escaped);
    }

    public void GameOver()
    {
        SetGameState(GameState.GameOver);
    }

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }

    private IEnumerator LoadSceneWithFade(string sceneName, GameState targetState)
    {
        SetGameState(GameState.Loading);

        // CRITICAL: Disable locomotion immediately via LocomotionController
        if (LocomotionController.Instance != null)
        {
            LocomotionController.Instance.SwitchToMenuMode();
        }

        // Fade out screen
        if (screenFader != null)
        {
            screenFader.FadeIn(1f);
            yield return new WaitForSeconds(1f);
        }

        // Track loading start time
        float loadingStartTime = Time.realtimeSinceStartup;
        float minimumLoadingTime = 3f; // Minimum time to show loading screen

        // Start async scene loading
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Prevent the scene from activating immediately
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is almost loaded (0.9 = 90%)
        while (asyncLoad.progress < 0.9f)
        {
            // use asyncLoad.progress here for progress tracking
            // Progress goes from 0 to 0.9 (Unity reserves the last 10% for activation)
            float loadingProgress = asyncLoad.progress / 0.9f; // Normalize to 0-1

            // Update loading UI
            if (loadingScreenUI != null)
            {
                loadingScreenUI.UpdateProgress(loadingProgress);
            }

            GameLog.Log($"Loading progress: {loadingProgress * 100}%");

            yield return null;
        }

        // Scene is loaded to 90%, continue showing progress smoothly to 100%
        float fakeProgress = 0.9f;
        while (fakeProgress < 1f)
        {
            fakeProgress += Time.deltaTime * 0.05f; // Smooth progress increment
            fakeProgress = Mathf.Clamp01(fakeProgress);

            if (loadingScreenUI != null)
            {
                loadingScreenUI.UpdateProgress(fakeProgress);
            }

            yield return null;
        }

        // Make sure minimum loading time has passed
        float elapsedTime = Time.realtimeSinceStartup - loadingStartTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
        }

        // Start audio fade out
        if (AudioManager.Instance != null)
        {
            StartCoroutine(AudioManager.Instance.FadeOutMusic());
        }

        // Scene is loaded, now we can activate it
        asyncLoad.allowSceneActivation = true;

        // Wait for scene activation to complete
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        GameLog.Log($"Scene {sceneName} loaded successfully");
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        // Update references for the new scene
        if (screenFader == null)
        {
            screenFader = FindObjectOfType<ScreenFader>();
        }

        // Update LocomotionController's XR Origin reference
        if (LocomotionController.Instance != null)
        {
            LocomotionController.Instance.UpdateXROriginReference();
        }

        // Update PlayerController's XR Origin reference (still needed for positioning)
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.UpdateXROriginReference();
        }

        // Update FootstepAudioManager's XR Origin reference
        if (FootstepAudioManager.Instance != null)
        {
            FootstepAudioManager.Instance.UpdateXROriginReference();
        }

        // Handle scene-specific logic
        if (scene.name == mainMenuSceneName)
        {
            SetGameState(GameState.MainMenu);
        }
        else if (scene.name == gameplaySceneName)
        {
            // Check if we're loading from a save
            if (isLoadingFromSave && pendingSaveData != null)
            {
                GameLog.Log("[GameManager] Restoring save data after scene load");
                StartCoroutine(RestoreSaveDataAfterSceneLoad(pendingSaveData));
            }
            else
            {
                // Normal new game start
                if (PlayerController.Instance != null)
                {
                    // Position player at spawn point
                    PlayerController.Instance.PositionAtSpawnPoint();
                }

                // Apply difficulty-based consumable scaling now that the gameplay
                // scene (and ConsumableManager within it) has fully loaded
                if (ConsumableManager.Instance != null)
                {
                    ConsumableManager.Instance.ResetForNewGame();
                    if (SettingsManager.Instance != null)
                    {
                        ConsumableManager.Instance.ApplyDifficultyCount(SettingsManager.Instance.GetConsumableObjectCount());
                    }
                }

                SetGameState(GameState.Playing);
            }
        }

        // Fade in screen
        if (screenFader != null)
        {
            screenFader.FadeOut(1f);
        }
    }

    /// <summary>
    /// Restore save data after scene has loaded
    /// </summary>
    private IEnumerator RestoreSaveDataAfterSceneLoad(SaveData data)
    {
        GameLog.Log("[GameManager] Starting save data restoration");

        // Wait for scene to fully initialize. These have to happen before the try block below:
        // a yield return cannot appear inside a try block that has a catch clause (only inside
        // a try-finally), so everything that can throw is restored to sequential code afterward.
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        try
        {
            // Restore player position FIRST (before locomotion initializes)
            if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
            {
                PlayerController.Instance.XROrigin.transform.position = data.playerPosition;
                PlayerController.Instance.XROrigin.transform.rotation = data.playerRotation;
                GameLog.Log($"[GameManager] Player position restored to {data.playerPosition}");
            }

            // Restore health/energy
            if (HealthEnergyManager.Instance != null)
            {
                HealthEnergyManager.Instance.SetHealth(data.currentHealth);
                HealthEnergyManager.Instance.SetEnergy(data.currentEnergy);

                // CRITICAL: Reinitialize movement tracking after player position is restored
                HealthEnergyManager.Instance.InitializeMovementTracking();

                GameLog.Log($"[GameManager] Health/Energy restored: {data.currentHealth}/{data.currentEnergy}");
            }

            // Restore GameTimer state BEFORE ISaveable objects (special handling for DontDestroyOnLoad singleton)
            if (GameTimer.Instance != null)
            {
                GameTimer.Instance.LoadState(data);
                GameLog.Log($"[GameManager] Timer restored: {data.timerRemainingTime}s remaining, Mode: {(GameTimer.TimerMode)data.timerMode}");
            }

            // Restore ClueManager state (special handling for DontDestroyOnLoad singleton)
            if (ClueManager.Instance != null)
            {
                ClueManager.Instance.LoadState(data);
                GameLog.Log($"[GameManager] ClueManager restored: {data.discoveredClueIDs.Count} clues discovered");
            }

            // Restore all ISaveable objects
            ISaveable[] saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
            foreach (ISaveable saveable in saveableObjects)
            {
                saveable.LoadState(data);
            }

            GameLog.Log("[GameManager] Save data restoration complete");
        }
        catch (System.Exception e)
        {
            GameLog.LogError("[GameManager] Save data could not be fully restored - some progress may be reset. " + e);
        }

        // Always reach a playable state and clear pending save data, whether restoration above
        // succeeded or not - the player must never be left stuck on a black/loading screen.
        SetGameState(GameState.Playing);

        // DON'T clear flags here - InitializeGameTimer will clear them after checking
        // isLoadingFromSave = false;  // REMOVED
        pendingSaveData = null; // Clear this one since it's safe
    }

    // Utility methods
    public float GetGameTime()
    {
        return gameStartTime > 0 ? Time.time - gameStartTime : 0f;
    }

    public bool IsGamePaused()
    {
        return gamePaused;
    }

    public bool IsInGameplay()
    {
        return currentState == GameState.Playing || currentState == GameState.Paused;
    }

    public bool IsLoading()
    {
        return isLoading;
    }
}