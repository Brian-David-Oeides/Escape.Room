/*
 * HapticCaller.cs
 * 
 * Copyright © 2025 Brian David
 * All Rights Reserved
 *
 * A companion script that finds and calls haptic methods on hand controllers.
 * Attach to objects that need to trigger haptic feedback via Unity Events.
 * 
 * Created: May 13, 2025
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HapticCaller : MonoBehaviour
{
    private HandHaptics leftHandHaptics;
    private HandHaptics rightHandHaptics;

    [Tooltip("The wrench object that's being interacted with")]
    public XRGrabInteractable xRGrabbableObject;

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

        // If wrench wasn't assigned in inspector, try to find it through the SocketRotator
        if (xRGrabbableObject == null)
        {
            SocketRotator socketRotator = GetComponent<SocketRotator>();
            if (socketRotator != null)
            {
                // Access the public wrench field from SocketRotator
                xRGrabbableObject = socketRotator.wrench;

                if (xRGrabbableObject != null && debugMode)
                {
                    Debug.Log("Found wrench from SocketRotator: " + xRGrabbableObject.name);
                }
            }

            // If still null, find any XRGrabInteractable
            if (xRGrabbableObject == null)
            {
                XRGrabInteractable[] interactables = FindObjectsOfType<XRGrabInteractable>();
                if (interactables.Length > 0)
                {
                    xRGrabbableObject = interactables[0];
                    if (debugMode) Debug.Log("Found first XRGrabInteractable in scene: " + xRGrabbableObject.name);
                }
            }
        }
    }

    // This is the method to connect to Unity Events (like your SocketRotator.onFullyTurned)
    public void TriggerActiveHandHaptic()
    {
        if (xRGrabbableObject == null)
        {
            if (debugMode) Debug.LogWarning("Wrench object is not assigned");
            TriggerBothHands();
            return;
        }

        // Check if the wrench is currently being interacted with
        if (xRGrabbableObject.isSelected)
        {
            IXRSelectInteractor interactor = xRGrabbableObject.firstInteractorSelecting;
            if (interactor != null)
            {
                MonoBehaviour interactorObj = interactor as MonoBehaviour;
                if (interactorObj != null)
                {
                    bool isLeftHand = DetermineIfLeftHand(interactorObj.gameObject);

                    if (isLeftHand && leftHandHaptics != null)
                    {
                        if (debugMode) Debug.Log("Triggering left hand haptic");
                        leftHandHaptics.TriggerSuccessHaptic();
                        return;
                    }
                    else if (!isLeftHand && rightHandHaptics != null)
                    {
                        if (debugMode) Debug.Log("Triggering right hand haptic");
                        rightHandHaptics.TriggerSuccessHaptic();
                        return;
                    }
                }
            }
        }

        // If the wrench isn't being interacted with or we couldn't determine the hand,
        // try to find the closest controller
        Transform closestController = FindClosestController();
        if (closestController != null)
        {
            bool isLeftHand = DetermineIfLeftHand(closestController.gameObject);

            if (isLeftHand && leftHandHaptics != null)
            {
                if (debugMode) Debug.Log("Triggering left hand haptic via proximity");
                leftHandHaptics.TriggerSuccessHaptic();
                return;
            }
            else if (!isLeftHand && rightHandHaptics != null)
            {
                if (debugMode) Debug.Log("Triggering right hand haptic via proximity");
                rightHandHaptics.TriggerSuccessHaptic();
                return;
            }
        }

        // If all approaches fail, trigger both hands as fallback
        if (debugMode) Debug.LogWarning("Could not determine which hand, triggering both");
        TriggerBothHands();
    }

    // Helper method to find which controller is closest to this object
    private Transform FindClosestController()
    {
        Transform leftController = leftHandHaptics?.transform;
        Transform rightController = rightHandHaptics?.transform;

        if (leftController == null && rightController == null) return null;
        if (leftController == null) return rightController;
        if (rightController == null) return leftController;

        float distToLeft = Vector3.Distance(transform.position, leftController.position);
        float distToRight = Vector3.Distance(transform.position, rightController.position);

        return (distToLeft < distToRight) ? leftController : rightController;
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

    // Method to trigger haptic on left hand
    public void TriggerLeftHand()
    {
        if (leftHandHaptics != null)
            leftHandHaptics.TriggerHaptic();
        else if (debugMode)
            Debug.LogWarning("Left hand haptics not found");
    }

    // Method to trigger haptic on right hand
    public void TriggerRightHand()
    {
        if (rightHandHaptics != null)
            rightHandHaptics.TriggerHaptic();
        else if (debugMode)
            Debug.LogWarning("Right hand haptics not found");
    }

    // Method to trigger haptic on both hands
    public void TriggerBothHands()
    {
        TriggerLeftHand();
        TriggerRightHand();
    }
}