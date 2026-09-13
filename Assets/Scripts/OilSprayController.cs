using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class OilSprayController : MonoBehaviour
{
    [Header("Particle System")]
    [SerializeField] private ParticleSystem oilParticleSystem;

    [Header("Audio")]
    [SerializeField] private AudioSource sprayAudioSource;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftTriggerAction;
    [SerializeField] private InputActionReference rightTriggerAction;

    [Header("Attempt Events")]
    [Tooltip("Fired when a spray attempt starts (trigger pressed while grabbed).")]
    public UnityEvent OnSprayAttemptStarted;

    [Tooltip("Fired when a spray attempt actually stops emitting (trigger released or tool dropped mid-spray).")]
    public UnityEvent OnSprayAttemptEnded;

    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;

    void Start()
    {
        // Get the XR Grab Interactable component
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Subscribe to grab events
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        // Ensure particle system is stopped at start
        if (oilParticleSystem != null)
            oilParticleSystem.Stop();
    }

    void OnEnable()
    {
        // Only subscribe to events when component is enabled
        if (leftTriggerAction != null)
        {
            leftTriggerAction.action.performed += OnTriggerPressed;
            leftTriggerAction.action.canceled += OnTriggerReleased;
        }

        if (rightTriggerAction != null)
        {
            rightTriggerAction.action.performed += OnTriggerPressed;
            rightTriggerAction.action.canceled += OnTriggerReleased;
        }
    }

    void OnDisable()
    {
        // Only unsubscribe when component is disabled
        if (leftTriggerAction != null)
        {
            leftTriggerAction.action.performed -= OnTriggerPressed;
            leftTriggerAction.action.canceled -= OnTriggerReleased;
        }

        if (rightTriggerAction != null)
        {
            rightTriggerAction.action.performed -= OnTriggerPressed;
            rightTriggerAction.action.canceled -= OnTriggerReleased;
        }
    }

    // Determine which hand's trigger action to use, based on the grabbing interactor's transform name
    // (matches the existing name-based hand-detection pattern used in DynamicAttachPoint.cs)
    private InputActionReference GetTriggerActionForInteractor(Transform interactorTransform)
    {
        bool isLeftHand = interactorTransform != null && interactorTransform.name.ToLower().Contains("left");
        return isLeftHand ? leftTriggerAction : rightTriggerAction;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        // Enable ONLY the action for the hand that grabbed this
        InputActionReference triggerAction = GetTriggerActionForInteractor(args.interactorObject.transform);
        if (triggerAction != null)
        {
            triggerAction.action.Enable();
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // Disable ONLY the action for the hand that released this
        InputActionReference triggerAction = GetTriggerActionForInteractor(args.interactorObject.transform);
        if (triggerAction != null)
        {
            triggerAction.action.Disable();
        }

        // Stop particle system when object is released
        StopSpray();
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        // Only spray if the object is currently grabbed
        if (isGrabbed && oilParticleSystem != null)
        {
            oilParticleSystem.Play();

            // Play spray sound effect
            if (sprayAudioSource != null && sprayAudioSource.clip != null)
            {
                sprayAudioSource.Play();
            }

            OnSprayAttemptStarted?.Invoke();
            GameLog.Log("[OilSprayController] TEMP-LOG: OnSprayAttemptStarted invoked"); // TODO: remove after miss-detection debugging
        }
    }

    private void OnTriggerReleased(InputAction.CallbackContext context)
    {
        // Stop spraying when trigger is released
        StopSpray();
    }

    // Shared stop path for both "trigger released" and "tool dropped" - only fires
    // OnSprayAttemptEnded once per actual stop, since wasPlaying guards against a
    // second call (e.g. dropping the tool right after releasing the trigger) re-firing it.
    private void StopSpray()
    {
        bool wasPlaying = oilParticleSystem != null && oilParticleSystem.isPlaying;

        if (wasPlaying)
        {
            oilParticleSystem.Stop();
        }

        if (sprayAudioSource != null && sprayAudioSource.isPlaying)
        {
            sprayAudioSource.Stop();
        }

        if (wasPlaying)
        {
            OnSprayAttemptEnded?.Invoke();
            GameLog.Log("[OilSprayController] TEMP-LOG: OnSprayAttemptEnded invoked"); // TODO: remove after miss-detection debugging
        }
    }

    void OnDestroy()
    {
        // Clean up event subscriptions
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
}