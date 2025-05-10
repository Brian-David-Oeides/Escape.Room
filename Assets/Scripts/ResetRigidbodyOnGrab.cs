using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]

public class ResetRigidbodyOnGrab : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;
    private InteractionLayerMask originalLayers;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        originalLayers = grab.interactionLayers;

        grab.selectEntered.AddListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        // Restore to original interaction layers
        grab.interactionLayers = originalLayers;
    }
}

