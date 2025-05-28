using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(AudioSource))] 

public class ClueUI : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeDuration = 1f;

    [Header("Audio")]
    public AudioClip fadeInSound;
    public AudioClip fadeOutSound;

    [Header("Floating Motion")]
    public float floatAmplitude = 0.01f;
    public float floatFrequency = 1f;

    [Header("Positioning")]
    public float distanceFromHead = 0.8f;

    [Header("Events")]
    public UnityEvent onClueShown;
    public UnityEvent onClueHidden;

    private CanvasGroup _canvasGroup;
    private AudioSource _audioSource;
    private float _fadeTimer;
    private bool _fadingIn;
    private bool _fadingOut;
    private Vector3 _initialPosition;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _audioSource = GetComponent<AudioSource>();
        _canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        PositionInFrontOfHead();
        _initialPosition = transform.position;
        _fadeTimer = 0f;
        _fadingIn = true;
        _fadingOut = false;
        _canvasGroup.alpha = 0f;

        if (fadeInSound && _audioSource)
            _audioSource.PlayOneShot(fadeInSound);
    }

    private void Update()
    {
        if (_fadingIn || _fadingOut)
        {
            float floatOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = _initialPosition + transform.up * floatOffset;
        }

        if (_fadingIn)
        {
            _fadeTimer += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(_fadeTimer / fadeDuration);

            if (_canvasGroup.alpha >= 1f)
            {
                _fadingIn = false;
                onClueShown?.Invoke();
            }
        }
        else if (_fadingOut)
        {
            _fadeTimer += Time.deltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(_fadeTimer / fadeDuration);

            if (_canvasGroup.alpha <= 0f)
            {
                _fadingOut = false;
                onClueHidden?.Invoke();
                gameObject.SetActive(false);
            }
        }
    }

    public void HideClue()
    {
        if (!gameObject.activeSelf) return;
        _fadeTimer = 0f;
        _fadingIn = false;
        _fadingOut = true;

        if (fadeOutSound && _audioSource)
            _audioSource.PlayOneShot(fadeOutSound);
    }

    private void PositionInFrontOfHead()
    {
        Transform head = Camera.main.transform;
        transform.position = head.position + head.forward * distanceFromHead;
        transform.rotation = Quaternion.LookRotation(transform.position - head.position);
    }
}