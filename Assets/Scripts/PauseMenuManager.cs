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

    [Header("Input")]
    public InputActionProperty menuButtonAction; // For XR controller input

    private bool _isMenuButtonPressed = false;
    private Coroutine _resumeCoroutine;

    private void Start()
    {
        // Hide pause menu at start
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
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
                break;

            case GameState.Loading:
            case GameState.MainMenu:
            case GameState.Escaped:
            case GameState.GameOver:
                HidePauseMenu();
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


    public void OnMainMenuButtonClicked()
    {
        Debug.Log("Main Menu button clicked from pause menu");
        if (GameManager.Instance != null)
        {
            // First unpause the game (to reset Time.timeScale)
            if (GameManager.Instance.currentState == GameState.Paused)
            {
                Time.timeScale = 1f;
            }
            GameManager.Instance.ReturnToMainMenu();
        }
    }

    public void OnExitGameButtonClicked()
    {
        Debug.Log("Exit Game button clicked");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}