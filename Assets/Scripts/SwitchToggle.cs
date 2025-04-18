using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class SwitchToggle : MonoBehaviour
{
    [SerializeField] private bool _isOn = false;

    [Header("Animation")]
    [SerializeField] private Animator _switchAnimator;
    [SerializeField] private string _animBoolParameterName = "SwitchOn";

    [Header("Events")]
    public UnityEvent OnSwitchOn;
    public UnityEvent OnSwitchOff;

    private XRSimpleInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();

        if (_interactable != null)
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
        }
        else
        {
            Debug.LogError("No XRSimpleInteractable found on this GameObject!");
        }

        if (_switchAnimator == null)
        {
            _switchAnimator = GetComponent<Animator>();
        }

        UpdateAnimationState();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        ToggleState();
    }

    public void ToggleState()
    {
        _isOn = !_isOn;
        UpdateAnimationState();

        if (_isOn)
        {
            OnSwitchOn?.Invoke();
        }
        else
        {
            OnSwitchOff?.Invoke();
        }
    }

    private void UpdateAnimationState()
    {
        if (_switchAnimator != null)
        {
            _switchAnimator.SetBool(_animBoolParameterName, _isOn);
        }
    }

    // Public method to manually turn on the switch
    public void TurnOn()
    {
        if (!_isOn)
        {
            ToggleState();
        }
    }

    // Public method to manually turn off the switch
    public void TurnOff()
    {
        if (_isOn)
        {
            ToggleState();
        }
    }
}
