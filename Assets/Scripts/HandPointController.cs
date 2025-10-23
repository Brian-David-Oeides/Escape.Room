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

    private void Start()
    {
        // Get the Direct Interactor component for hover detection
        directInteractor = GetComponentInChildren<XRDirectInteractor>();
        if (directInteractor == null)
        {
            Debug.LogError($"No XRDirectInteractor found on {gameObject.name}");
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
            // Handle trigger for pointing
            bool triggerPressed = xRController.activateInteractionState.active;
            bool gripPressed = xRController.selectInteractionState.active;

            // Check if hovering over a pinchable object
            bool isHoveringPinchable = IsHoveringPinchableObject();

            // Pinch takes priority: trigger + grip + hovering pinchable object
            bool shouldPinch = triggerPressed && gripPressed && isHoveringPinchable;

            if (shouldPinch)
            {
                // Pinch overrides everything
                handAnimator.SetBool(pinchParameterName, true);
                handAnimator.SetBool(pointParameterName, false);
                handAnimator.SetBool(gripParameterName, false);
            }
            else
            {
                // Normal behavior
                handAnimator.SetBool(pinchParameterName, false);
                handAnimator.SetBool(pointParameterName, triggerPressed);
                handAnimator.SetBool(gripParameterName, gripPressed);
            }

            // Debug output
            if (triggerPressed || gripPressed || shouldPinch)
            {
                Debug.Log($"{gameObject.name} - Point: {triggerPressed}, Grip: {gripPressed}, Pinch: {shouldPinch}, HoveringPinchable: {isHoveringPinchable}");
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
}
