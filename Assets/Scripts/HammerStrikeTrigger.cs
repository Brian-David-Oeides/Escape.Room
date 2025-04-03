using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HammerStrikeTrigger : MonoBehaviour
{
    [Tooltip("The Rigidbody (e.g., cabinet door) that should be un-kinematic on hammer impact.")]
    public Rigidbody targetRigidbody;

    [Tooltip("Tag of the hammer object.")]
    public string hammerTag = "Hammer";

    [Tooltip("Reference to the XR Socket Interactor holding the stake (on the cabinet).")]
    public XRSocketInteractor stakeSocket;

    private bool _hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;

        if (other.CompareTag(hammerTag) && targetRigidbody != null)
        {
            // step 1: unlock cabinet door
            targetRigidbody.isKinematic = false;

            // step 2: disable the XR Socket Interactor to release stake
            if (stakeSocket != null)
            {
                stakeSocket.enabled = false;
            }

            _hasTriggered = true;
        }
    }
}

