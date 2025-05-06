using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolboxLidCloseAudio : MonoBehaviour
{
    #region Variables

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip closeClip;

    #endregion

    #region Unity Events

    private void OnCollisionEnter(Collision collision)
    {
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
