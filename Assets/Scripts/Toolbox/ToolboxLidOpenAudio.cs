using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ToolboxLidOpenAudio : MonoBehaviour
{
    #region Variables

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openClip;

    [Header("References")]
    public XRGrabInteractable grabInteractable;
    public Transform lidTransform;

    private bool _isGrabbing = false;
    private bool _hasPlayedOpenSound = false;

    #endregion

    #region Unity Events

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    private void FixedUpdate()
    {
        if (!_isGrabbing || _hasPlayedOpenSound) return;

        float angle = lidTransform.localEulerAngles.x;
        if (angle > 180f) angle -= 360f;

        if (Mathf.Abs(angle) >= 1f)
        {
            audioSource.PlayOneShot(openClip);
            _hasPlayedOpenSound = true;
        }
    }

    #endregion

    #region Custom Methods

    private void OnGrab(SelectEnterEventArgs args)
    {
        _isGrabbing = true;
        _hasPlayedOpenSound = false;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _isGrabbing = false;
    }

    #endregion

}
