using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeUIButtonHandler : MonoBehaviour
{
    public GameObject xrOrigin;
    public Transform mainMenuPosition;
    public GameObject escapedUI;
    public GameObject mainMenuUI;
    public ScreenFader screenFader;

    [Header("Exit Confirmation")]
    public GameObject exitConfirmationPanel; // Reference to the ExitConfirmationPanel

    void Start()
    {
        // Make sure exit confirmation is hidden at start
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }
    }

    public void RestartGame()
    {
        StartCoroutine(RestartGameRoutine());
    }

    // NEW METHOD: Show exit confirmation instead of directly returning to menu
    public void ShowExitConfirmation()
    {
        Debug.Log("ShowExitConfirmation() called from Escape UI");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ExitConfirmationPanel is not assigned in EscapeUIButtonHandler!");
        }
    }

    // NEW METHOD: Confirm exit to main menu (called by "Yes, Exit" button)
    public void ConfirmExitToMainMenu()
    {
        Debug.Log("Exit to main menu confirmed by user");
        ReturnToMainMenu();
    }

    // NEW METHOD: Cancel exit (called by "Cancel" button)
    public void CancelExit()
    {
        Debug.Log("Exit to main menu cancelled by user");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(BackToMenu());
    }

    private IEnumerator RestartGameRoutine()
    {
        if (screenFader != null)
        {
            screenFader.FadeIn(1f); // fade to black
            yield return new WaitForSeconds(1f);
        }

        GameMode.startFromMenu = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator BackToMenu()
    {
        if (screenFader != null)
        {
            screenFader.FadeIn(1f);
            yield return new WaitForSeconds(1f);
        }

        GameMode.startFromMenu = true; // show menu UI
        SceneManager.LoadScene("MainMenuScene");
    }
}