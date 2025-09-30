using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

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

    [Header("XR References")]
    public GameObject xrOrigin;

    [Header("Fade System")]
    public ScreenFader screenFader;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;

    // Events for state changes
    public System.Action<GameState> OnStateChanged;

    // Game data
    private float gameStartTime;
    private bool gamePaused = false;

    public override void Init()
    {
        // Make sure GameManager persists between scenes
        DontDestroyOnLoad(gameObject);

        // Initialize based on current scene
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            SetGameState(GameState.MainMenu);
        }
        else if (SceneManager.GetActiveScene().name == gameplaySceneName)
        {
            SetGameState(GameState.Playing);
        }

        // Subscribe to scene loaded events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // Handle pause input (you can customize this for VR controllers)
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

    private void HandleMainMenuState()
    {
        Time.timeScale = 1f;
        gamePaused = false;
        PlayMusic(menuMusic);
        // Movement will be disabled in OnSceneLoaded after XR Origin is properly set
    }

    private void HandleLoadingState()
    {
        Time.timeScale = 1f;
    }

    private void HandlePlayingState()
    {
        Time.timeScale = 1f; 
        gamePaused = false;

        if (gameStartTime == 0f)
        {
            gameStartTime = Time.time;
        }

        PlayMusic(gameplayMusic);
        EnablePlayerMovement();
    }

    private void HandlePausedState()
    {
        // Don't use Time.timeScale = 0 for VR
        // Time.timeScale = 0f; // REMOVE or COMMENT OUT this line
        gamePaused = true;
        DisablePlayerMovement();
    }

    private void HandleGameOverState()
    {
        Time.timeScale = 1f;
        DisablePlayerMovement();
    }

    private void HandleEscapedState()
    {
        Time.timeScale = 1f;
        DisablePlayerMovement();

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

        // Fade out
        if (screenFader != null)
        {
            screenFader.FadeIn(1f);
            yield return new WaitForSeconds(1f);
        }

        // Load scene
        SceneManager.LoadScene(sceneName);

        // The OnSceneLoaded callback will handle setting the target state
        yield return null;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        // Update XR Origin reference for the new scene
        if (xrOrigin == null)
        {
            XROrigin xrOriginComponent = FindObjectOfType<XROrigin>();
            if (xrOriginComponent != null)
            {
                xrOrigin = xrOriginComponent.gameObject;
            }
        }

        // Update ScreenFader reference for the new scene
        if (screenFader == null)
        {
            screenFader = FindObjectOfType<ScreenFader>();
        }

        // Handle player positioning based on scene
        if (scene.name == mainMenuSceneName)
        {
            SetGameState(GameState.MainMenu);
            // Now that XR Origin is set, disable movement for main menu
            DisablePlayerMovement();
        }
        else if (scene.name == gameplaySceneName)
        {
            // Position player at spawn point for gameplay
            PositionPlayerAtSpawnPoint();
            SetGameState(GameState.Playing);
        }

        // Fade in
        if (screenFader != null)
        {
            screenFader.FadeOut(1f);
        }
    }

    private void PositionPlayerAtSpawnPoint()
    {
        // Find PlayerSpawnHandler in the scene
        PlayerSpawnHandler spawnHandler = FindObjectOfType<PlayerSpawnHandler>();
        if (spawnHandler != null && xrOrigin != null)
        {
            // Access the public fields from PlayerSpawnHandler
            if (spawnHandler.xrOrigin != null && spawnHandler.startPosition != null)
            {
                xrOrigin.transform.position = spawnHandler.startPosition.position;
                xrOrigin.transform.rotation = spawnHandler.startPosition.rotation;
                Debug.Log("Player positioned at spawn point");
            }
            else
            {
                Debug.LogWarning("PlayerSpawnHandler found but xrOrigin or startPosition is null");
            }
        }
        else
        {
            Debug.LogWarning("PlayerSpawnHandler not found in scene or xrOrigin is null");
        }
    }

    private void DisablePlayerMovement()
{
    if (xrOrigin == null) return;
    
    // Disable locomotion components
    var teleport = xrOrigin.GetComponent<TeleportationProvider>();
    if (teleport != null) teleport.enabled = false;
    
    var continuousMove = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
    if (continuousMove != null) continuousMove.enabled = false;
    
    var snapTurn = xrOrigin.GetComponent<ActionBasedSnapTurnProvider>();
    if (snapTurn != null) snapTurn.enabled = false;
    
    var continuousTurn = xrOrigin.GetComponent<ActionBasedContinuousTurnProvider>();
    if (continuousTurn != null) continuousTurn.enabled = false;
    
    Debug.Log("Player movement disabled");
}

    private void EnablePlayerMovement()
    {
        if (xrOrigin == null)
        {
            Debug.LogWarning("XR Origin is null when trying to enable movement!");
            return;
        }

        StartCoroutine(EnablePlayerMovementCoroutine());
    }

    private IEnumerator EnablePlayerMovementCoroutine()
    {
        // Small delay to ensure state changes have propagated
        yield return new WaitForSeconds(0.1f);

        // Enable locomotion components with verification
        var teleport = xrOrigin.GetComponent<TeleportationProvider>();
        if (teleport != null)
        {
            teleport.enabled = true;
            Debug.Log($"Teleport enabled: {teleport.enabled}");
        }

        var continuousMove = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
        if (continuousMove != null)
        {
            continuousMove.enabled = true;
            Debug.Log($"Continuous move enabled: {continuousMove.enabled}");
        }

        var snapTurn = xrOrigin.GetComponent<ActionBasedSnapTurnProvider>();
        if (snapTurn != null)
        {
            snapTurn.enabled = true;
            Debug.Log($"Snap turn enabled: {snapTurn.enabled}");
        }

        var continuousTurn = xrOrigin.GetComponent<ActionBasedContinuousTurnProvider>();
        if (continuousTurn != null)
        {
            continuousTurn.enabled = true;
            Debug.Log($"Continuous turn enabled: {continuousTurn.enabled}");
        }

        Debug.Log("Player movement enable sequence completed");
    }

    private void PlayMusic(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
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
}