using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for displaying individual save slot information
/// Attach this to each save slot button/panel in your UI
/// </summary>

public class SaveSlotUI : MonoBehaviour
{
    [Header("Slot Info")]
    [SerializeField] private int slotNumber = 1;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI slotNumberText;
    [SerializeField] private TextMeshProUGUI saveNameText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI playtimeText;
    [SerializeField] private TextMeshProUGUI puzzlesSolvedText;
    [SerializeField] private TextMeshProUGUI sceneNameText;
    [SerializeField] private GameObject emptySlotPanel;
    [SerializeField] private GameObject dataPanel;

    [Header("Button")]
    [SerializeField] private Button slotButton;

    private SaveSlotInfo cachedInfo;
    private System.Action<int> onSlotSelected;

    private void Awake()
    {
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotButtonClicked);
        }
    }

    /// <summary>
    /// Initialize this slot UI with slot number and callback
    /// </summary>
    public void Initialize(int slot, System.Action<int> callback)
    {
        slotNumber = slot;
        onSlotSelected = callback;
        RefreshDisplay();
    }

    /// <summary>
    /// Refresh the slot display with current save data
    /// </summary>
    public void RefreshDisplay()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("SaveManager not found - cannot refresh slot display");
            return;
        }

        // Get save slot info
        cachedInfo = SaveManager.Instance.GetSaveSlotInfo(slotNumber);

        // Update slot number text
        if (slotNumberText != null)
        {
            slotNumberText.text = $"Slot {slotNumber}";
        }

        if (cachedInfo.hasData)
        {
            // Slot has save data - show data panel
            if (emptySlotPanel != null) emptySlotPanel.SetActive(false);
            if (dataPanel != null) dataPanel.SetActive(true);

            // Display save information
            if (saveNameText != null)
            {
                saveNameText.text = cachedInfo.saveName;
            }

            if (timestampText != null)
            {
                timestampText.text = FormatTimestamp(cachedInfo.saveTimestamp);
            }

            if (playtimeText != null)
            {
                playtimeText.text = $"Playtime: {FormatPlaytime(cachedInfo.playtime)}";
            }

            if (puzzlesSolvedText != null)
            {
                puzzlesSolvedText.text = $"Puzzles: {cachedInfo.puzzlesSolved}";
            }

            if (sceneNameText != null)
            {
                sceneNameText.text = $"Scene: {cachedInfo.sceneName}";
            }
        }
        else
        {
            // Slot is empty - show empty panel
            if (emptySlotPanel != null) emptySlotPanel.SetActive(true);
            if (dataPanel != null) dataPanel.SetActive(false);
        }
    }

    private void OnSlotButtonClicked()
    {
        // Invoke callback with this slot number
        onSlotSelected?.Invoke(slotNumber);
    }

    /// <summary>
    /// Format timestamp for display
    /// </summary>
    private string FormatTimestamp(DateTime timestamp)
    {
        TimeSpan timeSince = DateTime.Now - timestamp;

        if (timeSince.TotalMinutes < 1)
        {
            return "Just now";
        }
        else if (timeSince.TotalHours < 1)
        {
            return $"{(int)timeSince.TotalMinutes} minutes ago";
        }
        else if (timeSince.TotalDays < 1)
        {
            return $"{(int)timeSince.TotalHours} hours ago";
        }
        else if (timeSince.TotalDays < 7)
        {
            return $"{(int)timeSince.TotalDays} days ago";
        }
        else
        {
            return timestamp.ToString("MM/dd/yyyy");
        }
    }

    /// <summary>
    /// Format playtime for display
    /// </summary>
    private string FormatPlaytime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);

        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        }
        else if (time.TotalMinutes >= 1)
        {
            return $"{(int)time.TotalMinutes}m {time.Seconds}s";
        }
        else
        {
            return $"{(int)time.TotalSeconds}s";
        }
    }

    public bool HasData() => cachedInfo != null && cachedInfo.hasData;
    public int GetSlotNumber() => slotNumber;
}
