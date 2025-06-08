using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]

public class DocumentViewer : MonoBehaviour
{
    [Header("Positioning")]
    public float distanceFromHead = 0.8f;
    public Vector3 verticalOffset = new Vector3(0, -0.1f, 0);

    [Header("Animation")]
    public float fadeDuration = 1f;
    public float floatAmplitude = 0.01f;
    public float floatFrequency = 1f;

    [Header("Audio")]
    public AudioClip showSound;
    public AudioClip hideSound;

    [Header("Events")]
    public UnityEvent onDocumentShown;
    public UnityEvent onDocumentHidden;

    #region Private Variables

    private enum ViewerState
    {
        Hidden,
        FadingIn,
        Visible,
        FadingOut
    }

    private ViewerState currentState = ViewerState.Hidden;
    private CanvasGroup _canvasGroup;
    private AudioSource _audioSource;
    private float _fadeTimer;
    private Vector3 _originalPosition;
    private Vector3 _initialFloatPosition;

    #endregion

    #region Unity Events

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        SetupAudioSource();

        _originalPosition = transform.position;
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleFading();
        HandleFloatingMotion();
        CheckDismissInput();
    }

    #endregion

    #region Setup

    private void SetupAudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.spatialBlend = 1.0f;
        _audioSource.playOnAwake = false;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.maxDistance = 10f;
    }

    #endregion

    #region Document Control

    public void ShowDocument()
    {
        if (currentState != ViewerState.Hidden) return;

        PositionInFrontOfHead();
        _initialFloatPosition = transform.position;

        currentState = ViewerState.FadingIn;
        _fadeTimer = 0f;
        gameObject.SetActive(true);

        PlaySound(showSound);

        Debug.Log("Starting to show document");
    }

    public void HideDocument()
    {
        if (currentState != ViewerState.Visible) return;

        currentState = ViewerState.FadingOut;
        _fadeTimer = 0f;

        PlaySound(hideSound);

        Debug.Log("Starting to hide document");
    }

    private void PositionInFrontOfHead()
    {
        Transform head = Camera.main.transform;
        transform.position = head.position + head.forward * distanceFromHead + verticalOffset;
        transform.rotation = Quaternion.LookRotation(transform.position - head.position);
    }

    #endregion

    #region Animation Handling

    private void HandleFading()
    {
        if (currentState == ViewerState.FadingIn)
        {
            _fadeTimer += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(_fadeTimer / fadeDuration);

            if (_canvasGroup.alpha >= 1f)
            {
                currentState = ViewerState.Visible;
                onDocumentShown?.Invoke();
                Debug.Log("Document fully shown");
            }
        }
        else if (currentState == ViewerState.FadingOut)
        {
            _fadeTimer += Time.deltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(_fadeTimer / fadeDuration);

            if (_canvasGroup.alpha <= 0f)
            {
                currentState = ViewerState.Hidden;
                gameObject.SetActive(false);
                onDocumentHidden?.Invoke();
                Debug.Log("Document fully hidden");
            }
        }
    }

    private void HandleFloatingMotion()
    {
        if (currentState == ViewerState.Visible || currentState == ViewerState.FadingIn)
        {
            float floatOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = _initialFloatPosition + transform.up * floatOffset;
        }
    }

    #endregion

    #region Input Handling

    private void CheckDismissInput()
    {
        if (currentState != ViewerState.Visible) return;

        // B button - Dismiss document
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand)
            .TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bButtonPressed))
        {
            if (bButtonPressed)
            {
                HideDocument();
            }
        }
    }

    #endregion

    #region Audio

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
            _audioSource.PlayOneShot(clip);
    }

    #endregion

    #region Public Methods

    public bool IsVisible()
    {
        return currentState == ViewerState.Visible;
    }

    public bool IsHidden()
    {
        return currentState == ViewerState.Hidden;
    }

    #endregion
}
