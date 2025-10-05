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

    [Header("Fade System")]
    public ScreenFader screenFader;

    // Events for state changes
    public System.Action<GameState> OnStateChanged;

    // Game data
    private float gameStartTime;
    private bool gamePaused = false;

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

        // Play menu music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }

        // Disable player movement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }
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

        // Play gameplay music with fade in
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic(fadeIn: true);
        }

        // Enable player movement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EnableMovement();
        }
    }

    private void HandlePausedState()
    {
        gamePaused = true;

        // Disable player movement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
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
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
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

        // Start audio fade out
        if (AudioManager.Instance != null)
        {
            StartCoroutine(AudioManager.Instance.FadeOutMusic());
        }

        // Fade out screen
        if (screenFader != null)
        {
            screenFader.FadeIn(1f);
            yield return new WaitForSeconds(1f);
        }

        // Load scene
        SceneManager.LoadScene(sceneName);

        yield return null;
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
            // Position player at spawn point
            if (PlayerController.Instance != null)
            {
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
}