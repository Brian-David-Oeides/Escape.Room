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

    // The trigger action belonging to the hand currently holding this tool. Both hands'
    // callbacks stay subscribed while enabled, so handlers filter on this instead.
    private InputActionReference heldTriggerAction;

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

    // Determine which hand's trigger action to use. The interactor GameObjects themselves are
    // named "Direct Interactor"/"Ray Interactor" for both hands, so walk up to the hand's
    // controller object ("Left Controller"/"Right Controller") and check that name instead -
    // the same object DynamicAttachPoint.cs does its name-based hand detection on.
    private InputActionReference GetTriggerActionForInteractor(Transform interactorTransform)
    {
        ActionBasedController controller = interactorTransform != null
            ? interactorTransform.GetComponentInParent<ActionBasedController>()
            : null;

        for (Transform t = controller != null ? controller.transform : interactorTransform; t != null; t = t.parent)
        {
            string name = t.name.ToLower();
            if (name.Contains("left")) return leftTriggerAction;
            if (name.Contains("right")) return rightTriggerAction;
        }

        GameLog.LogWarning($"[OilSprayController] Could not determine hand for interactor '{interactorTransform?.name}' - defaulting to right");
        return rightTriggerAction;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        // Remember which hand's trigger should drive the spray. The rig-owned Activate action
        // must never be enabled/disabled from here - the XR controllers depend on it.
        heldTriggerAction = GetTriggerActionForInteractor(args.interactorObject.transform);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        heldTriggerAction = null;

        // Stop particle system when object is released
        StopSpray();
    }

    private bool IsHeldHandAction(InputAction.CallbackContext context)
    {
        return isGrabbed && heldTriggerAction != null && context.action == heldTriggerAction.action;
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        // Only spray if grabbed, and only for the trigger of the hand holding this tool
        if (IsHeldHandAction(context) && oilParticleSystem != null)
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
        // Stop spraying when the holding hand's trigger is released
        // (dropping the tool mid-spray is handled by OnReleased)
        if (IsHeldHandAction(context))
        {
            StopSpray();
        }
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