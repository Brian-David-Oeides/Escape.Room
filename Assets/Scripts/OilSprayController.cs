using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class OilSprayController : MonoBehaviour
{
    [Header("Particle System")]
    [SerializeField] private ParticleSystem oilParticleSystem;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftTriggerAction;
    [SerializeField] private InputActionReference rightTriggerAction;

    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;

    void Start()
    {
        // get the XR Grab Interactable component
        grabInteractable = GetComponent<XRGrabInteractable>();

        // subscribe to grab events
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        // particle system is stopped at start
        if (oilParticleSystem != null)
            oilParticleSystem.Stop();
    }

    void OnEnable()
    {
        // Enable input actions
        if (leftTriggerAction != null)
        {
            leftTriggerAction.action.performed += OnTriggerPressed;
            leftTriggerAction.action.canceled += OnTriggerReleased;
            leftTriggerAction.action.Enable();
        }

        if (rightTriggerAction != null)
        {
            rightTriggerAction.action.performed += OnTriggerPressed;
            rightTriggerAction.action.canceled += OnTriggerReleased;
            rightTriggerAction.action.Enable();
        }
    }

    void OnDisable()
    {
        // Disable input actions
        if (leftTriggerAction != null)
        {
            leftTriggerAction.action.performed -= OnTriggerPressed;
            leftTriggerAction.action.canceled -= OnTriggerReleased;
            leftTriggerAction.action.Disable();
        }

        if (rightTriggerAction != null)
        {
            rightTriggerAction.action.performed -= OnTriggerPressed;
            rightTriggerAction.action.canceled -= OnTriggerReleased;
            rightTriggerAction.action.Disable();
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // Stop particle system when object is released
        if (oilParticleSystem != null && oilParticleSystem.isPlaying)
            oilParticleSystem.Stop();
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        // Only spray if the object is currently grabbed
        if (isGrabbed && oilParticleSystem != null)
        {
            oilParticleSystem.Play();
        }
    }

    private void OnTriggerReleased(InputAction.CallbackContext context)
    {
        // Stop spraying when trigger is released
        if (oilParticleSystem != null && oilParticleSystem.isPlaying)
        {
            oilParticleSystem.Stop();
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