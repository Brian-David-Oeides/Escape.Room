using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Picture frame combination lock puzzle with ISaveable implementation.
/// Requires 3 frames to be placed in correct order to unlock.
/// 
/// INTEGRATION GUIDE:
/// This shows how to convert FrameLock.cs to use PuzzleBase.
/// 
/// STEPS TO INTEGRATE:
/// 1. Make FrameLock inherit from PuzzleBase instead of MonoBehaviour
/// 2. Remove the existing _onCheck UnityEvent (use OnPuzzleCompleted instead)
/// 3. In CheckCombination(), replace _onCheck?.Invoke() with CompletePuzzle()
/// 4. Set unique puzzleID in inspector (e.g., "framelock_library_001")
/// 5. Wire OnPuzzleCompleted event in inspector to unlock door/trigger next puzzle
/// 6. Assign collidersToDisable and renderersToHide arrays if needed
/// </summary>
public class FrameLockSaveable : PuzzleBase
{
    [Header("Frame Lock Settings")]
    [SerializeField] private int[] _lockCode = new int[] { 1, 2, 3 };
    [SerializeField] private List<int> _enteredCode = new List<int>();
    [SerializeField] private int _maxCodeLength = 3;

    [Header("Frame References")]
    [SerializeField] private List<FrameID> _framesInOrder = new List<FrameID>();

    [Header("Audio")]
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private AudioSource audioSource;

    protected override void Start()
    {
        base.Start(); // IMPORTANT: Call base to load saved state

        // Additional initialization if needed
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Called when a frame is placed in the lock
    /// </summary>
    public void OnFramePlaced(int frameID)
    {
        // Don't allow interaction if puzzle is already completed
        if (isCompleted)
        {
            DebugLog("Puzzle already completed - ignoring frame placement");
            return;
        }

        if (_enteredCode.Count < _maxCodeLength)
        {
            _enteredCode.Add(frameID);
            DebugLog($"Frame {frameID} placed. Code: [{string.Join(", ", _enteredCode)}]");

            // Check if code is complete
            if (_enteredCode.Count == _maxCodeLength)
            {
                CheckCombination();
            }
        }
    }

    /// <summary>
    /// Check if entered code matches lock code
    /// </summary>
    private void CheckCombination()
    {
        if (_enteredCode.SequenceEqual(_lockCode))
        {
            DebugLog("CORRECT COMBINATION!");

            // Play unlock sound
            if (unlockSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }

            // INTEGRATION: Replace _onCheck?.Invoke() with:
            CompletePuzzle(); // This handles save system + fires OnPuzzleCompleted event
        }
        else
        {
            DebugLog($"Wrong combination: [{string.Join(", ", _enteredCode)}]");
            ResetCode();
        }
    }

    /// <summary>
    /// Reset the entered code
    /// </summary>
    public void ResetCode()
    {
        if (isCompleted)
        {
            DebugLog("Puzzle completed - cannot reset");
            return;
        }

        _enteredCode.Clear();
        DebugLog("Code reset");
    }

    /// <summary>
    /// Override CompletePuzzle to add custom completion logic
    /// </summary>
    public override void CompletePuzzle()
    {
        // Add any custom logic BEFORE calling base
        DebugLog($"Frame lock unlocked with code: [{string.Join(", ", _lockCode)}]");

        // Call base to handle save system and fire events
        base.CompletePuzzle();

        // Add any custom logic AFTER completion if needed
        // (e.g., disable frame sockets, lock frames in place, etc.)
    }

    /// <summary>
    /// Override to add custom visual state handling
    /// </summary>
    protected override void ApplyCompletedStateVisuals()
    {
        // Call base to handle colliders/renderers
        base.ApplyCompletedStateVisuals();

        // Add custom visual changes
        // Example: Keep frames locked in place, disable frame sockets, etc.
        DebugLog("Frame lock visual state applied - frames locked in solved position");
    }
}

/*
 * MIGRATION GUIDE FROM ORIGINAL FRAMELOCK.CS:
 * 
 * BEFORE (Original FrameLock.cs):
 * -----------------------------------
 * public class FrameLock : MonoBehaviour
 * {
 *     [SerializeField] UnityEvent _onCheck;
 *     
 *     private void CheckCombination()
 *     {
 *         if (_enteredCode.SequenceEqual(_lockCode))
 *         {
 *             _onCheck?.Invoke(); // ❌ NO SAVE
 *         }
 *     }
 * }
 * 
 * AFTER (Using PuzzleBase):
 * -----------------------------------
 * public class FrameLock : PuzzleBase
 * {
 *     // Remove: [SerializeField] UnityEvent _onCheck;
 *     // Use: OnPuzzleCompleted (inherited from PuzzleBase)
 *     
 *     private void CheckCombination()
 *     {
 *         if (_enteredCode.SequenceEqual(_lockCode))
 *         {
 *             CompletePuzzle(); // ✅ SAVES + FIRES EVENT
 *         }
 *     }
 * }
 * 
 * INSPECTOR SETUP:
 * -----------------------------------
 * 1. Set Puzzle ID: "framelock_library_001"
 * 2. Wire OnPuzzleCompleted event → Enable door GameObject, trigger next puzzle, etc.
 * 3. (Optional) Wire OnLoadCompletedState event → Same as above if objects need enabling on load
 * 4. (Optional) Assign collidersToDisable → Frame socket colliders to prevent re-interaction
 * 5. (Optional) Assign renderersToHide → If puzzle object should disappear when solved
 */
