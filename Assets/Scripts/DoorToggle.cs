using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DoorToggle : MonoBehaviour
{
    [SerializeField] private Animator _doorAnimator;
    [SerializeField] private string _boolParameterName = "IsOpen";
    private bool _isOpen = false;

    void Start()
    {
        // Get the Animator if not assigned
        if (_doorAnimator == null)
        {
            // Try to find the animator on the parent (door) if this script is on the handle
            _doorAnimator = GetComponentInParent<Animator>();

            // If still not found, try to find it on this object
            if (_doorAnimator == null)
            {
                _doorAnimator = GetComponent<Animator>();
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
        if (_doorAnimator != null)
        {
            _doorAnimator.SetBool(_boolParameterName, false);
            Debug.Log("Door animator initialized with parameter: " + _boolParameterName);
        }
        else
        {
            Debug.LogError("No Animator found for the door!");
        }
    }

    private void OnHandleGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("Handle grabbed event triggered");

        if (_doorAnimator == null)
        {
            Debug.LogError("Door Animator not assigned in DoorToggle script");
            return;
        }

        // Toggle the door state
        _isOpen = !_isOpen;

        Debug.Log("Toggling door state to: " + (_isOpen ? "Open" : "Closed"));
        _doorAnimator.SetBool(_boolParameterName, _isOpen);
    }

    // Public method to toggle door state from other scripts or events
    public void ToggleDoor()
    {
        Debug.Log("ToggleDoor method called directly");
        OnHandleGrabbed(null);
    }
}
