using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized manager for tracking puzzle completions and firing events
/// </summary>
/// 
public class PuzzleManager : MonoSingleton<PuzzleManager>
{
    [Header("Puzzle Tracking")]
    [SerializeField] private int totalPuzzlesCompleted = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Events
    public System.Action<int> OnPuzzleCompleted; // Fires with total count

    // Track which puzzles have been completed this session
    private HashSet<string> completedPuzzlesThisSession = new HashSet<string>();

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
        OnPuzzleCompleted?.Invoke(totalPuzzlesCompleted);
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
        OnPuzzleCompleted?.Invoke(totalPuzzlesCompleted);
    }

    /// <summary>
    /// Reset for new game
    /// </summary>
    public void ResetPuzzles()
    {
        totalPuzzlesCompleted = 0;
        completedPuzzlesThisSession.Clear();
        DebugLog("Puzzles reset for new game");

        // Fire event with 0 count
        OnPuzzleCompleted?.Invoke(0);
    }

    public int GetTotalPuzzlesCompleted() => totalPuzzlesCompleted;

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[PuzzleManager] {message}");
        }
    }
}