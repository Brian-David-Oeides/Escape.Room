using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashLightSwitchActivator : PuzzleBase
{
    [Header("Raycast Settings")]
    public float detectionRange = 10f;
    public LayerMask detectionLayer = -1;

    [Header("References")]
    public Light torchLight;
    public Transform raycastOrigin;

    [Header("Switch Reference")]
    public GameObject targetSwitchObject; // Direct reference to the ToggleSwitchOnOff05 GameObject

    private bool _switchAlreadyActivated = false;

    protected override void Start()
    {
        base.Start(); // CRITICAL: Loads saved completion state!

        // If already activated from save, mark as complete
        if (isCompleted)
        {
            _switchAlreadyActivated = true;
            DebugLog("Switch already activated from save - detection disabled");
        }
    }

    private void Update()
    {
        if (torchLight != null && torchLight.enabled && !_switchAlreadyActivated)
        {
            PerformSwitchDetection();
        }
    }

    private void PerformSwitchDetection()
    {
        Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
        RaycastHit hit;

        Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * detectionRange, Color.red);

        if (Physics.Raycast(ray, out hit, detectionRange, detectionLayer))
        {
            //GameLog.Log($"Raycast hit: {hit.collider.name}");

            if (hit.collider.CompareTag("Switch") || hit.collider.name.Contains("PlugAndSwitch"))
            {
                GameLog.Log("Switch object detected!");
                EnableSwitchGameObject();
            }
        }
    }

    private void EnableSwitchGameObject()
    {
        // Prevent re-triggering if already completed
        if (isCompleted || _switchAlreadyActivated)
        {
            return;
        }

        if (targetSwitchObject != null)
        {
            // Enable the XRSimpleInteractable component via SwitchToggle
            var switchToggle = targetSwitchObject.GetComponent<SwitchToggle>();
            if (switchToggle != null)
            {
                switchToggle.UnlockSwitch();
                _switchAlreadyActivated = true;

                DebugLog("Switch permanently unlocked by flashlight beam!");

                // Complete the puzzle (saves state)
                CompletePuzzle();
            }
            else
            {
                GameLog.LogError("[FlashLightSwitchActivator] SwitchToggle component not found on targetSwitchObject!");
            }
        }
        else
        {
            GameLog.LogError("[FlashLightSwitchActivator] Target Switch GameObject reference is null!");
        }
    }

    /// <summary>
    /// Override CompletePuzzle to log flashlight activation completion
    /// </summary>
    public override void CompletePuzzle()
    {
        DebugLog("Flashlight switch activation puzzle completed!");

        // Call base to handle save system and fire OnPuzzleCompleted event
        base.CompletePuzzle();
    }

    /// <summary>
    /// Override ApplyCompletedStateVisuals to restore activated state when loading
    /// </summary>
    protected override void ApplyCompletedStateVisuals()
    {
        // Call base to handle colliders/renderers
        base.ApplyCompletedStateVisuals();

        // Mark as already activated
        _switchAlreadyActivated = true;

        DebugLog("Flashlight switch activation state restored from save");
    }
}
