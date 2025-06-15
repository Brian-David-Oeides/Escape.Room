using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolboxLidCloseAudio : MonoBehaviour
{
    #region Variables

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip closeClip;

    [Header("Settings")]
    public float initializationDelay = 0.3f;
    public float minimumVelocity = 0.05f;

    private float _startTime;
    private Rigidbody _rigidbody;

    #endregion

    #region Unity Events

    private void OnCollisionEnter(Collision collision)
    {
        // Prevent audio during initialization
        if (Time.time - _startTime < initializationDelay)
            return;

        // Only play if there's actual movement
        if (_rigidbody != null && _rigidbody.velocity.magnitude < minimumVelocity)
            return;

        if (collision.gameObject.CompareTag("Stopper"))
        {
            // confirm it's at a near-closed angle
            float angle = collision.transform.localEulerAngles.x;
            if (angle > 180f) angle -= 360f;

            if (Mathf.Abs(angle) <= 10f)
            {
                audioSource.PlayOneShot(closeClip);
            }
        }
    }

    #endregion
}
