using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDoorController : MonoBehaviour, ISaveable
{
    [SerializeField] 
    private Animator _animator; 
    [SerializeField] 
    private XRGrabInteractable _grabInteractable;

    [SerializeField] 
    private string _turnHandleParam = "TurnHandle";
    [SerializeField] 
    private string _isOpenParam = "IsOpen";
    [SerializeField] 
    private float _handleAnimDuration = 1.0f;

    [Header("Puzzle Lock")]
    [SerializeField]
    [Tooltip("Should door handle be locked until safe is unlocked?")]
    private bool requireSafeUnlock = true;

    [Header("Save System")]
    [SerializeField] private string doorID = "safe_door";

    private bool _safeUnlocked = false;

    private bool _isOpen = false;
    private bool _isAnimating = false;

    void Start()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnHandleGrabbed);

            // Lock door handle if required
            if (requireSafeUnlock)
            {
                _grabInteractable.enabled = false;
                Debug.Log("[SafeDoorController] Door handle locked - safe must be unlocked first");
            }
        }
        else
        {
            Debug.LogError("[SafeDoorController] Missing XRGrabInteractable reference!");
        }

        // states start in disabled state
        if (_animator != null)
        {
            _animator.SetBool(_turnHandleParam, false);
            _animator.SetBool(_isOpenParam, false);
        }
    }

    /// <summary>
    /// Called via UnityEvent when safe dial is unlocked
    /// Enables the door handle XRGrabInteractable
    /// </summary>
    public void EnableDoorHandle()
    {
        if (_grabInteractable != null)
        {
            _safeUnlocked = true;
            _grabInteractable.enabled = true;
            Debug.Log("[SafeDoorController] Door handle unlocked! Can now be grabbed.");
        }
    }

    private void OnHandleGrabbed(SelectEnterEventArgs args)
    {
        if (_isAnimating) return;

        StartCoroutine(AnimateDoorToggle());
    }

    private IEnumerator AnimateDoorToggle()
    {
        _isAnimating = true;

        // Trigger handle animation
        _animator.SetBool(_turnHandleParam, true);

        yield return new WaitForSeconds(_handleAnimDuration);

        // Toggle door state
        _isOpen = !_isOpen;
        _animator.SetBool(_isOpenParam, _isOpen);

        // Reset handle param (if used in transition)
        _animator.SetBool(_turnHandleParam, false);

        _isAnimating = false;
    }

    #region ISaveable Implementation

    public string SaveID => doorID;

    public void SaveState(SaveData saveData)
    {
        // Save door state using MoveableObjectData
        // Format: _safeUnlocked|_isOpen
        string customData = $"{_safeUnlocked}|{_isOpen}";

        MoveableObjectData objectState = new MoveableObjectData(
            doorID,
            transform.position,
            transform.rotation,
            gameObject.activeSelf,
            customData
        );

        saveData.moveableObjects.Add(objectState);

        Debug.Log($"[SafeDoorController] Saved state for {doorID}: unlocked={_safeUnlocked}, open={_isOpen}");
    }

    public void LoadState(SaveData saveData)
    {
        // Find this door's saved state
        MoveableObjectData savedState = saveData.moveableObjects.Find(obj => obj.objectID == doorID);

        if (savedState != null && !string.IsNullOrEmpty(savedState.customData))
        {
            // Parse the customData string
            string[] parts = savedState.customData.Split('|');

            if (parts.Length == 2)
            {
                bool.TryParse(parts[0], out _safeUnlocked);
                bool.TryParse(parts[1], out _isOpen);

                Debug.Log($"[SafeDoorController] Loaded state for {doorID}: unlocked={_safeUnlocked}, open={_isOpen}");

                // Restore the door state
                RestoreDoorState();
            }
        }
        else
        {
            Debug.Log($"[SafeDoorController] No saved state found for {doorID} - using defaults");
        }
    }

    /// <summary>
    /// Restore door state when loading from save
    /// </summary>
    private void RestoreDoorState()
    {
        // Restore unlock state
        if (_safeUnlocked && _grabInteractable != null)
        {
            _grabInteractable.enabled = true;
            Debug.Log($"[SafeDoorController] Door handle restored to unlocked state");
        }

        // Restore door open/close state
        if (_animator != null)
        {
            _animator.SetBool(_isOpenParam, _isOpen);
            _animator.SetBool(_turnHandleParam, false);

            Debug.Log($"[SafeDoorController] Door restored to {(_isOpen ? "OPEN" : "CLOSED")} state");
        }
    }

    #endregion
}

