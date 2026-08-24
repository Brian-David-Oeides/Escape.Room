using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized manager for tracking puzzle completions and firing events
/// </summary>
/// 
public class PuzzleManager : MonoSingleton<PuzzleManager>, ISaveable
{
    [Header("Puzzle Tracking")]
    [SerializeField] private int totalPuzzlesCompleted = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Events
    public System.Action<int, string> OnPuzzleCompleted; // Fires with total count and the completed puzzle's ID
    public event System.Action<int> OnPuzzleUnregistered; // Fires with total count - sabotage-driven decrements only
    public event System.Action OnPuzzlesReset;

    // Track which puzzles have been completed this session
    private HashSet<string> completedPuzzlesThisSession = new HashSet<string>();

    // Sabotage orchestrator tracking
    private Dictionary<string, ISabotageable> sabotageableRegistry = new Dictionary<string, ISabotageable>();
    private HashSet<string> sabotagedOnceIDs = new HashSet<string>();

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Init()
    {
        DontDestroyOnLoad(gameObject);
        DebugLog("PuzzleManager initialized");
    }

    /// <summary>
    /// Called by PuzzleBase when a puzzle is completed
    /// </summary>
    public void RegisterPuzzleCompletion(string puzzleID)
    {
        // Prevent duplicate counting
        if (completedPuzzlesThisSession.Contains(puzzleID))
        {
            DebugLog($"Puzzle {puzzleID} already counted this session");
            return;
        }

        completedPuzzlesThisSession.Add(puzzleID);
        totalPuzzlesCompleted++;

        DebugLog($"Puzzle completed: {puzzleID} (Total: {totalPuzzlesCompleted})");

        // Fire event for NPC behavior, UI, etc.
        OnPuzzleCompleted?.Invoke(totalPuzzlesCompleted, puzzleID);
    }

    public void UnregisterPuzzleCompletion(string puzzleID)
    {
        if (completedPuzzlesThisSession.Contains(puzzleID))
        {
            completedPuzzlesThisSession.Remove(puzzleID);
            totalPuzzlesCompleted--;
            DebugLog($"Puzzle UN-completed (sabotaged): {puzzleID} (Total: {totalPuzzlesCompleted})");
            OnPuzzleUnregistered?.Invoke(totalPuzzlesCompleted);
        }
        else
        {
            DebugLog($"Cannot unregister {puzzleID} - was not registered as completed");
        }
    }

    public void RegisterSabotageable(string puzzleID, ISabotageable sabotageable)
    {
        sabotageableRegistry[puzzleID] = sabotageable;
    }

    public void UnregisterSabotageable(string puzzleID)
    {
        sabotageableRegistry.Remove(puzzleID);
    }

    public List<string> GetEligibleSabotageIDs()
    {
        List<string> eligible = new List<string>();
        foreach (string puzzleID in sabotageableRegistry.Keys)
        {
            if (completedPuzzlesThisSession.Contains(puzzleID) && !sabotagedOnceIDs.Contains(puzzleID))
                eligible.Add(puzzleID);
        }
        return eligible;
    }

    public void MarkSabotaged(string puzzleID)
    {
        sabotagedOnceIDs.Add(puzzleID);
    }

    public ISabotageable GetSabotageable(string puzzleID)
    {
        return sabotageableRegistry.ContainsKey(puzzleID) ? sabotageableRegistry[puzzleID] : null;
    }

    /// <summary>
    /// Called when loading a save to restore puzzle count
    /// </summary>
    public void RestorePuzzleCount(int count, List<string> completedIDs)
    {
        totalPuzzlesCompleted = count;
        completedPuzzlesThisSession.Clear();

        foreach (string id in completedIDs)
        {
            completedPuzzlesThisSession.Add(id);
        }

        DebugLog($"Restored puzzle count: {totalPuzzlesCompleted}");

        // Fire event so NPC updates state
        OnPuzzleCompleted?.Invoke(totalPuzzlesCompleted, null);
    }

    /// <summary>
    /// Reset for new game
    /// </summary>
    public void ResetPuzzles()
    {
        totalPuzzlesCompleted = 0;
        completedPuzzlesThisSession.Clear();
        sabotagedOnceIDs.Clear();
        DebugLog("Puzzles reset for new game");

        // Fire event with 0 count
        OnPuzzleCompleted?.Invoke(0, null);
        OnPuzzlesReset?.Invoke();
    }

    public int GetTotalPuzzlesCompleted() => totalPuzzlesCompleted;

    #region ISaveable Implementation

    public string SaveID => "puzzle_manager";

    public void SaveState(SaveData saveData)
    {
        // No-op: totalPuzzlesCompleted is fully derivable from
        // saveData.completedPuzzleIDs.Count, already gathered by each
        // individual puzzle script's own SaveState. Intentionally not
        // duplicating that data here to avoid the two counts drifting apart.
    }

    public void LoadState(SaveData saveData)
    {
        RestorePuzzleCount(saveData.completedPuzzleIDs.Count, saveData.completedPuzzleIDs);
    }

    #endregion

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            GameLog.Log($"[PuzzleManager] {message}");
        }
    }
}