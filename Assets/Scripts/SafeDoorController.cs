using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDoorController : MonoBehaviour
{
    [Header("Animator References")]
    [SerializeField] private Animator doorAnimator;     // Animator on Door_Hinge
    [SerializeField] private Animator handleAnimator;   // Animator on Door_Handle

    [Header("Animation Parameters")]
    [SerializeField] private string handleBool = "TurnHandle"; // Parameter in handle Animator
    [SerializeField] private string doorBool = "IsOpen";       // Parameter in door Animator

    [Header("Timing")]
    [SerializeField] private float handleAnimationDuration = 1.0f; // How long to wait before opening door

    [Header("Interaction")]
    [SerializeField] private XRGrabInteractable grabInteractable;  // Assigned to DoorHandleCollider

    private bool hasBeenUsed = false;

    private void Start()
    {
        // Try to auto-assign XR Grab Interactable if not set
        if (grabInteractable == null)
        {
            grabInteractable = GetComponentInChildren<XRGrabInteractable>();
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnHandleGrabbed);
            Debug.Log("SafeDoorController: Grab listener registered.");
        }
        else
        {
            Debug.LogError("SafeDoorController: XRGrabInteractable not assigned or found.");
        }

        // Ensure door starts in closed state
        if (doorAnimator != null)
        {
            doorAnimator.SetBool(doorBool, false);
        }
    }

    private void OnHandleGrabbed(SelectEnterEventArgs args)
    {
        if (hasBeenUsed) return;

        hasBeenUsed = true;
        Debug.Log("SafeDoorController: Handle grabbed, triggering animations.");

        // Start handle animation
        if (handleAnimator != null)
        {
            handleAnimator.SetBool(handleBool, true);
        }

        // Trigger door after short delay
        StartCoroutine(TriggerDoorAfterDelay());
    }

    private IEnumerator TriggerDoorAfterDelay()
    {
        yield return new WaitForSeconds(handleAnimationDuration);

        if (doorAnimator != null)
        {
            doorAnimator.SetBool(doorBool, true);
            Debug.Log("SafeDoorController: Door animation triggered.");
        }
    }
    public void TriggerHandleAndDoor()
    {
        if (!hasBeenUsed)
        {
            hasBeenUsed = true;
            Debug.Log("SafeDoorController: Triggered via inspector event.");

            if (handleAnimator != null)
                handleAnimator.SetBool(handleBool, true);

            StartCoroutine(TriggerDoorAfterDelay());
        }
    }

}
