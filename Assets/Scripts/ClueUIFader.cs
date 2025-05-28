using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;


[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(AudioSource))]
public class ClueUIFader : MonoBehaviour
{
    #region Variables

    [Header("Positioning")]
    public float distanceFromHead = 0.5f;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;

    [Header("Audio")]
    public AudioClip fadeInSound;
    public AudioClip fadeOutSound;
    private AudioSource _audioSource;

    [Header("Floating Motion")]
    public float floatAmplitude = 0.01f;
    public float floatFrequency = 1f;

    private Vector3 _initialPosition;

    private CanvasGroup _canvasGroup;
    private float _fadeTimer;
    private bool _fadingIn;
    private bool _fadingOut;

    [Header("UI Binding")]
    public TMPro.TextMeshProUGUI clueText;

    [Header("Events")]
    public UnityEvent onClueShown;
    public UnityEvent onClueHidden;

    #endregion

    #region Unity Events

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        _audioSource = GetComponent<AudioSource>();

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
                gameObject.SetActive(false); // fully hide it
            }
        }
    }

    #endregion

    #region Custom Methods
    public void SetMessage(string message)
    {
        if (clueText != null)
            clueText.text = message;
    }

    private void PositionInFrontOfHead()
    {
        Transform head = Camera.main.transform;
        transform.position = head.position + head.forward * distanceFromHead;
        transform.rotation = Quaternion.LookRotation(transform.position - head.position);
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

    #endregion
}
