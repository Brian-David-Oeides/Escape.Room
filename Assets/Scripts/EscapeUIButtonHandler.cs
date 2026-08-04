using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeUIButtonHandler : MonoBehaviour
{
    [Header("UI References")]
    public GameObject escapedUI;
    public Transform mainMenuPosition;

    [Header("Exit Confirmation")]
    public GameObject exitConfirmationPanel; // Reference to the ExitConfirmationPanel

    private void Start()
    {
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

        // Hide escaped UI initially
        if (escapedUI != null)
        {
            escapedUI.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Escaped:
                ShowEscapedUI();
                break;
            case GameState.Playing:
                HideEscapedUI();
                break;
            case GameState.Loading:
                HideEscapedUI();
                break;
        }
    }

    private void ShowEscapedUI()
    {
        if (escapedUI != null)
        {
            escapedUI.SetActive(true);
            // Position player for UI interaction if needed
            PositionPlayerForUI();
        }
    }

    private void HideEscapedUI()
    {
        if (escapedUI != null)
        {
            escapedUI.SetActive(false);
        }
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }
    }

    private void PositionPlayerForUI()
    {
        // Don't move the player - they should already be positioned correctly by CutSceneManager
        // The CutSceneManager has already moved them to the exit target position
        // We just need to make sure they're facing the UI properly
        if (PlayerController.Instance?.XROrigin != null && escapedUI != null)
        {
            // Optional: Rotate player to face the escaped UI if needed
            // But don't change their position - they should stay at the exit
        }
    }

    // Public methods called by UI buttons
    public void RestartGame()
    {
        GameLog.Log("RestartGame() called from Escape UI");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            GameLog.LogError("GameManager instance not found!");
        }
    }

    public void ShowExitConfirmation()
    {
        GameLog.Log("ShowExitConfirmation() called from Escape UI");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(true);
        }
        else
        {
            GameLog.LogWarning("ExitConfirmationPanel is not assigned in EscapeUIButtonHandler!");
        }
    }

    public void ConfirmExitToMainMenu()
    {
        GameLog.Log("Exit to main menu confirmed by user");
        ReturnToMainMenu();
    }

    public void CancelExit()
    {
        GameLog.Log("Exit to main menu cancelled by user");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }
    }

    public void ReturnToMainMenu()
    {
        GameLog.Log("ReturnToMainMenu() called from Escape UI");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
        else
        {
            GameLog.LogError("GameManager instance not found!");
        }
    }

    // Call this method when the player successfully escapes
    public void TriggerPlayerEscaped()
    {
        GameLog.Log("Player has escaped! Triggering escaped state.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerEscaped();
        }
        else
        {
            GameLog.LogError("GameManager instance not found!");
        }
    }
}