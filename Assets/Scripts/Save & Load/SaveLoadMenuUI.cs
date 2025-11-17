using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the Save/Load menu panels and slot selection
/// Attach this to your Pause Menu or Main Menu canvas
/// </summary>

public class SaveLoadMenuUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject savePanel;
    [SerializeField] private GameObject loadPanel;

    [Header("Save Slot UI References")]
    [SerializeField] private SaveSlotUI saveSlot1;
    [SerializeField] private SaveSlotUI saveSlot2;
    [SerializeField] private SaveSlotUI saveSlot3;

    [Header("Load Slot UI References")]
    [SerializeField] private SaveSlotUI loadSlot1;
    [SerializeField] private SaveSlotUI loadSlot2;
    [SerializeField] private SaveSlotUI loadSlot3;

    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDuration = 2f;

    private int pendingSlotNumber = -1;
    private bool isPendingSave = false;

    private void Start()
    {
        // Hide all panels at start
        HideAllPanels();

        // Initialize save slots
        if (saveSlot1 != null) saveSlot1.Initialize(1, OnSaveSlotSelected);
        if (saveSlot2 != null) saveSlot2.Initialize(2, OnSaveSlotSelected);
        if (saveSlot3 != null) saveSlot3.Initialize(3, OnSaveSlotSelected);

        // Initialize load slots
        if (loadSlot1 != null) loadSlot1.Initialize(1, OnLoadSlotSelected);
        if (loadSlot2 != null) loadSlot2.Initialize(2, OnLoadSlotSelected);
        if (loadSlot3 != null) loadSlot3.Initialize(3, OnLoadSlotSelected);
    }

    #region Show/Hide Panels

    public void ShowSavePanel()
    {
        Debug.Log("=== ShowSavePanel START ===");

        HideAllPanels();
        Debug.Log("HideAllPanels completed");

        if (savePanel != null)
        {
            Debug.Log($"savePanel is NOT null. Current active state: {savePanel.activeSelf}");
            Debug.Log($"Attempting to activate savePanel: {savePanel.name}");

            savePanel.SetActive(true);

            Debug.Log($"savePanel.SetActive(true) called. New active state: {savePanel.activeSelf}");
            Debug.Log($"savePanel parent: {savePanel.transform.parent?.name}");
            Debug.Log($"savePanel parent active: {savePanel.transform.parent?.gameObject.activeSelf}");

            RefreshSaveSlots();
            Debug.Log("RefreshSaveSlots completed");
        }
        else
        {
            Debug.LogError("savePanel is NULL!");
        }

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayMenuOpen();
            Debug.Log("UIAudioManager.PlayMenuOpen called");
        }

        Debug.Log("=== ShowSavePanel END ===");
    }

    public void ShowLoadPanel()
    {
        Debug.Log("=== ShowLoadPanel START ===");
        Debug.Log("ShowLoadPanel called");
        Debug.Log($"loadPanel is null? {loadPanel == null}");

        HideAllPanels();
        Debug.Log("HideAllPanels completed");

        if (loadPanel != null)
        {
            Debug.Log($"LoadPanel name: {loadPanel.name}");
            Debug.Log($"LoadPanel active before: {loadPanel.activeSelf}");

            Debug.Log($"Activating loadPanel: {loadPanel.name}");
            loadPanel.SetActive(true);

            Debug.Log($"LoadPanel active after SetActive(true): {loadPanel.activeSelf}");
            Debug.Log($"LoadPanel parent: {loadPanel.transform.parent?.name}");

            RefreshLoadSlots();
            Debug.Log($"loadPanel.activeSelf: {loadPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("Load Panel reference is NULL in SaveLoadMenuUI!");
        }

        UIAudioManager.Instance?.PlayMenuOpen();
        Debug.Log("=== ShowLoadPanel END ===");
    }

    public void HideAllPanels()
    {
        Debug.Log("HideAllPanels called");

        if (savePanel != null)
        {
            Debug.Log("Hiding savePanel");
            savePanel.SetActive(false);
        }

        if (loadPanel != null)
        {
            Debug.Log($"Hiding loadPanel (was {loadPanel.activeSelf})");
            loadPanel.SetActive(false);
        }

        if (confirmationPanel != null)
        {
            Debug.Log("Hiding confirmationPanel");
            confirmationPanel.SetActive(false);
        }

        if (feedbackPanel != null)
        {
            Debug.Log("Hiding feedbackPanel");
            feedbackPanel.SetActive(false);
        }

        Debug.Log("HideAllPanels finished");
    }

    #endregion

    #region Refresh Slots

    private void RefreshSaveSlots()
    {
        saveSlot1?.RefreshDisplay();
        saveSlot2?.RefreshDisplay();
        saveSlot3?.RefreshDisplay();
    }

    private void RefreshLoadSlots()
    {
        loadSlot1?.RefreshDisplay();
        loadSlot2?.RefreshDisplay();
        loadSlot3?.RefreshDisplay();
    }

    #endregion

    #region Save Slot Selection

    private void OnSaveSlotSelected(int slotNumber)
    {
        Debug.Log($"Save slot {slotNumber} selected");

        // Check if slot already has data
        bool slotHasData = SaveManager.Instance.SaveSlotExists(slotNumber);

        if (slotHasData)
        {
            // Show overwrite confirmation
            ShowOverwriteConfirmation(slotNumber);
        }
        else
        {
            // Slot is empty - save immediately
            ExecuteSave(slotNumber);
        }
    }

    private void ShowOverwriteConfirmation(int slotNumber)
    {
        pendingSlotNumber = slotNumber;
        isPendingSave = true;

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
        }

        if (confirmationText != null)
        {
            confirmationText.text = $"Overwrite save in Slot {slotNumber}?";
        }
    }

    public void OnConfirmOverwrite()
    {
        if (isPendingSave && pendingSlotNumber != -1)
        {
            ExecuteSave(pendingSlotNumber);
        }

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        UIAudioManager.Instance?.PlayConfirm();
    }

    public void OnCancelOverwrite()
    {
        pendingSlotNumber = -1;
        isPendingSave = false;

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        UIAudioManager.Instance?.PlayCancel();
    }

    private void ExecuteSave(int slotNumber)
    {
        bool success = SaveManager.Instance.SaveGame(slotNumber);

        if (success)
        {
            ShowFeedback($"Game saved to Slot {slotNumber}!");
            RefreshSaveSlots();

            // Close save panel after short delay
            // StartCoroutine(ClosePanelAfterDelay(savePanel, 1.5f));
        }
        else
        {
            ShowFeedback($"Failed to save to Slot {slotNumber}");
        }

        pendingSlotNumber = -1;
        isPendingSave = false;
    }

    #endregion

    #region Load Slot Selection

    private void OnLoadSlotSelected(int slotNumber)
    {
        Debug.Log($"Load slot {slotNumber} selected");

        // Check if slot has data
        bool slotHasData = SaveManager.Instance.SaveSlotExists(slotNumber);

        if (!slotHasData)
        {
            ShowFeedback($"Slot {slotNumber} is empty!");
            return;
        }

        // Show load confirmation
        ShowLoadConfirmation(slotNumber);
    }

    private void ShowLoadConfirmation(int slotNumber)
    {
        pendingSlotNumber = slotNumber;
        isPendingSave = false;

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
        }

        if (confirmationText != null)
        {
            confirmationText.text = $"Load game from Slot {slotNumber}?\n(Current progress will be lost)";
        }
    }

    public void OnConfirmLoad()
    {
        if (!isPendingSave && pendingSlotNumber != -1)
        {
            ExecuteLoad(pendingSlotNumber);
        }

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        UIAudioManager.Instance?.PlayConfirm();
    }

    public void OnCancelLoad()
    {
        pendingSlotNumber = -1;

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        UIAudioManager.Instance?.PlayCancel();
    }

    private void ExecuteLoad(int slotNumber)
    {
        ShowFeedback($"Loading from Slot {slotNumber}...");

        bool success = SaveManager.Instance.LoadGame(slotNumber);

        if (!success)
        {
            ShowFeedback($"Failed to load from Slot {slotNumber}");
        }

        // Note: If load succeeds, scene will change and this UI will be destroyed
        pendingSlotNumber = -1;
    }

    #endregion

    #region Feedback

    private void ShowFeedback(string message)
    {
        if (feedbackPanel != null && feedbackText != null)
        {
            feedbackPanel.SetActive(true);
            feedbackText.text = message;

            StartCoroutine(HideFeedbackAfterDelay());
        }

        Debug.Log($"[SaveLoadMenuUI] {message}");
    }

    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);

        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
    }

    private IEnumerator ClosePanelAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    #endregion

    #region Public Button Methods

    public void OnBackButton()
    {
        HideAllPanels();
        UIAudioManager.Instance?.PlayCancel();
    }

    #endregion
}
