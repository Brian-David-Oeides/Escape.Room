using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDialHaptics : MonoBehaviour
{
    #region Variables

    [Header("References")]
    public SafeDial safeDial; // Reference to the SafeDial script
    private XRBaseInteractor _currentInteractor;

    [Header("Haptic Settings")]
    public float tickAmplitude = 0.2f;
    public float tickDuration = 0.02f;
    public float correctAmplitude = 0.6f;
    public float correctDuration = 0.1f;
    public float unlockAmplitude = 1f;
    public float unlockDuration = 0.3f;

    private int _lastKnownDialNumber = -1;

    #endregion

    #region Unity Events

    private void Awake()
    {
        var grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
        else
        {
            Debug.LogWarning("No XRGrabInteractable found on Safe Dial Haptics GameObject!");
        }

        if (safeDial == null)
        {
            safeDial = GetComponent<SafeDial>();
        }
    }

    private void Update()
    {
        if (safeDial == null || _currentInteractor == null)
            return;

        // detect number changes (tick)
        if (safeDial.currentDialNumber != _lastKnownDialNumber)
        {
            _lastKnownDialNumber = safeDial.currentDialNumber;
            SendHaptic(tickAmplitude, tickDuration);
        }

        // detect correct number landing
        if (!safeDial.IsUnlocked && safeDial.CurrentCombinationIndex < safeDial.CorrectCombination.Length)
        {
            int target = safeDial.CorrectCombination[safeDial.CurrentCombinationIndex];
            if (Mathf.Abs(safeDial.currentDialNumber - target) <= safeDial.NumberTolerance)
            {
                SendHaptic(correctAmplitude, correctDuration);
            }
        }

        // detect unlock
        if (safeDial.IsUnlocked && _currentInteractor != null)
        {
            SendHaptic(unlockAmplitude, unlockDuration);
        }
    }

    #endregion

    #region Custom Methods

    private void OnGrab(SelectEnterEventArgs args)
    {
        _currentInteractor = args.interactorObject as XRBaseInteractor;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _currentInteractor = null;
    }

    private void SendHaptic(float amplitude, float duration)
    {
        if (_currentInteractor != null)
        {
            if (_currentInteractor is XRBaseControllerInteractor controllerInteractor)
            {
                controllerInteractor.SendHapticImpulse(Mathf.Clamp01(amplitude), duration);
            }
        }
    }

    #endregion
}

