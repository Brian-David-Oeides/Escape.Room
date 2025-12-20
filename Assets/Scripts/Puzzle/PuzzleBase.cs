using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for all one-time completion puzzles in the game.
/// Implements ISaveable with persistent accumulation pattern (no SetActive bug).
/// Inherit from this class for puzzles that complete once and stay completed.
/// 
/// USAGE:
/// 1. Inherit from PuzzleBase
/// 2. Set unique puzzleID in inspector
/// 3. Override CompletePuzzle() to add your puzzle logic
/// 4. Call CompletePuzzle() when puzzle is solved
/// 5. Wire OnPuzzleCompleted UnityEvent in inspector to enable/disable objects
/// </summary>
public abstract class PuzzleBase : MonoBehaviour, ISaveable
{
    [Header("Save System")]
    [Tooltip("Unique ID for this puzzle - MUST be unique across all puzzles in game")]
    [SerializeField] public string puzzleID = "puzzle_base_001";

    [Header("Hint System")]
    [Tooltip("Number of failed attempts before hint becomes available (0 = use ClueManager default)")]
    [Range(0, 10)]
    public int hintThreshold = 0; // 0 means use global default

    [Header("Puzzle State")]
    [Tooltip("Is this puzzle already completed? (set by save system)")]
    [SerializeField] protected bool isCompleted = false;

    [Header("Puzzle Events")]
    [Tooltip("Fired when puzzle is completed for the first time")]
    [SerializeField] protected UnityEvent OnPuzzleCompleted;

    [Tooltip("Fired when puzzle state is loaded as already completed")]
    [SerializeField] protected UnityEvent OnLoadCompletedState;

    [Header("Visual Feedback")]
    [Tooltip("Disable these components when puzzle is completed (optional)")]
    [SerializeField] protected Collider[] collidersToDisable;

    [Tooltip("Hide these renderers when puzzle is completed (optional)")]
    [SerializeField] protected MeshRenderer[] renderersToHide;

    [Header("Debug")]
    [SerializeField] protected bool showDebugLogs = true;

    public string SaveID => puzzleID;

    protected virtual void Start()
    {
        // If already completed from save, apply completed state immediately
        if (isCompleted)
        {
            DebugLog($"Puzzle {puzzleID} loaded as already completed");
            ApplyCompletedStateVisuals();
        }
    }

    /// <summary>
    /// Call this method when the puzzle is solved
    /// Override to add custom completion logic BEFORE base.CompletePuzzle()
    /// </summary>
    public virtual void CompletePuzzle()
    {
        if (isCompleted)
        {
            DebugLog($"Puzzle {puzzleID} already completed - ignoring duplicate completion");
            return;
        }

        isCompleted = true;
        DebugLog($"Puzzle {puzzleID} COMPLETED!");

        // Apply visual changes
        ApplyCompletedStateVisuals();

        // Fire completion event (enables objects, triggers next puzzle, etc.)
        OnPuzzleCompleted?.Invoke();
    }

    /// <summary>
    /// Apply visual state for completed puzzle
    /// ARCHITECTURAL FIX: Does NOT use SetActive(false) to avoid save bug
    /// Instead disables components while keeping GameObject active for save system
    /// </summary>
    protected virtual void ApplyCompletedStateVisuals()
    {
        // Disable colliders (prevents interaction)
        if (collidersToDisable != null)
        {
            foreach (var col in collidersToDisable)
            {
                if (col != null) col.enabled = false;
            }
        }

        // Hide renderers (makes invisible)
        if (renderersToHide != null)
        {
            foreach (var renderer in renderersToHide)
            {
                if (renderer != null) renderer.enabled = false;
            }
        }

        // GameObject stays ACTIVE in scene hierarchy for save system ✓
        DebugLog($"{puzzleID} visual state applied (colliders/renderers disabled, GameObject active)");
    }

    #region ISaveable Implementation

    public void SaveState(SaveData saveData)
    {
        // If completed, add to completed puzzles list
        if (isCompleted && !saveData.completedPuzzleIDs.Contains(puzzleID))
        {
            saveData.completedPuzzleIDs.Add(puzzleID);
            DebugLog($"Saved completed state for puzzle {puzzleID}");
        }
    }

    public void LoadState(SaveData saveData)
    {
        // Debug: Show what's in the completed list
        string completedList = string.Join(", ", saveData.completedPuzzleIDs);
        DebugLog($"LoadState called for {puzzleID}. Completed list: [{completedList}]");

        // Check if this puzzle was completed in the save
        bool wasCompleted = saveData.completedPuzzleIDs.Contains(puzzleID);

        if (wasCompleted && !isCompleted)
        {
            // Puzzle was completed in save but not in current state
            isCompleted = true;
            DebugLog($"Loading completed state for {puzzleID}");

            // Apply visual state
            ApplyCompletedStateVisuals();

            // Fire load event (for objects that need to be enabled on load)
            OnLoadCompletedState?.Invoke();
        }
        else if (wasCompleted && isCompleted)
        {
            DebugLog($"{puzzleID} already marked completed in scene and save");
        }
        else
        {
            DebugLog($"{puzzleID} was NOT completed - staying in unsolved state");
        }
    }

    #endregion

    #region Debug

    protected void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[PuzzleBase:{puzzleID}] {message}");
        }
    }

    #endregion

    #region Editor Helpers

#if UNITY_EDITOR
    /// <summary>
    /// Validate puzzle ID is set in editor
    /// </summary>
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(puzzleID) || puzzleID == "puzzle_base_001")
        {
            Debug.LogWarning($"[{gameObject.name}] Puzzle ID is not set or using default! Set a unique puzzleID in inspector.", this);
        }
    }
#endif

    #endregion
}
