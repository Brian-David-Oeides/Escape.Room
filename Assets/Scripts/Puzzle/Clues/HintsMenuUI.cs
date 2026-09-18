using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Hints Menu UI in the pause menu system
/// Shows clue collection progress and allows manual hint requests
/// </summary>
/// 

public class HintsMenuUI : MonoBehaviour
{
    #region UI References

    [Header("Main Panel")]
    [SerializeField] private GameObject hintsPanel;
    [SerializeField] private Button backButton;

    [Header("Clue Collection Display")]
    [SerializeField] private TextMeshProUGUI clueProgressText;
    [SerializeField] private TextMeshProUGUI clueListText;

    [Header("Manual Hint Request")]
    [SerializeField] private Button requestHintButton;
    [SerializeField] private TextMeshProUGUI requestHintButtonText;
    [SerializeField] private TextMeshProUGUI cooldownTimerText;

    [Header("Statistics Display")]
    [SerializeField] private TextMeshProUGUI hintsRequestedText;
    [SerializeField] private TextMeshProUGUI hintsRemainingText;

    [Header("Hint Request Settings")]
    [Tooltip("Cooldown time in seconds - overridden by SettingsManager on Start")]
    [SerializeField] private float hintCooldown = 60f; // 60 seconds between manual hint requests
    [Tooltip("Maximum manual hints per session - overridden by SettingsManager on Start")]
    [SerializeField] private int maxManualHints = 5; // Maximum manual hints per session

    #endregion

    #region Private Variables

    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;
    private int hintsRequested = 0;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Wire up back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // Wire up hint request button
        if (requestHintButton != null)
        {
            requestHintButton.onClick.AddListener(OnRequestHintClicked);
        }

        // Load settings from SettingsManager
        LoadHintSettings();

        // Subscribe to settings changes
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnHintSettingsChanged += OnHintSettingsChanged;
        }

        // Subscribe to puzzle completions - refresh the manual-hint budget per puzzle solved
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.OnPuzzleCompleted += OnAnyPuzzleCompleted;
        }

        // Hide panel initially
        if (hintsPanel != null)
        {
            hintsPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from settings changes
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnHintSettingsChanged -= OnHintSettingsChanged;
        }

        // Unsubscribe from puzzle completions
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.OnPuzzleCompleted -= OnAnyPuzzleCompleted;
        }
    }

    /// <summary>
    /// Refresh the manual-hint budget whenever a puzzle is genuinely solved.
    /// puzzleID is null when this fires from a save-load restore rather than a live completion,
    /// so we ignore that case rather than treating a loaded save as a fresh solve.
    /// </summary>
    private void OnAnyPuzzleCompleted(int totalCompleted, string puzzleID)
    {
        if (string.IsNullOrEmpty(puzzleID)) return;

        hintsRequested = 0;
        GameLog.Log($"[HintsMenuUI] Puzzle '{puzzleID}' completed - manual hint count reset");
        RefreshAllDisplays();
    }

    private void Update()
    {
        // Update cooldown timer
        if (isOnCooldown)
        {
            cooldownTimer -= Time.unscaledDeltaTime; // Use unscaled time since game is paused

            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
            }

            UpdateCooldownDisplay();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Show the hints menu and refresh all displays
    /// </summary>
    public void ShowHints()
    {
        if (hintsPanel != null)
        {
            hintsPanel.SetActive(true);
            RefreshAllDisplays();

            // Audio feedback
            UIAudioManager.Instance?.PlayMenuOpen();

            GameLog.Log("[HintsMenuUI] Hints menu shown");
        }
    }

    /// <summary>
    /// Hide the hints menu
    /// </summary>
    public void HideHints()
    {
        if (hintsPanel != null)
        {
            hintsPanel.SetActive(false);

            // Audio feedback
            UIAudioManager.Instance?.PlayMenuClose();

            GameLog.Log("[HintsMenuUI] Hints menu hidden");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Refresh all UI displays with current data
    /// </summary>
    private void RefreshAllDisplays()
    {
        RefreshClueProgress();
        RefreshClueList();
        RefreshHintButton();
        RefreshStatistics();
    }

    /// <summary>
    /// Update the clue collection progress text (e.g., "3/7 Clues Found")
    /// </summary>
    private void RefreshClueProgress()
    {
        if (ClueManager.Instance != null && clueProgressText != null)
        {
            int discovered = ClueManager.Instance.GetDiscoveredClueCount();
            int total = ClueManager.Instance.GetTotalClueCount();
            clueProgressText.text = $"Clues Found: {discovered}/{total}";
        }
    }

    /// <summary>
    /// Update the list of discovered clues
    /// </summary>
    private void RefreshClueList()
    {
        if (ClueManager.Instance != null && clueListText != null)
        {
            List<string> discoveredClueNames = new List<string>();

            foreach (var clue in ClueManager.Instance.allClues)
            {
                if (clue.isDiscovered)
                {
                    // Use FOUND: prefix for clear indication
                    discoveredClueNames.Add($"FOUND: {clue.clueName}");
                }
            }

            if (discoveredClueNames.Count > 0)
            {
                clueListText.text = string.Join("\n", discoveredClueNames);
            }
            else
            {
                clueListText.text = "No clues discovered yet.\nExplore the room to find clues!";
            }
        }
    }

    /// <summary>
    /// Update hint request button state and text
    /// </summary>
    private void RefreshHintButton()
    {
        if (requestHintButton == null) return;

        // Check if player has reached max hints
        bool maxHintsReached = hintsRequested >= maxManualHints;

        // Check if ClueManager is available
        bool clueManagerAvailable = ClueManager.Instance != null && ClueManager.Instance.hintsEnabled;

        // Button is enabled only if: not on cooldown, hasn't reached max, and system available
        requestHintButton.interactable = !isOnCooldown && !maxHintsReached && clueManagerAvailable;

        // Update button text
        if (requestHintButtonText != null)
        {
            if (maxHintsReached)
            {
                requestHintButtonText.text = "Max Hints Reached";
            }
            else if (isOnCooldown)
            {
                requestHintButtonText.text = $"Cooldown: {Mathf.CeilToInt(cooldownTimer)}s";
            }
            else if (!clueManagerAvailable)
            {
                requestHintButtonText.text = "Hints Disabled";
            }
            else
            {
                requestHintButtonText.text = "Request Hint";
            }
        }
    }

    /// <summary>
    /// Update the cooldown timer display
    /// </summary>
    private void UpdateCooldownDisplay()
    {
        if (cooldownTimerText != null)
        {
            if (isOnCooldown)
            {
                cooldownTimerText.text = $"Next hint available in: {Mathf.CeilToInt(cooldownTimer)}s";
                cooldownTimerText.gameObject.SetActive(true);
            }
            else
            {
                cooldownTimerText.gameObject.SetActive(false);
            }
        }

        // Also update button text during cooldown
        RefreshHintButton();
    }

    /// <summary>
    /// Update statistics display
    /// </summary>
    private void RefreshStatistics()
    {
        if (hintsRequestedText != null)
        {
            hintsRequestedText.text = $"Hints Requested: {hintsRequested}";
        }

        if (hintsRemainingText != null)
        {
            int remaining = Mathf.Max(0, maxManualHints - hintsRequested);
            hintsRemainingText.text = $"Hints Remaining: {remaining}/{maxManualHints}";
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Handle back button click - returns to main pause menu
    /// </summary>
    public void OnBackButtonClicked()
    {
        GameLog.Log("[HintsMenuUI] Back button clicked");

        // Audio feedback
        UIAudioManager.Instance?.PlayCancel();

        // Notify PauseMenuManager (it will handle hiding this menu and showing main menu)
        PauseMenuManager pauseManager = FindObjectOfType<PauseMenuManager>();
        if (pauseManager != null)
        {
            pauseManager.OnBackToPauseMenu();
        }
    }

    /// <summary>
    /// Handle manual hint request button click
    /// </summary>
    public void OnRequestHintClicked()
    {
        // Safety checks
        if (ClueManager.Instance == null || !ClueManager.Instance.hintsEnabled)
        {
            GameLog.LogWarning("[HintsMenuUI] Cannot request hint - ClueManager not available or hints disabled");
            UIAudioManager.Instance?.PlayError();
            return;
        }

        if (isOnCooldown)
        {
            GameLog.Log("[HintsMenuUI] Cannot request hint - on cooldown");
            UIAudioManager.Instance?.PlayError();
            return;
        }

        if (hintsRequested >= maxManualHints)
        {
            GameLog.Log("[HintsMenuUI] Cannot request hint - max hints reached");
            UIAudioManager.Instance?.PlayError();
            return;
        }

        GameLog.Log("[HintsMenuUI] Manual hint requested");

        // Audio feedback - success
        UIAudioManager.Instance?.PlayConfirm();

        // Request hint for a random puzzle (or most recently failed puzzle)
        RequestManualHint();

        // Start cooldown
        isOnCooldown = true;
        cooldownTimer = hintCooldown;

        // Increment counter
        hintsRequested++;

        // Refresh displays
        RefreshAllDisplays();
    }

    /// <summary>
    /// Request a manual hint from ClueManager
    /// Strategy: Show hint for the puzzle with the most failed attempts
    /// </summary>
    private void RequestManualHint()
    {
        if (ClueManager.Instance == null) return;

        // Find puzzle with most failed attempts
        string targetPuzzleID = FindPuzzleNeedingHelp();

        if (string.IsNullOrEmpty(targetPuzzleID))
        {
            // No puzzles with failed attempts - show generic message
            ShowGenericHint();
            return;
        }

        // Get hint for this puzzle
        string hintMessage = ClueManager.Instance.GetHint(targetPuzzleID);

        // Display hint using HintUIController
        if (ClueManager.Instance.hintUI != null)
        {
            ClueManager.Instance.hintUI.ShowHint(hintMessage);
            GameLog.Log($"[HintsMenuUI] Showing manual hint for puzzle: {targetPuzzleID}");
        }
        else
        {
            GameLog.LogError("[HintsMenuUI] HintUI reference not set in ClueManager!");
        }
    }

    /// <summary>
    /// Find the puzzle that needs help most (highest failed attempts, excluding completed puzzles)
    /// </summary>
    private string FindPuzzleNeedingHelp()
    {
        if (ClueManager.Instance == null) return null;

        PuzzleBase[] allPuzzles = FindObjectsOfType<PuzzleBase>();

        string bestPuzzleID = null;
        int bestFailedAttempts = 0;

        foreach (PuzzleBase puzzle in allPuzzles)
        {
            if (string.IsNullOrEmpty(puzzle.puzzleID))
                continue;

            if (puzzle.IsCompleted)
                continue; // never target an already-solved puzzle, even with a stale attempt count

            int attempts = ClueManager.Instance.GetFailedAttemptCount(puzzle.puzzleID);
            if (attempts > 0 && attempts > bestFailedAttempts)
            {
                bestFailedAttempts = attempts;
                bestPuzzleID = puzzle.puzzleID;
            }
        }

        return bestPuzzleID;
    }

    /// <summary>
    /// Show a generic hint when no specific puzzle is targeted
    /// </summary>
    private void ShowGenericHint()
    {
        string genericHint = "Hint: Nothing specific to point you toward right now — try examining objects closely, reading notes, and checking under or inside things.";

        if (ClueManager.Instance != null && ClueManager.Instance.hintUI != null)
        {
            ClueManager.Instance.hintUI.ShowHint(genericHint);
            GameLog.Log("[HintsMenuUI] Showing generic hint");
        }
    }

    #endregion

    #region Settings Integration

    /// <summary>
    /// Load hint settings from SettingsManager
    /// </summary>
    private void LoadHintSettings()
    {
        if (SettingsManager.Instance != null)
        {
            maxManualHints = SettingsManager.Instance.GetMaxManualHints();
            hintCooldown = SettingsManager.Instance.GetHintCooldown();

            GameLog.Log($"[HintsMenuUI] Loaded settings: MaxHints={maxManualHints}, Cooldown={hintCooldown}s");
        }
    }

    /// <summary>
    /// Handle hint settings changes from SettingsManager
    /// </summary>
    /// <param name="hintsEnabled">Whether hints are enabled</param>
    /// <param name="maxHints">Maximum manual hints</param>
    /// <param name="cooldown">Cooldown in seconds</param>
    private void OnHintSettingsChanged(bool hintsEnabled, int maxHints, float cooldown)
    {
        maxManualHints = maxHints;
        hintCooldown = cooldown;

        // Refresh displays to reflect new max hints
        RefreshAllDisplays();

        GameLog.Log($"[HintsMenuUI] Settings updated: Enabled={hintsEnabled}, MaxHints={maxHints}, Cooldown={cooldown}s");
    }

    #endregion

    #region Testing Methods

    [ContextMenu("Test: Reset Hint Statistics")]
    private void TestResetStats()
    {
        hintsRequested = 0;
        isOnCooldown = false;
        cooldownTimer = 0f;
        RefreshAllDisplays();
        GameLog.Log("[HintsMenuUI] Statistics reset");
    }

    #endregion
}