using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TorchSocketController : MonoBehaviour
{
    // Reference to the Light component on the torch_light object
    [SerializeField] private Light torchLight;

    // Reference to the ElectricTorchOnOff script
    [SerializeField] private ElectricTorchOnOff torchScript;

    // Reference to renderer with emission material (optional)
    [SerializeField] private Renderer emissionRenderer;

    // Track socket state
    private bool isInSocket = false;

    private void Awake()
    {
        // Find required components if not set in inspector
        if (torchScript == null)
        {
            torchScript = GetComponentInChildren<ElectricTorchOnOff>();
        }

        if (torchLight == null && torchScript != null)
        {
            torchLight = torchScript.GetComponent<Light>();
        }

        // Set up XR interactions
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            // When grabbed by hand
            grabInteractable.selectEntered.AddListener(OnSelectEntered);

            // When released (either dropped or placed in socket)
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        // Force initial state to be off
        if (torchScript != null)
        {
            torchScript.TurnFlashlightOff();
        }
        else if (torchLight != null)
        {
            // Direct control fallback
            torchLight.enabled = false;
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Check if grabbed by hand (not socket)
        if (args.interactorObject is XRDirectInteractor)
        {
            // Turn on light when grabbed by hand
            EnableLight(true);
            isInSocket = false;
        }
        else if (args.interactorObject is XRSocketInteractor)
        {
            // Explicitly turn off when entering socket
            EnableLight(false);
            isInSocket = true;
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // If removed from socket into hand
        if (args.interactorObject is XRSocketInteractor)
        {
            // Turn on when taken from socket
            EnableLight(true);
            isInSocket = false;
        }
        else if (args.interactorObject is XRDirectInteractor)
        {
            // If dropped (not into socket), turn off
            if (!isInSocket)
            {
                EnableLight(false);
            }
        }
    }

    // Helper method to ensure light gets turned on/off correctly
    private void EnableLight(bool enable)
    {
        // Try using the script's methods first
        if (torchScript != null)
        {
            if (enable)
            {
                torchScript.TurnFlashlightOn();
            }
            else
            {
                torchScript.TurnFlashlightOff();
            }
            return;
        }

        // Direct control fallback if script reference fails
        if (torchLight != null)
        {
            if (enable)
            {
                torchLight.enabled = true;
            }
            else
            {
                torchLight.enabled = false;
            }
        }

        // Handle emission material if available
        if (emissionRenderer != null)
        {
            if (enable)
            {
                emissionRenderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                emissionRenderer.material.DisableKeyword("_EMISSION");
            }
        }
    }
}
