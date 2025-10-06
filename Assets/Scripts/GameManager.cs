using System.Collections;
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

    protected override void Awake()
    {
        base.Awake(); // THIS IS CRITICAL - calls MonoSingleton's Awake first
        Debug.Log("GameManager Awake called");
    }

    public override void Init()
    {
        Debug.Log("GameManager Init called");
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(DelayedInitialization());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;

        Debug.Log($"Game State changed from {previousState} to {newState}");

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

    private IEnumerator DelayedInitialization()
    {
        Debug.Log("DelayedInitialization started");

        // Wait a couple frames for all Awake/Start calls to complete
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Debug.Log("Proceeding with game initialization");

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

        if (InputModeManager.Instance != null)
        {
            InputModeManager.Instance.SwitchToMenuMode();
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

        // Disable player movement during loading
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
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

        // Enable player movement
        if (InputModeManager.Instance != null)
        {
            InputModeManager.Instance.SwitchToGameplayMode();
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

    private void HandlePausedState()
    {
        gamePaused = true;

        // Disable player movement
        if (InputModeManager.Instance != null)
        {
            InputModeManager.Instance.SwitchToMenuMode();
        }
    }

    private void HandleGameOverState()
    {
        Time.timeScale = 1f;

        // Disable player movement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }
    }

    private void HandleEscapedState()
    {
        Time.timeScale = 1f;

        // Disable player movement
        if (InputModeManager.Instance != null)
        {
            InputModeManager.Instance.SwitchToMenuMode();
        }

        float completionTime = Time.time - gameStartTime;
        Debug.Log($"Level completed in {completionTime:F2} seconds!");
    }

    // Public methods for scene transitions
    public void StartGame()
    {
        StartCoroutine(LoadSceneWithFade(gameplaySceneName, GameState.Playing));
    }

    public void RestartGame()
    {
        gameStartTime = 0f;
        StartCoroutine(LoadSceneWithFade(gameplaySceneName, GameState.Playing));
    }

    public void ReturnToMainMenu()
    {
        gameStartTime = 0f;
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

        // CRITICAL: Disable movement immediately before any delays
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }

        // Disable all input action maps during transition
        if (InputModeManager.Instance != null && InputModeManager.Instance.inputActionAsset != null)
        {
            InputModeManager.Instance.inputActionAsset.Disable();
        }

        // Fade out screen
        if (screenFader != null)
        {
            screenFader.FadeIn(1f);
            yield return new WaitForSeconds(1f);
        }

        // Track loading start time
        float loadingStartTime = Time.realtimeSinceStartup;
        float minimumLoadingTime = 5f; // Minimum time to show loading screen

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

            Debug.Log($"Loading progress: {loadingProgress * 100}%");

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

        // Ensure minimum loading time has passed
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

        Debug.Log($"Scene {sceneName} loaded successfully");
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        // Update references for the new scene
        if (screenFader == null)
        {
            screenFader = FindObjectOfType<ScreenFader>();
        }

        // Update PlayerController's XR Origin reference
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.UpdateXROriginReference();
        }

        // Handle scene-specific logic
        if (scene.name == mainMenuSceneName)
        {
            SetGameState(GameState.MainMenu);
        }
        else if (scene.name == gameplaySceneName)
        {
            if (PlayerController.Instance != null)
            {
                // CRITICAL: Update XR Origin reference first
                PlayerController.Instance.UpdateXROriginReference();
                // Position player at spawn point
                PlayerController.Instance.PositionAtSpawnPoint();
            }

            SetGameState(GameState.Playing);
        }

        // Fade in screen
        if (screenFader != null)
        {
            screenFader.FadeOut(1f);
        }
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