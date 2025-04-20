using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DoorToggle : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string boolParameterName = "IsOpen";
    private bool isOpen = false;

    void Start()
    {
        // Get the Animator if not assigned
        if (doorAnimator == null)
        {
            // Try to find the animator on the parent (door) if this script is on the handle
            doorAnimator = GetComponentInParent<Animator>();

            // If still not found, try to find it on this object
            if (doorAnimator == null)
            {
                doorAnimator = GetComponent<Animator>();
            }
        }

        // Get the handle's XR Grab Interactable
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();

        // Subscribe to the grab event
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnHandleGrabbed);
            Debug.Log("Successfully registered grab listener");
        }
        else
        {
            Debug.LogError("No XR Grab Interactable found on this object!");
        }

        // Initialize the door to closed state
        if (doorAnimator != null)
        {
            doorAnimator.SetBool(boolParameterName, false);
            Debug.Log("Door animator initialized with parameter: " + boolParameterName);
        }
        else
        {
            Debug.LogError("No Animator found for the door!");
        }
    }

    private void OnHandleGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("Handle grabbed event triggered");

        if (doorAnimator == null)
        {
            Debug.LogError("Door Animator not assigned in DoorToggle script");
            return;
        }

        // Toggle the door state
        isOpen = !isOpen;

        Debug.Log("Toggling door state to: " + (isOpen ? "Open" : "Closed"));
        doorAnimator.SetBool(boolParameterName, isOpen);
    }

    // Public method to toggle door state from other scripts or events
    public void ToggleDoor()
    {
        Debug.Log("ToggleDoor method called directly");
        OnHandleGrabbed(null);
    }
}
