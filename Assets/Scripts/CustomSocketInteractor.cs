using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CustomSocketInteractor : MonoBehaviour
{
    [Tooltip("Name tag to identify socketable objects")]
    public string validTag = "Socketable";

    [Tooltip("The transform where the object should snap")]
    public Transform socketAttachPoint; // assigned in HL_Valve inspector

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(validTag)) return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab == null || grab.isSelected) return; // Skip if grabbed

        // Freeze object physics
        Rigidbody rb = grab.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Snap to socket (using the VALVE's attach point, not the wrench’s)
        other.transform.SetPositionAndRotation(socketAttachPoint.position, socketAttachPoint.rotation);

        Debug.Log($"Snapped wrench to {socketAttachPoint.position} with rotation {socketAttachPoint.rotation.eulerAngles}");

        // Optional: prevent regrab
        grab.interactionLayerMask = 0; // (disable interaction without disabling the whole component)
    }


}

