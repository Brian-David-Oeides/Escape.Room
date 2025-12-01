using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDoorController : MonoBehaviour
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
}

