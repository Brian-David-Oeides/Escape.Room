using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDialHaptics : MonoBehaviour
{
    [Header("References")]
    public SafeDial safeDial; // Reference to the SafeDial script
    private XRBaseInteractor currentInteractor;

    [Header("Haptic Settings")]
    public float tickAmplitude = 0.2f;
    public float tickDuration = 0.02f;
    public float correctAmplitude = 0.6f;
    public float correctDuration = 0.1f;
    public float unlockAmplitude = 1f;
    public float unlockDuration = 0.3f;

    private int lastKnownDialNumber = -1;

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

    private void OnGrab(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject as XRBaseInteractor;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        currentInteractor = null;
    }

    private void Update()
    {
        if (safeDial == null || currentInteractor == null)
            return;

        // Detect number changes (tick)
        if (safeDial.currentDialNumber != lastKnownDialNumber)
        {
            lastKnownDialNumber = safeDial.currentDialNumber;
            SendHaptic(tickAmplitude, tickDuration);
        }

        // Detect correct number landing
        if (!safeDial.IsUnlocked && safeDial.CurrentCombinationIndex < safeDial.CorrectCombination.Length)
        {
            int target = safeDial.CorrectCombination[safeDial.CurrentCombinationIndex];
            if (Mathf.Abs(safeDial.currentDialNumber - target) <= safeDial.NumberTolerance)
            {
                SendHaptic(correctAmplitude, correctDuration);
            }
        }

        // Detect unlock
        if (safeDial.IsUnlocked && currentInteractor != null)
        {
            SendHaptic(unlockAmplitude, unlockDuration);
        }
    }

    private void SendHaptic(float amplitude, float duration)
    {
        if (currentInteractor != null)
        {
            if (currentInteractor is XRBaseControllerInteractor controllerInteractor)
            {
                controllerInteractor.SendHapticImpulse(Mathf.Clamp01(amplitude), duration);
            }
        }
    }
}

