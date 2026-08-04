using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chain cutting puzzle with save system integration.
/// Inherits from PuzzleBase for automatic ISaveable implementation.
/// Chain can only be cut once and state persists across sessions.
/// </summary>
/// 

public class ChainCutterReciever : PuzzleBase // CHANGE 1: Inherit from PuzzleBase instead of MonoBehaviour
{
    [Header("Chain References")]
    [Tooltip("Assign the real chain Rigidbody here")]
    public Rigidbody chainBody;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Chain Behavior")]
    [Tooltip("Delay before disabling after hit")]
    public float disableDelay = 2f;

    private bool _hasBeenCut = false;  // Keep for internal logic

    protected override void Start()  // CHANGE 2: Add override and call base.Start()
    {
        base.Start(); // CRITICAL: Loads saved completion state!

        // If already cut from save, apply cut state immediately
        if (isCompleted)
        {
            _hasBeenCut = true;
            ApplyCutState(skipAnimation: true);
        }
    }

    /// <summary>
    /// Called by BoltCutter when it collides with the chain
    /// </summary>
    public void CutChain()
    {
        // CHANGE 3: Prevent re-cutting if already completed
        if (isCompleted || _hasBeenCut)
        {
            DebugLog("Chain already cut - ignoring");
            return;
        }

        _hasBeenCut = true;

        DebugLog("Chain cut by bolt cutters!");

        // Apply the cut (enable physics)
        ApplyCutState(skipAnimation: false);

        // CHANGE 4: Call CompletePuzzle() to save state
        CompletePuzzle();
    }

    /// <summary>
    /// Apply the cut state to the chain (enable physics, play audio)
    /// </summary>
    private void ApplyCutState(bool skipAnimation)
    {
        if (chainBody != null)
        {
            chainBody.isKinematic = false;
            chainBody.useGravity = true;

            if (audioSource != null && !skipAnimation)
            {
                audioSource.Play();
            }

            DebugLog($"Chain physics enabled (skipAnimation: {skipAnimation})");
        }
        else
        {
            GameLog.LogWarning("[ChainCutterReceiver] Chain Rigidbody not assigned!");
        }
    }

    /// <summary>
    /// Override CompletePuzzle to log chain cut completion
    /// </summary>
    public override void CompletePuzzle()
    {
        DebugLog("Chain cutting puzzle completed!");

        // Call base to handle save system and fire OnPuzzleCompleted event
        base.CompletePuzzle();
    }

    /// <summary>
    /// Override ApplyCompletedStateVisuals to apply cut state when loading
    /// </summary>
    protected override void ApplyCompletedStateVisuals()
    {
        // Call base to handle colliders/renderers
        base.ApplyCompletedStateVisuals();

        // Apply cut state (chain should already be on floor or disabled)
        _hasBeenCut = true;
        ApplyCutState(skipAnimation: true);

        DebugLog("Chain cut state restored from save");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // CHANGE 5: Only process collision if chain was just cut (not loaded from save)
        if (_hasBeenCut && collision.gameObject.CompareTag("Floor"))
        {
            DebugLog("Chain hit the floor. Will disable soon...");
            StartCoroutine(DisableChainAfterDelay());
        }
    }

    private IEnumerator DisableChainAfterDelay()
    {
        yield return new WaitForSeconds(disableDelay);

        // CHANGE 6: Don't use SetActive(false) - use base class method instead
        // Keep GameObject active but hide visuals
        if (chainBody != null)
        {
            // Disable the rigidbody so it stops simulating physics
            chainBody.isKinematic = true;
            chainBody.useGravity = false;

            // Hide the chain visually
            MeshRenderer renderer = chainBody.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            // Disable collider
            Collider col = chainBody.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            DebugLog("Chain disabled (hidden, physics off, collider off, GameObject active)");
        }

        // Also hide this receiver object
        MeshRenderer receiverRenderer = GetComponent<MeshRenderer>();
        if (receiverRenderer != null)
        {
            receiverRenderer.enabled = false;
        }

        Collider receiverCollider = GetComponent<Collider>();
        if (receiverCollider != null)
        {
            receiverCollider.enabled = false;
        }
    }
}