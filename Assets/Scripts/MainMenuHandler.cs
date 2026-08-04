using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using static LocomotionController;

public class MainMenuHandler : MonoBehaviour
{
    [Header("Menu UI References")]
    public GameObject mainMenuUI;
    public Transform mainMenuSpawnPoint; // reference for the MainMenuPosition

    [Header("UI Panels")]
    public GameObject mainMenuPanel;

    [Header("Main Menu Buttons")]
    public GameObject newGameButton;
    public GameObject continueButton;
    public GameObject loadGameButton;
    public GameObject settingsButton;
    public GameObject exitButton;

    [Header("Save/Load UI")]
    public SaveLoadMenuUI saveLoadMenuUI;

    [Header("Settings UI")]
    public SettingsMenuUI settingsMenuUI;

    [Header("Exit Confirmation")]
    public GameObject exitConfirmationPanel; // Reference to the ExitConfirmationPanel

    [Header("Position Lock Settings")]
    [SerializeField] private bool lockPositionInMenu = true;

    private Vector3 lockedPosition;
    private bool isInMainMenu = false;

    private void Start()
    {
        // Subscribe to GameManager state changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        // Initialize menu if we're in main menu scene
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            StartCoroutine(InitializeMainMenuDelayed());
        }
        else
        {
            // In gameplay scene - hide menu UI and disable this script
            if (mainMenuUI != null)
            {
                mainMenuUI.SetActive(false);
            }
            this.enabled = false;
        }
    }

    private IEnumerator InitializeMainMenuDelayed()
    {
        // Wait for PlayerController to be initialized
        while (PlayerController.Instance == null || PlayerController.Instance.XROrigin == null)
        {
            yield return null;
        }

        // Wait one more frame to ensure everything is set up
        yield return null;

        InitializeMainMenu();

        // Ensure locomotion is in menu mode
        if (LocomotionController.Instance != null)
        {
            LocomotionController.Instance.SwitchToMenuMode();
        }

        // Update Continue button state based on available saves
        UpdateContinueButton();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }

    private void InitializeMainMenu()
    {
        // Position and orient the XR Origin
        if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            GameObject xrOrigin = PlayerController.Instance.XROrigin;

            if (mainMenuSpawnPoint != null)
            {
                xrOrigin.transform.position = mainMenuSpawnPoint.position;
                xrOrigin.transform.rotation = mainMenuSpawnPoint.rotation;

                // Store the locked position
                lockedPosition = mainMenuSpawnPoint.position;

                if (mainMenuUI != null)
                {
                    RotateOriginToFace(mainMenuUI.transform.position);
                }

                GameLog.Log("Player positioned at main menu spawn point");
            }
        }

        // Show main menu UI (canvas always stays active)
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);  // Show button panel
        }

        // Hide exit confirmation
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }

        // Hide save/load panels
        if (saveLoadMenuUI != null)
        {
            saveLoadMenuUI.HideAllPanels();
        }

        // Hide settings menu
        if (settingsMenuUI != null)
        {
            settingsMenuUI.HideSettings();
        }

        isInMainMenu = true;
    }

    private void LateUpdate()
    {
        // Lock player position in main menu
        if (lockPositionInMenu && isInMainMenu && PlayerController.Instance?.XROrigin != null)
        {
            PlayerController.Instance.XROrigin.transform.position = lockedPosition;
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(true);  // Show buttons
                }
                isInMainMenu = true;
                UpdateContinueButton();
                break;

            case GameState.Loading:
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(false);  // Hide buttons
                }
                isInMainMenu = false;
                break;

            default:
                isInMainMenu = false;
                break;
        }
    }

    #region Button Click Handlers

    // Public methods called by UI buttons
    public void StartNewGame()
    {
        GameLog.Log("StartNewGame() called from UI button");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            GameLog.LogError("GameManager instance not found!");
        }
    }

    // Continue (Load from Continue Slot 0)
    public void OnContinueButtonClicked()
    {
        if (SaveManager.Instance == null)
        {
            GameLog.LogError("SaveManager not found!");
            return;
        }

        // CRITICAL: Continue always loads from Slot 0 (hidden continue slot)
        if (!SaveManager.Instance.SaveSlotExists(0))
        {
            GameLog.LogWarning("No continue save found!");
            return;
        }

        GameLog.Log("[MainMenu] Loading from Continue Slot (Slot 0)");
        SaveManager.Instance.LoadGame(0);
    }

    // Load Game (Show Load Panel)
    public void OnLoadGameButtonClicked()
    {
        GameLog.Log("Load Game button clicked");

        if (saveLoadMenuUI != null)
        {
            // Hide main menu PANEL (not canvas)
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);  // Only hides buttons
                GameLog.Log("Main menu panel hidden");
            }

            saveLoadMenuUI.ShowLoadPanel();
            GameLog.Log("ShowLoadPanel called");
        }
        else
        {
            GameLog.LogWarning("SaveLoadMenuUI reference is missing!");
        }
    }

    // Settings (Show Settings Menu)
    public void OnSettingsButtonClicked()
    {
        GameLog.Log("Settings button clicked");

        if (settingsMenuUI != null)
        {
            // Hide main menu PANEL (not canvas)
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);  // Only hides buttons
                GameLog.Log("Main menu panel hidden");
            }

            settingsMenuUI.ShowSettings();
            GameLog.Log("Settings menu shown");
        }
        else
        {
            GameLog.LogWarning("SettingsMenuUI reference is missing!");
        }
    }

    // Back from Load Panel to Main Menu
    public void OnBackToMainMenu()
    {
        GameLog.Log("Back to main menu");

        if (saveLoadMenuUI != null)
        {
            saveLoadMenuUI.HideAllPanels();
        }

        if (settingsMenuUI != null)
        {
            settingsMenuUI.HideSettings();
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);  // ✅ Re-shows buttons!
        }
    }

    public void ShowExitConfirmation()
    {
        GameLog.Log("ShowExitConfirmation() called");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(true);
        }
        else
        {
            GameLog.LogWarning("ExitConfirmationPanel is not assigned!");
        }
    }

    public void ConfirmExit()
    {
        GameLog.Log("Exit confirmed by user");
        ExitGame();
    }

    public void CancelExit()
    {
        GameLog.Log("Exit cancelled by user");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }
    }

    public void ExitGame()
    {
        GameLog.Log("Quitting game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    #endregion

    #region Helper Methods

    // Update Continue Button Interactability
    private void UpdateContinueButton()
    {
        if (continueButton == null || SaveManager.Instance == null)
            return;

        // CRITICAL: Continue button only checks Slot 0 (continue slot)
        bool hasContinueData = SaveManager.Instance.SaveSlotExists(0);

        // Enable/disable continue button
        var button = continueButton.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.interactable = hasContinueData;
        }

        GameLog.Log($"Continue button {(hasContinueData ? "enabled" : "disabled")} - Continue Slot exists: {hasContinueData}");
    }

    private void RotateOriginToFace(Vector3 targetPosition)
    {
        if (PlayerController.Instance?.XROrigin == null) return;

        GameObject xrOrigin = PlayerController.Instance.XROrigin;
        Camera camera = xrOrigin.GetComponentInChildren<Camera>();
        if (camera == null) return;

        Vector3 headsetForward = camera.transform.forward;
        headsetForward.y = 0;

        Vector3 directionToUI = targetPosition - camera.transform.position;
        directionToUI.y = 0;

        float angle = Vector3.SignedAngle(headsetForward, directionToUI, Vector3.up);
        xrOrigin.transform.Rotate(0, angle, 0);
    }

    #endregion
}