using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuCanvas;
    public GameObject pauseMenuPanel;

    [Header("Button References")]
    public GameObject resumeButton;
    public GameObject saveGameButton;  // save game button
    public GameObject loadGameButton; // load game button
    public GameObject mainMenuButton;
    public GameObject exitGameButton;

    [Header("Save/Load UI")]  // save/load menu UI reference
    public SaveLoadMenuUI saveLoadMenuUI;

    [Header("Exit Confirmation")]
    public GameObject exitConfirmationPanel;
    public GameObject mainMenuConfirmationTextObject; // GameObject with pre-styled text for Main Menu
    public GameObject exitGameConfirmationTextObject; // GameObject with pre-styled text for Exit Game
    public GameObject confirmYesButton;
    public GameObject confirmCancelButton;

    [Header("Input")]
    public InputActionProperty menuButtonAction; // For XR controller input

    [Header("Debug/Setup")]
    [SerializeField] private bool showForSetup = false; // Toggle this to preview UI in Editor

    private bool _isMenuButtonPressed = false;
    private Coroutine _resumeCoroutine;
    private System.Action _pendingConfirmAction; // Store which action to execute if confirmed

    private void OnValidate() // Runs in Editor when values change
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) // Only in edit mode, not during play
        {
            if (exitConfirmationPanel != null)
                exitConfirmationPanel.SetActive(showForSetup);
            if (mainMenuConfirmationTextObject != null)
                mainMenuConfirmationTextObject.SetActive(showForSetup && exitConfirmationPanel.activeSelf);
            if (exitGameConfirmationTextObject != null)
                exitGameConfirmationTextObject.SetActive(showForSetup && exitConfirmationPanel.activeSelf);
        }
#endif
    }

    private void Start()
    {
        // Hide pause menu at start
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }

        // Hide exit confirmation at start
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }

        // Hide save/load panels at start
        if (saveLoadMenuUI != null)
        {
            saveLoadMenuUI.HideAllPanels();
        }

        // Subscribe to GameManager state changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        // Set up XR input for menu button
        if (menuButtonAction.action != null)
        {
            menuButtonAction.action.Enable();
            menuButtonAction.action.performed += OnMenuButtonPressed;
            menuButtonAction.action.canceled += OnMenuButtonReleased;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        // Clean up input actions
        if (menuButtonAction.action != null)
        {
            menuButtonAction.action.performed -= OnMenuButtonPressed;
            menuButtonAction.action.canceled -= OnMenuButtonReleased;
        }
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext context)
    {
        if (!_isMenuButtonPressed && GameManager.Instance != null)
        {
            _isMenuButtonPressed = true;

            // Only allow pausing during gameplay
            if (GameManager.Instance.currentState == GameState.Playing)
            {
                GameManager.Instance.TogglePause();
            }
            else if (GameManager.Instance.currentState == GameState.Paused)
            {
                // If confirmation panel is showing, don't unpause
                if (exitConfirmationPanel != null && exitConfirmationPanel.activeSelf)
                {
                    return; // Don't unpause if confirmation dialog is open
                }

                if (saveLoadMenuUI != null && (saveLoadMenuUI.transform.Find("SavePanel")?.gameObject.activeSelf == true ||
                                               saveLoadMenuUI.transform.Find("LoadPanel")?.gameObject.activeSelf == true))
                {
                    return;
                }

                GameManager.Instance.TogglePause();
            }
        }
    }

    private void OnMenuButtonReleased(InputAction.CallbackContext context)
    {
        _isMenuButtonPressed = false;
    }

    private void OnGameStateChanged(GameState newState)
    {
        // Find and notify the validator
        var validator = FindObjectOfType<XRLocomotionValidator>();

        switch (newState)
        {
            case GameState.Paused:
                ShowPauseMenu();
                PauseGameTimer();
                break;
            case GameState.Playing:
                HidePauseMenu();
                ResumeGameTimer();
                HideExitConfirmation(); // Hide confirmation if game resumes
                // Hide save/load panels when resuming
                if (saveLoadMenuUI != null)
                {
                    saveLoadMenuUI.HideAllPanels();
                }
                break;
            case GameState.Loading:
            case GameState.MainMenu:
            case GameState.Escaped:
            case GameState.GameOver:
                HidePauseMenu();
                HideExitConfirmation();
                break;
        }
    }

    private void ShowPauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);

            // Show main pause menu panel, hide save/load panels
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }

            if (saveLoadMenuUI != null)
            {
                saveLoadMenuUI.HideAllPanels();
            }
        }

        UIAudioManager.Instance?.PlayMenuOpen();
   
    }

    private void HidePauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
            UIAudioManager.Instance?.PlayMenuClose();
        }
    }

    private void PauseGameTimer()
    {
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.PauseTimer();
        }
    }

    private void ResumeGameTimer()
    {
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.ResumeTimer();
        }
    }

    #region Button Click Handlers

    // Public methods for button clicks
    public void OnResumeButtonClicked()
    {
        Debug.Log("Resume button clicked");

        // Hide any open confirmation dialog
        HideExitConfirmation();

        // Cancel any existing resume coroutine
        if (_resumeCoroutine != null)
        {
            StopCoroutine(_resumeCoroutine);
        }

        // Start the resume coroutine
        _resumeCoroutine = StartCoroutine(ResumeGameCoroutine());
    }

    private IEnumerator ResumeGameCoroutine()
    {
        // First hide the pause menu immediately
        HidePauseMenu();

        // Wait one frame for UI to update
        yield return null;

        // Then toggle pause state
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Paused)
        {
            GameManager.Instance.TogglePause();
        }

        // Wait another frame to ensure state change is processed
        yield return null;

        // Force re-enable movement as a safety check
        if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            var continuousMove = PlayerController.Instance.XROrigin.GetComponent<ActionBasedContinuousMoveProvider>();
            if (continuousMove != null)
            {
                // Disable then re-enable to force refresh
                continuousMove.enabled = false;
                yield return null;
                continuousMove.enabled = true;
                Debug.Log("Force re-enabled continuous move provider");
            }
        }

        _resumeCoroutine = null;
    }

    // save Game Button
    public void OnSaveGameButtonClicked()
    {
        Debug.Log("Save Game button clicked");

        if (saveLoadMenuUI != null)
        {
            // Hide main pause menu, show save panel
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            saveLoadMenuUI.ShowSavePanel();
        }
        else
        {
            Debug.LogWarning("SaveLoadMenuUI reference is missing!");
        }
    }

    // load Game Button
    public void OnLoadGameButtonClicked()
    {
        Debug.Log("Load Game button clicked");

        if (saveLoadMenuUI != null)
        {
            // Hide main pause menu, show load panel
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            saveLoadMenuUI.ShowLoadPanel();
        }
        else
        {
            Debug.LogWarning("SaveLoadMenuUI reference is missing!");
        }
    }

    // back from Save/Load to Pause Menu
    public void OnBackToPauseMenu()
    {
        Debug.Log("Back to pause menu");

        if (saveLoadMenuUI != null)
        {
            saveLoadMenuUI.HideAllPanels();
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
    }

    // Modified to show confirmation instead of direct action
    public void OnMainMenuButtonClicked()
    {
        Debug.Log("Main Menu button clicked - showing confirmation");
        ShowExitConfirmation(mainMenuConfirmationTextObject, () => ExecuteReturnToMainMenu());
    }

    // Modified to show confirmation instead of direct action
    public void OnExitGameButtonClicked()
    {
        Debug.Log("Exit Game button clicked - showing confirmation");
        ShowExitConfirmation(exitGameConfirmationTextObject, () => ExecuteExitGame());
    }

    #endregion

    #region Confirmation Dialogs

    // Modified show confirmation method
    private void ShowExitConfirmation(GameObject textObjectToShow, System.Action confirmAction)
    {
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(true);

            // Hide all confirmation texts first
            if (mainMenuConfirmationTextObject != null)
                mainMenuConfirmationTextObject.SetActive(false);
            if (exitGameConfirmationTextObject != null)
                exitGameConfirmationTextObject.SetActive(false);

            // Show the appropriate text
            if (textObjectToShow != null)
                textObjectToShow.SetActive(true);

            _pendingConfirmAction = confirmAction;
        }
        else
        {
            Debug.LogWarning("ExitConfirmationPanel is not assigned in PauseMenuManager!");
            // If no confirmation panel, execute action directly
            confirmAction?.Invoke();
        }
    }

    // Called by "Yes" button in confirmation dialog
    public void OnConfirmYes()
    {
        Debug.Log("Action confirmed by user");
        UIAudioManager.Instance?.PlayConfirm();
        HideExitConfirmation();
        _pendingConfirmAction?.Invoke();
        _pendingConfirmAction = null;
    }

    // Called by "Cancel" button in confirmation dialog
    public void OnConfirmCancel()
    {
        Debug.Log("Action cancelled by user");
        UIAudioManager.Instance?.PlayCancel();
        HideExitConfirmation();
        _pendingConfirmAction = null;
    }

    private void HideExitConfirmation()
    {
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }
    }

    #endregion

    #region Execute Actions

    // Actual execution methods (called after confirmation)
    private void ExecuteReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu from pause menu");

        if (GameManager.Instance != null)
        {
            // First unpause the game (to reset Time.timeScale if you're using it)
            if (GameManager.Instance.currentState == GameState.Paused)
            {
                Time.timeScale = 1f;
            }

            GameManager.Instance.ReturnToMainMenu();
        }
    }

    private void ExecuteExitGame()
    {
        Debug.Log("Exiting game from pause menu");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}