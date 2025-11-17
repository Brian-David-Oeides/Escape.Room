using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class HandPointController : MonoBehaviour
{
    [Header("XR Controller")]
    public ActionBasedController xRController;

    [Header("Animation")]
    public string pointParameterName = "Point";
    public string gripParameterName = "Grip"; // For grip state
    public string pinchParameterName = "Pinch"; // For pinch state

    private Animator handAnimator;
    private XRDirectInteractor directInteractor; // To detect hovered objects
    private XRRayInteractor rayInteractor; // To detect ray-grabbed objects

    private void Start()
    {
        // Get the Direct Interactor component for hover detection
        directInteractor = GetComponentInChildren<XRDirectInteractor>();
        rayInteractor = GetComponentInChildren<XRRayInteractor>();

        if (directInteractor == null)
        {
            Debug.LogError($"No XRDirectInteractor found on {gameObject.name}");
        }

        if (rayInteractor == null)
        {
            Debug.LogError($"No XRRayInteractor found on {gameObject.name}");
        }

        StartCoroutine(FindHandAnimatorAfterSpawn());
    }

    private IEnumerator FindHandAnimatorAfterSpawn()
    {
        yield return new WaitForSeconds(0.5f);

        bool isLeftController = gameObject.name.ToLower().Contains("left");

        Animator[] animators = FindObjectsOfType<Animator>();

        foreach (Animator anim in animators)
        {
            if (anim.runtimeAnimatorController != null)
            {
                bool isLeftHand = anim.runtimeAnimatorController.name.Contains("LeftHand");
                bool isRightHand = anim.runtimeAnimatorController.name.Contains("RightHand");

                if ((isLeftController && isLeftHand) || (!isLeftController && isRightHand))
                {
                    handAnimator = anim;
                    Debug.Log($"Found runtime hand: {anim.gameObject.name} for {gameObject.name}");
                    break;
                }
            }
        }

        if (handAnimator == null)
        {
            Debug.LogError($"Could not find runtime hand animator for {gameObject.name}");
        }
    }

    private void Update()
    {
        if (xRController != null && handAnimator != null)
        {
            // Get input states
            bool triggerPressed = xRController.activateInteractionState.active;
            bool gripPressed = xRController.selectInteractionState.active;

            // Check if hovering over a pinchable object
            bool isHoveringPinchable = IsHoveringPinchableObject() || IsGrabbingPinchableObject();

            // Smart grip: if hovering pinchable object, grip = pinch. Otherwise grip = fist
            if (gripPressed && isHoveringPinchable)
            {
                // Pinch small objects
                handAnimator.SetBool(pinchParameterName, true);
                handAnimator.SetBool(gripParameterName, false);
                handAnimator.SetBool(pointParameterName, false);
            }
            else if (gripPressed && !isHoveringPinchable)
            {
                // Normal fist for regular objects
                handAnimator.SetBool(pinchParameterName, false);
                handAnimator.SetBool(gripParameterName, true);
                handAnimator.SetBool(pointParameterName, false);
            }
            else if (triggerPressed)
            {
                // Point gesture
                handAnimator.SetBool(pinchParameterName, false);
                handAnimator.SetBool(gripParameterName, false);
                handAnimator.SetBool(pointParameterName, true);
            }
            else
            {
                // Open hand (nothing pressed)
                handAnimator.SetBool(pinchParameterName, false);
                handAnimator.SetBool(gripParameterName, false);
                handAnimator.SetBool(pointParameterName, false);
            }

            // Debug output
            if (triggerPressed || gripPressed)
            {
                // Debug.Log($"{gameObject.name} - Trigger: {triggerPressed}, Grip: {gripPressed}, Pinch: {isHoveringPinchable && gripPressed}, HoveringPinchable: {isHoveringPinchable}");
            }
        }
    }

    private bool IsHoveringPinchableObject()
    {
        if (directInteractor == null) return false;

        // Check if we're hovering over any interactables using the new API
        foreach (var target in directInteractor.interactablesHovered)
        {
            // Check if the hovered object has a PinchableObject component
            if (target is XRBaseInteractable interactable &&
                interactable.GetComponent<PinchableObject>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsGrabbingPinchableObject()
    {
        // Check Ray Interactor's selected targets
        if (rayInteractor != null && rayInteractor.interactablesSelected.Count > 0)
        {
            foreach (var target in rayInteractor.interactablesSelected)
            {
                if (target is XRBaseInteractable interactable &&
                    interactable.GetComponent<PinchableObject>() != null)
                {
                    return true;
                }
            }
        }

        // Check Direct Interactor's selected targets
        if (directInteractor != null && directInteractor.interactablesSelected.Count > 0)
        {
            foreach (var target in directInteractor.interactablesSelected)
            {
                if (target is XRBaseInteractable interactable &&
                    interactable.GetComponent<PinchableObject>() != null)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
