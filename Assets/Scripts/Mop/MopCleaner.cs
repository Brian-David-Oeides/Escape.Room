using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MopCleaner : MonoBehaviour
{
    [Header("Cleaning Settings")]
    [Tooltip("How long must mop stay in puddle to clean it")]
    [SerializeField] private float cleaningDuration = 2f;

    [Tooltip("Mop head collider that detects puddles")]
    [SerializeField] private Collider mopHeadCollider;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem waterAbsorbParticles;

    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;
    private bool isCleaningInProgress = false;
    private float cleaningTimer = 0f;
    private PuddleHazard currentPuddle = null;
    private bool hasCleanedThisFrame = false;
    private Vector3 originalPuddleScale;

    private void Start()
    {
        GameLog.Log("=== MOP CLEANER START ===");

        // Setup grab interactable
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            //GameLog.Log($"[MopCleaner] ✓ Found XRGrabInteractable");
            //GameLog.Log($"[MopCleaner] Interaction Layer Mask: {grabInteractable.interactionLayers.value}");
            //GameLog.Log($"[MopCleaner] Colliders count: {grabInteractable.colliders.Count}");

            for (int i = 0; i < grabInteractable.colliders.Count; i++)
            {
                var col = grabInteractable.colliders[i];
                GameLog.Log($"[MopCleaner] Collider {i}: {col.name} | GameObject Layer: {LayerMask.LayerToName(col.gameObject.layer)} | IsTrigger: {col.isTrigger}");
            }

            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
        else
        {
            GameLog.LogError("[MopCleaner] ✗ XRGrabInteractable NOT FOUND!");
        }

        GameLog.Log($"[MopCleaner] This GameObject Layer: {LayerMask.LayerToName(gameObject.layer)}");
        GameLog.Log("=== MOP CLEANER READY ===");
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        GameLog.Log("[MopCleaner] Mop grabbed");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        StopCleaning();
        GameLog.Log("[MopCleaner] Mop released");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit a puddle
        PuddleHazard puddle = other.GetComponent<PuddleHazard>();
        if (puddle != null && puddle.IsPuddlePresent() && isGrabbed)
        {
            currentPuddle = puddle;
            StartCleaning();
            GameLog.Log("[MopCleaner] Mop entered puddle - starting cleaning");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Prevent multiple colliders from advancing timer in same frame
        if (hasCleanedThisFrame) return;

        if (!isGrabbed || currentPuddle == null || !isCleaningInProgress) return;

        // Check if this is still the current puddle
        PuddleHazard puddle = other.GetComponent<PuddleHazard>();
        if (puddle != currentPuddle) return;

        // Mark that we've cleaned this frame
        hasCleanedThisFrame = true;

        // Update cleaning progress ONCE per frame
        cleaningTimer += Time.deltaTime;

        // Calculate shrink percentage (0 to 1)
        float shrinkProgress = cleaningTimer / cleaningDuration;

        // Gradually scale down the puddle visual
        if (currentPuddle != null)
        {
            GameObject puddleVisual = currentPuddle.transform.Find("PuddleVisual")?.gameObject;
            if (puddleVisual != null)
            {
                // Lerp from original scale to zero
                Vector3 targetScale = Vector3.zero;
                puddleVisual.transform.localScale = Vector3.Lerp(originalPuddleScale, targetScale, shrinkProgress);
            }
        }

        GameLog.Log($"[MopCleaner] Cleaning progress: {cleaningTimer:F2}/{cleaningDuration} - Shrink: {shrinkProgress:F2}");

        // Check if cleaning is complete
        if (cleaningTimer >= cleaningDuration)
        {
            CompleteCleaning();
        }
    }

    private void LateUpdate()
    {
        // Reset flag at end of frame
        hasCleanedThisFrame = false;
    }

    private void OnTriggerExit(Collider other)
    {
        PuddleHazard puddle = other.GetComponent<PuddleHazard>();
        if (puddle == currentPuddle)
        {
            StopCleaning();
            GameLog.Log("[MopCleaner] Mop left puddle - cleaning stopped");
        }
    }

    private void StartCleaning()
    {
        isCleaningInProgress = true;
        cleaningTimer = 0f;

        // Store the original scale when cleaning starts
        if (currentPuddle != null)
        {
            GameObject puddleVisual = currentPuddle.transform.Find("PuddleVisual")?.gameObject;
            if (puddleVisual != null)
            {
                originalPuddleScale = puddleVisual.transform.localScale;
            }
        }

        // Start water absorb particles
        if (waterAbsorbParticles != null)
        {
            waterAbsorbParticles.Play();
        }

        GameLog.Log("[MopCleaner] Cleaning started");
    }

    private void StopCleaning()
    {
        isCleaningInProgress = false;
        cleaningTimer = 0f;
        currentPuddle = null;

        // Stop particles
        if (waterAbsorbParticles != null)
        {
            waterAbsorbParticles.Stop();
        }
    }

    private void CompleteCleaning()
    {
        if (currentPuddle != null)
        {
            currentPuddle.RemovePuddle();
            GameLog.Log("[MopCleaner] ✓ Puddle cleaned successfully!");
        }

        StopCleaning();
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
}
