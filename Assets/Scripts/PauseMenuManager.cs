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
    public GameObject settingsButton; // settings button
    public GameObject hintsButton; // hints menu button
    public GameObject mainMenuButton;
    public GameObject exitGameButton;

    [Header("Save/Load UI")]  // save/load menu UI reference
    public SaveLoadMenuUI saveLoadMenuUI;

    [Header("Settings UI")]
    public SettingsMenuUI settingsMenuUI;

    [Header("Hints UI")]
    public HintsMenuUI hintsMenuUI;

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

        // Hide settings menu at start (disable entire canvas to prevent ray blocking)
        if (settingsMenuUI != null)
        {
            Transform canvas = settingsMenuUI.transform.parent;
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        // Hide hints menu at start (disable entire canvas to prevent ray blocking)
        if (hintsMenuUI != null)
        {
            Transform canvas = hintsMenuUI.transform.parent;
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
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
            menuButtonAction.action.Disable();
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

                // If pause menu panel is hidden, that means a sub-panel is showing (save/load/settings/hints)
                if (pauseMenuPanel != null && !pauseMenuPanel.activeSelf)
                {
                    return; // Don't unpause if we're in a sub-menu
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
                // Hide settings when resuming (disable entire canvas)
                if (settingsMenuUI != null)
                {
                    Transform canvas = settingsMenuUI.transform.parent;
                    if (canvas != null)
                    {
                        canvas.gameObject.SetActive(false);
                    }
                }
                // Hide hints when resuming (disable entire canvas)
                if (hintsMenuUI != null)
                {
                    Transform canvas = hintsMenuUI.transform.parent;
                    if (canvas != null)
                    {
                        canvas.gameObject.SetActive(false);
                    }
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

    #region Menu Navigation

    private void ShowPauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);

            // Show main pause menu panel, hide save/load, settings, and hints panels
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }

            if (saveLoadMenuUI != null)
            {
                saveLoadMenuUI.HideAllPanels();
            }

            if (settingsMenuUI != null)
            {
                Transform canvas = settingsMenuUI.transform.parent;
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(false);
                }
            }

            if (hintsMenuUI != null)
            {
                Transform canvas = hintsMenuUI.transform.parent;
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(false);
                }
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

    #endregion

    #region Pause/Resume

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

    #endregion

    #region Button Click Handlers

    // Public methods for button clicks
    public void OnResumeButtonClicked()
    {
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

        // REsume game from the GM
        // Then toggle pause state
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Paused)
        {
            GameManager.Instance.TogglePause();
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

    // Settings Button (NEW)
    public void OnSettingsButtonClicked()
    {
        Debug.Log("Settings button clicked");

        // Hide main pause menu panel first
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Debug.Log("Pause menu panel hidden");
        }

        // Show settings menu
        if (settingsMenuUI != null)
        {
            // CRITICAL: Enable the entire SettingsMenuCanvas (not just SettingsPanel)
            // This allows raycasters to work and prevents invisible ray blocking during gameplay
            Transform canvas = settingsMenuUI.transform.parent;
            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
                Debug.Log("Settings canvas enabled");
            }

            // Enable the SettingsPanel GameObject
            settingsMenuUI.gameObject.SetActive(true);
            Debug.Log("Settings panel GameObject enabled");

            // Reposition settings menu in front of player
            RepositionSettingsMenu();

            settingsMenuUI.ShowSettings();
            Debug.Log("Settings menu shown");
        }
        else
        {
            Debug.LogWarning("SettingsMenuUI reference is missing!");
        }
    }

    // Hints Button (NEW)
    public void OnHintsButtonClicked()
    {
        Debug.Log("Hints button clicked");

        // Hide main pause menu panel first
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Debug.Log("Pause menu panel hidden");
        }

        // Show hints menu
        if (hintsMenuUI != null)
        {
            // CRITICAL: Enable the entire HintsMenuCanvas (not just HintsPanel)
            // This allows raycasters to work and prevents invisible ray blocking during gameplay
            Transform canvas = hintsMenuUI.transform.parent;
            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
                Debug.Log("Hints canvas enabled");
            }

            // Enable the HintsPanel GameObject
            hintsMenuUI.gameObject.SetActive(true);
            Debug.Log("Hints panel GameObject enabled");

            // Reposition hints menu in front of player
            RepositionHintsMenu();

            hintsMenuUI.ShowHints();
            Debug.Log("Hints menu shown");
        }
        else
        {
            Debug.LogWarning("HintsMenuUI reference is missing!");
        }
    }

    private void RepositionSettingsMenu()
    {
        if (settingsMenuUI == null) return;

        // Get the settings canvas (parent of SettingsPanel)
        Transform settingsCanvas = settingsMenuUI.transform.parent;
        if (settingsCanvas == null) return;

        // Position in front of player's camera
        if (PlayerController.Instance?.XROrigin != null)
        {
            Camera playerCamera = PlayerController.Instance.XROrigin.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                // Position 0.5 units in front of camera at eye level
                Vector3 cameraForward = playerCamera.transform.forward;
                cameraForward.y = 0; // Keep on horizontal plane
                cameraForward.Normalize();

                Vector3 targetPosition = playerCamera.transform.position + (cameraForward * 0.5f);

                settingsCanvas.position = targetPosition;

                // Make it face the camera
                Vector3 directionToCamera = playerCamera.transform.position - targetPosition;
                directionToCamera.y = 0;
                if (directionToCamera != Vector3.zero)
                {
                    settingsCanvas.rotation = Quaternion.LookRotation(-directionToCamera);
                }

                Debug.Log($"Settings menu repositioned to {targetPosition}");
            }
        }
    }

    private void RepositionHintsMenu()
    {
        if (hintsMenuUI == null) return;

        // Get the hints canvas (parent of HintsPanel)
        Transform hintsCanvas = hintsMenuUI.transform.parent;
        if (hintsCanvas == null) return;

        // Position in front of player's camera
        if (PlayerController.Instance?.XROrigin != null)
        {
            Camera playerCamera = PlayerController.Instance.XROrigin.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                // Position 0.5 units in front of camera at eye level
                Vector3 cameraForward = playerCamera.transform.forward;
                cameraForward.y = 0; // Keep on horizontal plane
                cameraForward.Normalize();

                Vector3 targetPosition = playerCamera.transform.position + (cameraForward * 0.5f);

                hintsCanvas.position = targetPosition;

                // Make it face the camera
                Vector3 directionToCamera = playerCamera.transform.position - targetPosition;
                directionToCamera.y = 0;
                if (directionToCamera != Vector3.zero)
                {
                    hintsCanvas.rotation = Quaternion.LookRotation(-directionToCamera);
                }

                Debug.Log($"Hints menu repositioned to {targetPosition}");
            }
        }
    }

    // back from Save/Load/Settings/Hints to Pause Menu
    public void OnBackToPauseMenu()
    {
        Debug.Log("Back to pause menu");

        if (saveLoadMenuUI != null)
        {
            saveLoadMenuUI.HideAllPanels();
        }

        if (settingsMenuUI != null)
        {
            // Disable entire canvas to prevent ray blocking
            Transform canvas = settingsMenuUI.transform.parent;
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        if (hintsMenuUI != null)
        {
            // Disable entire canvas to prevent ray blocking
            Transform canvas = hintsMenuUI.transform.parent;
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
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

        // DEBUG: Check timer state before saving
        if (GameTimer.Instance != null)
        {
            Debug.Log($"[DEBUG] Timer before save - RemainingTime: {GameTimer.Instance.RemainingTime}, IsRunning: {GameTimer.Instance.IsRunning}, Mode: {GameTimer.Instance.Mode}");
        }

        // CRITICAL: Save to Continue Slot (Slot 0) before returning to menu
        if (SaveManager.Instance != null)
        {
            bool saveSuccess = SaveManager.Instance.SaveGame(0);
            Debug.Log($"[PauseMenu] Game saved to Continue Slot before returning to menu: {saveSuccess}");
        }
        else
        {
            Debug.LogError("[PauseMenu] SaveManager not found - cannot save before returning to menu!");
        }

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