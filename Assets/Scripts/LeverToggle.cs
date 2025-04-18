using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class LeverToggle : MonoBehaviour
{
    [SerializeField] private bool _isOn = false;

    [Header("Animation")]
    [SerializeField] private Animator _leverAnimator;

    [Header("Events")]
    public UnityEvent OnLeverUp;
    public UnityEvent OnLeverDown;

    private XRSimpleInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();

        if (_interactable != null)
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
        }

        if (_leverAnimator == null)
        {
            _leverAnimator = GetComponent<Animator>();
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Make sure animator is enabled when lever is interacted with
        if (_leverAnimator != null && !_leverAnimator.enabled)
        {
            _leverAnimator.enabled = true;
        }

        ToggleState();
    }

    public void ToggleState()
    {
        _isOn = !_isOn;

        // Update animation if animator is available and enabled
        if (_leverAnimator != null)
        {
            if (!_leverAnimator.enabled)
            {
                _leverAnimator.enabled = true;
            }

            _leverAnimator.SetBool("LeverOn", _isOn);
        }

        // Trigger appropriate events
        if (_isOn)
        {
            OnLeverUp?.Invoke();
        }
        else
        {
            OnLeverDown?.Invoke();
        }
    }

    // Optional method to manually enable the animator
    public void EnableAnimator()
    {
        if (_leverAnimator != null)
        {
            _leverAnimator.enabled = true;
        }
    }
}