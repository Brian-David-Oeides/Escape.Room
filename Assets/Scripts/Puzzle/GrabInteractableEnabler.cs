using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Simple helper script to enable/disable XRGrabInteractable via UnityEvents
/// Attach this to any GameObject with XRGrabInteractable component
/// </summary>

public class GrabInteractableEnabler : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            GameLog.LogError($"[GrabInteractableEnabler] No XRGrabInteractable found on {gameObject.name}!");
        }
    }

    /// <summary>
    /// Enable the XRGrabInteractable component (call from UnityEvent)
    /// </summary>
    public void EnableGrabbing()
    {
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
            GameLog.Log($"[GrabInteractableEnabler] Enabled grabbing on {gameObject.name}");
        }
    }

    /// <summary>
    /// Disable the XRGrabInteractable component (call from UnityEvent)
    /// </summary>
    public void DisableGrabbing()
    {
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
            GameLog.Log($"[GrabInteractableEnabler] Disabled grabbing on {gameObject.name}");
        }
    }
}
