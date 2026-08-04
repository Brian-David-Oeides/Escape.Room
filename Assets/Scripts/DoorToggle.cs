using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DoorToggle : MonoBehaviour, ISaveable
{
    [SerializeField] private Animator _doorAnimator;
    [SerializeField] private string _boolParameterName = "IsOpen";

    [Header("Save System")]
    [SerializeField] private string doorID = "";

    private bool _isOpen = false;

    void Start()
    {
        // Auto-generate unique ID if not set
        if (string.IsNullOrEmpty(doorID))
        {
            doorID = GenerateUniqueID();
            GameLog.Log($"[DoorToggle] Auto-generated ID: {doorID}");
        }

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
            GameLog.Log("Successfully registered grab listener");
        }
        else
        {
            GameLog.LogError("No XR Grab Interactable found on this object!");
        }

        // Initialize the door to closed state
        if (_doorAnimator != null)
        {
            _doorAnimator.SetBool(_boolParameterName, false);
            GameLog.Log("Door animator initialized with parameter: " + _boolParameterName);
        }
        else
        {
            GameLog.LogError("No Animator found for the door!");
        }
    }

    private void OnHandleGrabbed(SelectEnterEventArgs args)
    {
        GameLog.Log("Handle grabbed event triggered");

        if (_doorAnimator == null)
        {
            GameLog.LogError("Door Animator not assigned in DoorToggle script");
            return;
        }

        // Toggle the door state
        _isOpen = !_isOpen;

        GameLog.Log("Toggling door state to: " + (_isOpen ? "Open" : "Closed"));
        _doorAnimator.SetBool(_boolParameterName, _isOpen);
    }

    // Public method to toggle door state from other scripts or events
    public void ToggleDoor()
    {
        GameLog.Log("ToggleDoor method called directly");
        OnHandleGrabbed(null);
    }

    /// <summary>
    /// Generate unique ID based on GameObject hierarchy path
    /// </summary>
    private string GenerateUniqueID()
    {
        string path = GetHierarchyPath(transform);
        return $"door_{path}".Replace("/", "_").Replace(" ", "_");
    }

    /// <summary>
    /// Get the full hierarchy path of this GameObject
    /// </summary>
    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    #region ISaveable Implementation

    public string SaveID => doorID;

    public void SaveState(SaveData saveData)
    {
        // Save door state - add to list if OPEN, remove if CLOSED
        if (_isOpen)
        {
            if (!saveData.completedPuzzleIDs.Contains(doorID))
            {
                saveData.completedPuzzleIDs.Add(doorID);
            }
        }
        else
        {
            saveData.completedPuzzleIDs.Remove(doorID);
        }

        GameLog.Log($"[DoorToggle] Saved state for {doorID}: {(_isOpen ? "OPEN" : "CLOSED")}");
    }

    public void LoadState(SaveData saveData)
    {
        // Check if door was open when saved
        bool wasOpen = saveData.completedPuzzleIDs.Contains(doorID);

        // Restore door state
        _isOpen = wasOpen;

        // Update animator without triggering events
        if (_doorAnimator != null)
        {
            _doorAnimator.SetBool(_boolParameterName, _isOpen);
        }

        GameLog.Log($"[DoorToggle] Loaded state for {doorID}: {(_isOpen ? "OPEN" : "CLOSED")}");
    }

    #endregion
}
