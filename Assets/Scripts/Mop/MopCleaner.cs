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

    private void Start()
    {
        Debug.Log("=== MOP CLEANER START ===");

        // Setup grab interactable
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            //Debug.Log($"[MopCleaner] ✓ Found XRGrabInteractable");
            //Debug.Log($"[MopCleaner] Interaction Layer Mask: {grabInteractable.interactionLayers.value}");
            //Debug.Log($"[MopCleaner] Colliders count: {grabInteractable.colliders.Count}");

            for (int i = 0; i < grabInteractable.colliders.Count; i++)
            {
                var col = grabInteractable.colliders[i];
                Debug.Log($"[MopCleaner] Collider {i}: {col.name} | GameObject Layer: {LayerMask.LayerToName(col.gameObject.layer)} | IsTrigger: {col.isTrigger}");
            }

            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
        else
        {
            Debug.LogError("[MopCleaner] ✗ XRGrabInteractable NOT FOUND!");
        }

        Debug.Log($"[MopCleaner] This GameObject Layer: {LayerMask.LayerToName(gameObject.layer)}");
        Debug.Log("=== MOP CLEANER READY ===");
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        Debug.Log("[MopCleaner] Mop grabbed");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        StopCleaning();
        Debug.Log("[MopCleaner] Mop released");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit a puddle
        PuddleHazard puddle = other.GetComponent<PuddleHazard>();
        if (puddle != null && puddle.IsPuddlePresent() && isGrabbed)
        {
            currentPuddle = puddle;
            StartCleaning();
            Debug.Log("[MopCleaner] Mop entered puddle - starting cleaning");
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
        Debug.Log($"[MopCleaner] Cleaning progress: {cleaningTimer:F2}/{cleaningDuration}");

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
            Debug.Log("[MopCleaner] Mop left puddle - cleaning stopped");
        }
    }

    private void StartCleaning()
    {
        isCleaningInProgress = true;
        cleaningTimer = 0f;

        // Start water absorb particles
        if (waterAbsorbParticles != null)
        {
            waterAbsorbParticles.Play();
        }

        Debug.Log("[MopCleaner] Cleaning started");
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
            Debug.Log("[MopCleaner] ✓ Puddle cleaned successfully!");
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
