using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages clue discovery, puzzle attempt tracking, and hint system
/// </summary>

public class ClueManager : MonoSingleton<ClueManager>, ISaveable
{
    #region Data Structures

    [System.Serializable]
    public class ClueData
    {
        public string clueID;
        public string clueName;
        public string clueDescription;
        public string[] relatedPuzzleIDs;
        public bool isRequired;

        [NonSerialized] public bool isDiscovered;
        [NonSerialized] public float discoveryTime;
    }

    #endregion

    #region Configuration

    [Header("Clue Database")]
    [Tooltip("Define all clues in the game here")]
    public List<ClueData> allClues = new List<ClueData>();

    [Header("Hint Settings")]
    [Tooltip("Enable or disable the hint system")]
    public bool hintsEnabled = true;

    [Tooltip("Number of failed attempts before offering hint")]
    [Range(1, 10)]
    public int failedAttemptsBeforeHint = 1;

    [Header("Debug")]
    public bool showDebugLogs = true;

    #endregion

    #region Runtime Data

    private HashSet<string> discoveredClues = new HashSet<string>();
    private Dictionary<string, int> puzzleFailedAttempts = new Dictionary<string, int>();

    #endregion

    #region Events

    public event Action<ClueData> OnClueDiscovered;
    public event Action<string, int> OnPuzzleAttemptFailed;
    public event Action<string> OnHintAvailable;

    #endregion

    #region Initialization

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public override void Init()
    {
        DebugLog("ClueManager initialized");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Register that a clue has been discovered
    /// </summary>
    public void DiscoverClue(string clueID)
    {
        if (string.IsNullOrEmpty(clueID))
        {
            Debug.LogWarning("[ClueManager] Attempted to discover clue with empty ID");
            return;
        }

        if (discoveredClues.Contains(clueID))
        {
            DebugLog($"Clue already discovered: {clueID}");
            return;
        }

        discoveredClues.Add(clueID);

        ClueData clue = GetClueData(clueID);
        if (clue != null)
        {
            clue.isDiscovered = true;
            clue.discoveryTime = Time.time;

            OnClueDiscovered?.Invoke(clue);

            DebugLog($"✓ Clue discovered: {clue.clueName} ({clueID})");
        }
        else
        {
            Debug.LogWarning($"[ClueManager] Clue ID '{clueID}' not found in database!");
        }
    }

    /// <summary>
    /// Register a failed puzzle attempt
    /// </summary>
    public void RegisterFailedAttempt(string puzzleID)
    {
        if (string.IsNullOrEmpty(puzzleID))
        {
            Debug.LogWarning("[ClueManager] Attempted to register failed attempt with empty puzzle ID");
            return;
        }

        if (!puzzleFailedAttempts.ContainsKey(puzzleID))
        {
            puzzleFailedAttempts[puzzleID] = 0;
        }

        puzzleFailedAttempts[puzzleID]++;
        int attempts = puzzleFailedAttempts[puzzleID];

        OnPuzzleAttemptFailed?.Invoke(puzzleID, attempts);

        DebugLog($"Failed attempt on {puzzleID}: {attempts} total");

        // Check if hint should be offered
        CheckHintEligibility(puzzleID);
    }

    /// <summary>
    /// Get total number of discovered clues
    /// </summary>
    public int GetDiscoveredClueCount()
    {
        return discoveredClues.Count;
    }

    /// <summary>
    /// Get total number of clues in game
    /// </summary>
    public int GetTotalClueCount()
    {
        return allClues.Count;
    }

    /// <summary>
    /// Check if a specific clue has been discovered
    /// </summary>
    public bool IsClueDiscovered(string clueID)
    {
        return discoveredClues.Contains(clueID);
    }

    #endregion

    #region Private Methods

    private void CheckHintEligibility(string puzzleID)
    {
        if (!hintsEnabled) return;

        int attempts = puzzleFailedAttempts[puzzleID];

        // Check if there's a specific threshold for this puzzle
        int threshold = GetPuzzleHintThreshold(puzzleID);

        if (attempts >= threshold)
        {
            OnHintAvailable?.Invoke(puzzleID);
            DebugLog($"💡 Hint available for {puzzleID} (attempts: {attempts}/{threshold})");
        }
    }

    /// <summary>
    /// Get the hint threshold for a specific puzzle
    /// Checks if puzzle has custom threshold, otherwise uses global default
    /// </summary>
    private int GetPuzzleHintThreshold(string puzzleID)
    {
        // Try to find the puzzle GameObject by ID
        PuzzleBase[] allPuzzles = FindObjectsOfType<PuzzleBase>();

        foreach (PuzzleBase puzzle in allPuzzles)
        {
            if (puzzle.puzzleID == puzzleID)
            {
                // If puzzle has custom threshold (not 0), use it
                if (puzzle.hintThreshold > 0)
                {
                    return puzzle.hintThreshold;
                }
                break;
            }
        }

        // Fall back to global default
        return failedAttemptsBeforeHint;
    }

    private ClueData GetClueData(string clueID)
    {
        return allClues.Find(c => c.clueID == clueID);
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ClueManager] {message}");
        }
    }

    #endregion

    #region Save/Load System

    public string SaveID => "clue_manager";

    public void SaveState(SaveData saveData)
    {
        // Save discovered clues
        saveData.discoveredClueIDs = new List<string>(discoveredClues);

        // Save failed attempts (serialize Dictionary to List)
        saveData.puzzleFailedAttemptsData.Clear();
        foreach (var kvp in puzzleFailedAttempts)
        {
            saveData.puzzleFailedAttemptsData.Add($"{kvp.Key}:{kvp.Value}");
        }

        // Statistics
        saveData.totalCluesDiscovered = discoveredClues.Count;

        DebugLog($"Saved {discoveredClues.Count} discovered clues, {puzzleFailedAttempts.Count} puzzle attempts");
    }

    public void LoadState(SaveData saveData)
    {
        // Clear existing data
        discoveredClues.Clear();

        // Load discovered clues
        foreach (string clueID in saveData.discoveredClueIDs)
        {
            discoveredClues.Add(clueID);

            // Update ClueData
            ClueData clue = GetClueData(clueID);
            if (clue != null)
            {
                clue.isDiscovered = true;
            }
        }

        // Load failed attempts (deserialize List to Dictionary)
        puzzleFailedAttempts.Clear();
        foreach (string data in saveData.puzzleFailedAttemptsData)
        {
            string[] parts = data.Split(':');
            if (parts.Length == 2)
            {
                string puzzleID = parts[0];
                if (int.TryParse(parts[1], out int attempts))
                {
                    puzzleFailedAttempts[puzzleID] = attempts;
                }
            }
        }

        DebugLog($"Loaded {discoveredClues.Count} discovered clues, {puzzleFailedAttempts.Count} puzzle attempts");
    }

    #endregion

    #region Testing Methods (Remove after testing)

    [ContextMenu("Test: Discover Journal_005")]
    private void TestDiscoverClue()
    {
        DiscoverClue("journal_005");
    }

    [ContextMenu("Test: Register Failed Attempt on Safe")]
    private void TestFailedAttempt()
    {
        RegisterFailedAttempt("safe_dial");
    }

    [ContextMenu("Test: Show Clue Stats")]
    private void TestShowStats()
    {
        Debug.Log($"Discovered: {GetDiscoveredClueCount()}/{GetTotalClueCount()} clues");
    }

    #endregion
}
