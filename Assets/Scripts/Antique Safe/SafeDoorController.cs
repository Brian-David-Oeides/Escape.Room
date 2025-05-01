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

    private bool _isOpen = false;
    private bool _isAnimating = false;

    void Start()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnHandleGrabbed);
        }

        // states start in disabled state
        _animator.SetBool(_turnHandleParam, false);
        _animator.SetBool(_isOpenParam, false);
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

