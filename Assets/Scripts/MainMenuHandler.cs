using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

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
    public GameObject exitButton;

    [Header("Save/Load UI")]  
    public SaveLoadMenuUI saveLoadMenuUI;

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

        // Force disable movement again to ensure it's locked
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
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

                Debug.Log("Player positioned at main menu spawn point");
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
        Debug.Log("StartNewGame() called from UI button");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }

    // Continue (Load Most Recent Save)
    public void OnContinueButtonClicked()
    {
        //Debug.Log("Continue button clicked");

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager not found!");
            return;
        }

        // Find most recent save slot
        int mostRecentSlot = GetMostRecentSaveSlot();
       

        if (mostRecentSlot == -1)
        {
            Debug.LogWarning("No saves found to continue!");
            return;
        }

        // Debug.Log($"Continuing from most recent save: Slot {mostRecentSlot}");
        SaveManager.Instance.LoadGame(mostRecentSlot);
    }

    // Load Game (Show Load Panel)
    public void OnLoadGameButtonClicked()
    {
        Debug.Log("Load Game button clicked");

        if (saveLoadMenuUI != null)
        {
            // Hide main menu PANEL (not canvas)
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);  // Only hides buttons
                Debug.Log("Main menu panel hidden");
            }

            saveLoadMenuUI.ShowLoadPanel();
            Debug.Log("ShowLoadPanel called");
        }
        else
        {
            Debug.LogWarning("SaveLoadMenuUI reference is missing!");
        }
    }

    // Back from Load Panel to Main Menu
    public void OnBackToMainMenu()
    {
        Debug.Log("Back to main menu");

        if (saveLoadMenuUI != null)
        {
            saveLoadMenuUI.HideAllPanels();
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);  // ✅ Re-shows buttons!
        }
    }

    public void ShowExitConfirmation()
    {
        Debug.Log("ShowExitConfirmation() called");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ExitConfirmationPanel is not assigned!");
        }
    }

    public void ConfirmExit()
    {
        Debug.Log("Exit confirmed by user");
        ExitGame();
    }

    public void CancelExit()
    {
        Debug.Log("Exit cancelled by user");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }
    }

    public void ExitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    #endregion

    #region Helper Methods

    // NEW: Update Continue Button Interactability
    private void UpdateContinueButton()
    {
        if (continueButton == null || SaveManager.Instance == null)
            return;

        // Check if any save slots have data
        bool hasSaveData = SaveManager.Instance.SaveSlotExists(1) ||
                          SaveManager.Instance.SaveSlotExists(2) ||
                          SaveManager.Instance.SaveSlotExists(3);

        // Enable/disable continue button
        var button = continueButton.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.interactable = hasSaveData;
        }

        Debug.Log($"Continue button {(hasSaveData ? "enabled" : "disabled")} - Save data exists: {hasSaveData}");
    }

    // Find Most Recent Save Slot
    private int GetMostRecentSaveSlot()
    {
        if (SaveManager.Instance == null)
        {
            return -1;
        }

        SaveSlotInfo[] allSlots = SaveManager.Instance.GetAllSaveSlots();

        int mostRecentSlot = -1;
        System.DateTime mostRecentTime = System.DateTime.MinValue;

        foreach (SaveSlotInfo slot in allSlots)
        {
            Debug.Log($"Slot {slot.slotNumber}: hasData={slot.hasData}, timestamp={slot.saveTimestamp}");

            if (slot.hasData && slot.saveTimestamp >= mostRecentTime)
            {
                mostRecentTime = slot.saveTimestamp;
                mostRecentSlot = slot.slotNumber;
            }
        }

        return mostRecentSlot;
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