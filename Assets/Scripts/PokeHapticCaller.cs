/*
 * PokeHapticCaller.cs
 * 
 * Copyright © 2025 Brian David
 * All Rights Reserved
 * 
 * A companion script that finds and calls haptic methods on hand controllers
 * specifically for poke interactions. Attach to objects that need to trigger 
 * haptic feedback when poked.
 * 
 * Adapted from HapticCaller.cs for poke interactions
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PokeHapticCaller : MonoBehaviour
{
    private HandHaptics leftHandHaptics;
    private HandHaptics rightHandHaptics;

    [Header("Poke Interaction Settings")]
    [Tooltip("The XR Simple Interactable that's being poked")]
    public XRSimpleInteractable pokeInteractable;

    [Header("Haptic Timing")]
    [Tooltip("Trigger haptics on hover enter (finger gets close)")]
    public bool hapticOnHover = false;

    [Tooltip("Trigger haptics on select enter (finger pokes through)")]
    public bool hapticOnPoke = true;

    [Tooltip("Trigger haptics on select exit (finger pulls away)")]
    public bool hapticOnRelease = false;

    [Header("Haptic Types")]
    [Tooltip("Use gentle haptic for hover")]
    public bool gentleHoverFeedback = true;

    [Tooltip("Use success pattern for successful poke")]
    public bool successPokePattern = true;

    [Header("Haptic Timing & Intensity")]
    [Range(0.005f, 0.2f)]
    [Tooltip("Duration for hover haptic feedback (seconds) - try 0.02s for ultra-short")]
    public float hoverDuration = 0.02f;

    [Range(0.0f, 1.0f)]
    [Tooltip("Intensity for hover haptic feedback")]
    public float hoverIntensity = 0.1f;

    [Range(0.005f, 0.3f)]
    [Tooltip("Duration for poke haptic feedback (seconds) - try 0.025s for crisp tap")]
    public float pokeDuration = 0.025f;

    [Range(0.0f, 1.0f)]
    [Tooltip("Intensity for poke haptic feedback")]
    public float pokeIntensity = 0.8f;

    [Range(0.005f, 0.2f)]
    [Tooltip("Duration for release haptic feedback (seconds) - try 0.015s for quick release")]
    public float releaseDuration = 0.015f;

    [Range(0.0f, 1.0f)]
    [Tooltip("Intensity for release haptic feedback")]
    public float releaseIntensity = 0.15f;

    [Tooltip("Enable debug logs")]
    public bool debugMode = true;

    private void Start()
    {
        // Find all hand haptic scripts in the scene
        HandHaptics[] allHandHaptics = FindObjectsOfType<HandHaptics>();

        foreach (HandHaptics haptic in allHandHaptics)
        {
            if (haptic.isLeftHand)
            {
                leftHandHaptics = haptic;
                if (debugMode) Debug.Log("Found left hand haptics on: " + haptic.gameObject.name);
            }
            else
            {
                rightHandHaptics = haptic;
                if (debugMode) Debug.Log("Found right hand haptics on: " + haptic.gameObject.name);
            }
        }

        // If poke interactable wasn't assigned, try to find it on this object
        if (pokeInteractable == null)
        {
            pokeInteractable = GetComponent<XRSimpleInteractable>();
            if (pokeInteractable != null && debugMode)
            {
                Debug.Log("Found XR Simple Interactable on: " + gameObject.name);
            }
        }

        // Subscribe to poke interaction events
        SetupPokeEvents();
    }

    private void SetupPokeEvents()
    {
        if (pokeInteractable == null)
        {
            if (debugMode) Debug.LogWarning("No XR Simple Interactable found for poke haptics");
            return;
        }

        // Subscribe to interaction events
        if (hapticOnHover)
        {
            pokeInteractable.hoverEntered.AddListener(OnPokeHoverEntered);
        }

        if (hapticOnPoke)
        {
            pokeInteractable.selectEntered.AddListener(OnPokeSelectEntered);
        }

        if (hapticOnRelease)
        {
            pokeInteractable.selectExited.AddListener(OnPokeSelectExited);
        }

        if (debugMode) Debug.Log("Poke haptic events setup complete");
    }

    private void OnPokeHoverEntered(HoverEnterEventArgs args)
    {
        if (debugMode) Debug.Log("Poke hover entered - triggering gentle haptic");

        if (gentleHoverFeedback)
        {
            TriggerPokeHandHaptic(args.interactorObject, hoverIntensity, hoverDuration);
        }
        else
        {
            TriggerPokeHandHaptic(args.interactorObject, pokeIntensity, pokeDuration);
        }
    }

    private void OnPokeSelectEntered(SelectEnterEventArgs args)
    {
        if (debugMode) Debug.Log("Poke select entered - triggering poke haptic");

        if (successPokePattern)
        {
            TriggerPokeHandSuccessHaptic(args.interactorObject);
        }
        else
        {
            TriggerPokeHandHaptic(args.interactorObject, pokeIntensity, pokeDuration);
        }
    }

    private void OnPokeSelectExited(SelectExitEventArgs args)
    {
        if (debugMode) Debug.Log("Poke select exited - triggering release haptic");
        TriggerPokeHandHaptic(args.interactorObject, releaseIntensity, releaseDuration);
    }

    // Main method to trigger haptic on the hand that's doing the poking
    private void TriggerPokeHandHaptic(IXRInteractor interactor, float intensity = -1, float duration = -1)
    {
        if (interactor == null) return;

        MonoBehaviour interactorObj = interactor as MonoBehaviour;
        if (interactorObj == null) return;

        bool isLeftHand = DetermineIfLeftHand(interactorObj.gameObject);

        if (isLeftHand && leftHandHaptics != null)
        {
            if (debugMode) Debug.Log("Triggering left hand poke haptic");

            if (intensity >= 0 && duration >= 0)
                leftHandHaptics.TriggerHaptic(intensity, duration);
            else
                leftHandHaptics.TriggerHaptic();
        }
        else if (!isLeftHand && rightHandHaptics != null)
        {
            if (debugMode) Debug.Log("Triggering right hand poke haptic");

            if (intensity >= 0 && duration >= 0)
                rightHandHaptics.TriggerHaptic(intensity, duration);
            else
                rightHandHaptics.TriggerHaptic();
        }
        else
        {
            if (debugMode) Debug.LogWarning("Could not determine poke hand or haptics not found");
        }
    }

    // Trigger success haptic pattern for the poking hand
    private void TriggerPokeHandSuccessHaptic(IXRInteractor interactor)
    {
        if (interactor == null) return;

        MonoBehaviour interactorObj = interactor as MonoBehaviour;
        if (interactorObj == null) return;

        bool isLeftHand = DetermineIfLeftHand(interactorObj.gameObject);

        if (isLeftHand && leftHandHaptics != null)
        {
            if (debugMode) Debug.Log("Triggering left hand success haptic");
            leftHandHaptics.TriggerSuccessHaptic();
        }
        else if (!isLeftHand && rightHandHaptics != null)
        {
            if (debugMode) Debug.Log("Triggering right hand success haptic");
            rightHandHaptics.TriggerSuccessHaptic();
        }
    }

    // Helper method to determine if a GameObject is a left hand
    private bool DetermineIfLeftHand(GameObject obj)
    {
        if (obj == null) return false;

        // Method 1: Check if it's the same object as the left haptics
        if (leftHandHaptics != null && obj == leftHandHaptics.gameObject)
            return true;
        if (rightHandHaptics != null && obj == rightHandHaptics.gameObject)
            return false;

        // Method 2: Check name for "left"/"right"
        string objName = obj.name.ToLower();
        if (objName.Contains("left")) return true;
        if (objName.Contains("right")) return false;

        // Method 3: Check all parents
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            string parentName = parent.name.ToLower();
            if (parentName.Contains("left")) return true;
            if (parentName.Contains("right")) return false;
            parent = parent.parent;
        }

        // Method 4: Check distance to known controllers
        Transform leftTransform = leftHandHaptics?.transform;
        Transform rightTransform = rightHandHaptics?.transform;

        if (leftTransform != null && rightTransform != null)
        {
            float distToLeft = Vector3.Distance(obj.transform.position, leftTransform.position);
            float distToRight = Vector3.Distance(obj.transform.position, rightTransform.position);
            return distToLeft < distToRight;
        }

        return false;
    }

    // Public methods for manual triggering (can be called from Unity Events)
    public void TriggerLeftHandPoke()
    {
        if (leftHandHaptics != null)
            leftHandHaptics.TriggerHaptic();
        else if (debugMode)
            Debug.LogWarning("Left hand haptics not found");
    }

    public void TriggerRightHandPoke()
    {
        if (rightHandHaptics != null)
            rightHandHaptics.TriggerHaptic();
        else if (debugMode)
            Debug.LogWarning("Right hand haptics not found");
    }

    public void TriggerBothHandsPoke()
    {
        TriggerLeftHandPoke();
        TriggerRightHandPoke();
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (pokeInteractable != null)
        {
            pokeInteractable.hoverEntered.RemoveListener(OnPokeHoverEntered);
            pokeInteractable.selectEntered.RemoveListener(OnPokeSelectEntered);
            pokeInteractable.selectExited.RemoveListener(OnPokeSelectExited);
        }
    }
}