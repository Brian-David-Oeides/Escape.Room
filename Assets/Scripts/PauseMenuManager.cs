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
    public GameObject mainMenuButton;
    public GameObject exitGameButton;

    [Header("Exit Confirmation")]
    public GameObject exitConfirmationPanel;
    public TextMeshProUGUI confirmationText; // Text component to change the message
    public GameObject confirmYesButton;
    public GameObject confirmCancelButton;

    [Header("Input")]
    public InputActionProperty menuButtonAction; // For XR controller input

    private bool _isMenuButtonPressed = false;
    private Coroutine _resumeCoroutine;
    private System.Action _pendingConfirmAction; // Store which action to execute if confirmed

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

            // Position menu in front of player
            // PositionMenuInFrontOfPlayer();
        }
    }

    private void HidePauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
    }

    /* private void PositionMenuInFrontOfPlayer()
    {
        if (Camera.main != null && pauseMenuCanvas != null)
        {
            Transform cam = Camera.main.transform;
            Vector3 forward = cam.forward;
            Vector3 spawnPos = cam.position + forward * 2f; // 2 meters in front

            pauseMenuCanvas.transform.position = spawnPos;

            // Make it face the player
            Vector3 lookDir = pauseMenuCanvas.transform.position - cam.position;
            lookDir.y = 0; // Keep upright
            pauseMenuCanvas.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }*/

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
        if (GameManager.Instance != null && GameManager.Instance.xrOrigin != null)
        {
            var continuousMove = GameManager.Instance.xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
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


    // Modified to show confirmation instead of direct action
    public void OnMainMenuButtonClicked()
    {
        Debug.Log("Main Menu button clicked - showing confirmation");
        ShowExitConfirmation("Are you sure you want to return to the Main Menu?\nAll progress will be lost.",
            () => ExecuteReturnToMainMenu());
    }

    // Modified to show confirmation instead of direct action
    public void OnExitGameButtonClicked()
    {
        Debug.Log("Exit Game button clicked - showing confirmation");
        ShowExitConfirmation("Are you sure you want to exit the game?",
            () => ExecuteExitGame());
    }

    // New method to show confirmation with custom text
    private void ShowExitConfirmation(string message, System.Action confirmAction)
    {
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(true);

            if (confirmationText != null)
            {
                confirmationText.text = message;
            }

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
        HideExitConfirmation();
        _pendingConfirmAction?.Invoke();
        _pendingConfirmAction = null;
    }

    // Called by "Cancel" button in confirmation dialog
    public void OnConfirmCancel()
    {
        Debug.Log("Action cancelled by user");
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

}