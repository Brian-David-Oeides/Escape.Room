using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HammerStrikeTrigger : PuzzleBase
{
    [Tooltip("The Rigidbody (e.g., cabinet door) that should be un-kinematic on hammer impact.")]
    public Rigidbody targetRigidbody;

    [Tooltip("HingeJoint on targetRigidbody. Used to kick the door open along its actual hinge axis once unlocked, since gravity alone only produces a weak swing on this geometry.")]
    public HingeJoint targetHinge;

    [Tooltip("Torque impulse applied along the hinge axis (opening direction) on unlock, to make the door swing open decisively instead of creeping.")]
    public float openTorqueImpulse = 8f;

    [Tooltip("Tag of the hammer object.")]
    public string hammerTag = "Hammer";

    [Tooltip("Reference to the XR Socket Interactor holding the stake (on the cabinet).")]
    public XRSocketInteractor stakeSocket;

    private bool _hasTriggered = false;

    protected override void Start()
    {
        base.Start(); // CRITICAL: Loads saved completion state!

        // If already triggered from save, apply unlocked state immediately
        if (isCompleted)
        {
            _hasTriggered = true;
            ApplyUnlockedState(skipAnimation: true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Prevent re-triggering if already completed
        if (isCompleted || _hasTriggered)
        {
            return;
        }

        if (other.CompareTag(hammerTag) && targetRigidbody != null)
        {
            DebugLog("Hammer struck! Unlocking cabinet door.");

            _hasTriggered = true;

            // Apply the unlocked state
            ApplyUnlockedState(skipAnimation: false);

            // Complete the puzzle (saves state)
            CompletePuzzle();
        }
        else if (IsValidFailedAttempt(other)) // NEW: Track only legitimate failed attempts
        {
            ClueManager.Instance?.RegisterFailedAttempt(puzzleID);
            DebugLog($"Wrong object hit stake: {other.name} (needed hammer)");
        }
    }

    /// <summary>
    /// Apply the unlocked state to the cabinet door
    /// </summary>
    private void ApplyUnlockedState(bool skipAnimation)
    {
        // Step 1: Unlock cabinet door
        if (targetRigidbody != null)
        {
            targetRigidbody.isKinematic = false;

            // Gravity alone only produces a weak swing on this hinge geometry.
            // Kick it open along the hinge's own axis (opposite the axis direction,
            // matching the negative-angle side the Limits now open toward) so it
            // swings open decisively rather than creeping.
            if (targetHinge != null)
            {
                Vector3 worldHingeAxis = targetHinge.transform.TransformDirection(targetHinge.axis).normalized;
                targetRigidbody.AddTorque(-worldHingeAxis * openTorqueImpulse, ForceMode.Impulse);
            }
        }

        // Step 2: Disable the XR Socket Interactor to release stake
        if (stakeSocket != null)
        {
            stakeSocket.enabled = false;
        }

        DebugLog($"Cabinet door unlocked (skipAnimation: {skipAnimation})");
    }

    /// <summary>
    /// Override CompletePuzzle to log hammer strike completion
    /// </summary>
    public override void CompletePuzzle()
    {
        DebugLog("Hammer strike puzzle completed!");

        // Call base to handle save system and fire OnPuzzleCompleted event
        base.CompletePuzzle();
    }

    /// <summary>
    /// Override ApplyCompletedStateVisuals to apply unlocked state when loading
    /// </summary>
    protected override void ApplyCompletedStateVisuals()
    {
        // Call base to handle colliders/renderers
        base.ApplyCompletedStateVisuals();

        // Apply unlocked state
        _hasTriggered = true;
        ApplyUnlockedState(skipAnimation: true);

        DebugLog("Cabinet door unlocked state restored from save");
    }

    /// <summary>
    /// Check if collision represents a valid failed attempt
    /// </summary>
    private bool IsValidFailedAttempt(Collider other)
    {
        // Ignore VR interaction colliders
        if (other.name.Contains("Interactor") ||
            other.name.Contains("Socket") ||
            other.CompareTag("Player"))
        {
            return false;
        }

        // Only count objects with Rigidbody (actual physics objects)
        return other.attachedRigidbody != null;
    }
}

