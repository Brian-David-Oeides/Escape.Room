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

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _switchOnSound;
    [SerializeField] private AudioClip _switchOffSound;

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

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
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
        PlaySwitchSound();

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

    private void PlaySwitchSound()
    {
        if (_audioSource != null)
        {
            AudioClip clipToPlay = _isOn ? _switchOnSound : _switchOffSound;

            if (clipToPlay != null)
            {
                _audioSource.PlayOneShot(clipToPlay);
            }
            else
            {
                Debug.LogWarning($"Missing audio clip for switch {(_isOn ? "ON" : "OFF")} state!");
            }
        }
        else
        {
            Debug.LogWarning("No AudioSource assigned to SwitchToggle!");
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
