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

    [Tooltip("The Cabinet's floor BoxCollider ('Cabinet Base floor'). The open door sweeps through this panel, so collision response against it is disabled - otherwise the solver pushes the door off its hinge arc every step and it never settles.")]
    [SerializeField] private Collider cabinetFloorCollider;

    [Tooltip("The Cabinet's side-wall BoxCollider ('Cabinet Base side'). Same reasoning as cabinetFloorCollider - the open door overlaps this panel across most of its swing.")]
    [SerializeField] private Collider cabinetSideCollider;

    [SerializeField] private AudioSource hammerAudioSource;
    [SerializeField] private AudioClip stakeHitClip;

    [Tooltip("Tag of the hammer object.")]
    public string hammerTag = "Hammer";

    [Tooltip("Reference to the XR Socket Interactor holding the stake (on the cabinet).")]
    public XRSocketInteractor stakeSocket;

    private bool _hasTriggered = false;

    private void Awake()
    {
        // Must run before Start(), since a save with this puzzle completed unlocks the
        // door there and it starts falling on the very next physics step.
        IgnoreCabinetShellCollisions();
    }

    /// <summary>
    /// Disable collision response between the cabinet door and the Cabinet's two static
    /// shell panels. The door's HingeJoint constrains it to a fixed arc, and that arc
    /// passes through both panels (measured: up to ~7mm into 'Cabinet Base side' and
    /// ~2mm into 'Cabinet Base floor' within the joint's own -90..-180 limit range).
    /// The contact solver pushes the door off the arc, the joint pulls it back, and
    /// neither wins - so the door jitters forever instead of coming to rest and sleeping.
    /// The joint's own m_EnableCollision:0 only covers the door vs its connected body
    /// ('Base'); these two panels are separate static colliders it does not protect.
    /// Same fix as BronzeKeyDrawerFollow and PlumbersWrenchCabinetRest.
    /// </summary>
    private void IgnoreCabinetShellCollisions()
    {
        if (targetRigidbody == null) return;

        foreach (Collider doorCollider in targetRigidbody.GetComponentsInChildren<Collider>(true))
        {
            // Triggers produce no collision response to suppress, and ignoring them here
            // would silently kill their trigger events too (the door's child StakeSocket
            // is a trigger) - so leave them alone.
            if (doorCollider.isTrigger) continue;

            if (cabinetFloorCollider != null)
                Physics.IgnoreCollision(doorCollider, cabinetFloorCollider, true);

            if (cabinetSideCollider != null)
                Physics.IgnoreCollision(doorCollider, cabinetSideCollider, true);
        }
    }

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

        if (other.CompareTag(hammerTag))
        {
            PlayStakeHitSound();
        }

        if (other.CompareTag(hammerTag) && targetRigidbody != null
            && stakeSocket != null && stakeSocket.hasSelection)
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
    /// Play the stake-hit sound. Fires on every hammer-stake contact while the
    /// puzzle is still unsolved, regardless of whether the stake is socketed -
    /// separate from (and does not gate) puzzle completion.
    /// </summary>
    private void PlayStakeHitSound()
    {
        if (hammerAudioSource != null && stakeHitClip != null)
        {
            hammerAudioSource.PlayOneShot(stakeHitClip);
        }
    }

    /// <summary>
    /// Apply the unlocked state to the cabinet door
    /// </summary>
    private void ApplyUnlockedState(bool skipAnimation)
    {
        // Unity resets an ignored collider pair whenever either collider is disabled and
        // re-enabled, so re-assert it at the moment the door actually starts moving.
        // Idempotent - Awake() has already done this once.
        IgnoreCabinetShellCollisions();

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
        // Only count attempts made while the stake is actually socketed -
        // a stray hit on the loose, unsocketed stake shouldn't consume
        // hint-tracking attempts.
        if (stakeSocket == null || !stakeSocket.hasSelection)
        {
            return false;
        }

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

