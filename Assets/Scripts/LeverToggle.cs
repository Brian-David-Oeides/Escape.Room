using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class LeverToggle : MonoBehaviour
{
    #region Variables

    [SerializeField] private bool _isOn = false;

    [Header("Animation")]
    [SerializeField] private Animator _leverAnimator;

    [Header("Audio")]
    [SerializeField] private AudioSource _toggleAudioSource;
    [SerializeField] private AudioClip _toggleSoundClip;

    [SerializeField] private AudioSource _electricShockAudioSource;
    [SerializeField] private AudioClip _electricShockClip;

    [SerializeField] private AudioSource _powerGeneratorAudioSource;
    [SerializeField] private AudioClip _powerGeneratorClip;

    [SerializeField] private AudioSource _electricArcAudioSource;
    [SerializeField] private AudioClip _electricArcClip;

    [Header("Arc Audio Settings")]
    [SerializeField] private float _arcMinDelay = 60f;
    [SerializeField] private float _arcMaxDelay = 120f;

    [Header("Events")]
    public UnityEvent OnLeverUp;
    public UnityEvent OnLeverDown;

    private XRSimpleInteractable _interactable;
    private Coroutine _electricArcCoroutine;

    #endregion

    private void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();

        if (_interactable != null)
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
        }

        if (_leverAnimator == null)
        {
            _leverAnimator = GetComponent<Animator>();
        }

        // Setup audio sources if not assigned
        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        // Get or create audio sources
        AudioSource[] audioSources = GetComponents<AudioSource>();

        if (_toggleAudioSource == null)
        {
            _toggleAudioSource = gameObject.AddComponent<AudioSource>();
        }
        ConfigureAudioSource(_toggleAudioSource, false, false);

        if (_electricShockAudioSource == null)
        {
            _electricShockAudioSource = gameObject.AddComponent<AudioSource>();
        }
        ConfigureAudioSource(_electricShockAudioSource, false, false);

        if (_powerGeneratorAudioSource == null)
        {
            _powerGeneratorAudioSource = gameObject.AddComponent<AudioSource>();
        }
        ConfigureAudioSource(_powerGeneratorAudioSource, false, true);

        if (_electricArcAudioSource == null)
        {
            _electricArcAudioSource = gameObject.AddComponent<AudioSource>();
        }
        ConfigureAudioSource(_electricArcAudioSource, false, false);
    }

    private void ConfigureAudioSource(AudioSource audioSource, bool playOnAwake, bool loop)
    {
        audioSource.playOnAwake = playOnAwake;
        audioSource.loop = loop;
        audioSource.spatialBlend = 1.0f; // Set to 3D spatial audio
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Make sure animator is enabled when lever is interacted with
        if (_leverAnimator != null && !_leverAnimator.enabled)
        {
            _leverAnimator.enabled = true;
        }

        ToggleState();
    }

    public void ToggleState()
    {
        _isOn = !_isOn;

        // Play toggle sound feedback
        PlayToggleSound();

        // Update animation if animator is available and enabled
        if (_leverAnimator != null)
        {
            if (!_leverAnimator.enabled)
            {
                _leverAnimator.enabled = true;
            }

            _leverAnimator.SetBool("LeverOn", _isOn);
        }

        // Trigger appropriate events
        if (_isOn)
        {
            PlayOnSounds();
            OnLeverUp?.Invoke();
        }
        else
        {
            StopAllAudio();
            OnLeverDown?.Invoke();
        }
    }

    private void PlayToggleSound()
    {
        if (_toggleAudioSource != null && _toggleSoundClip != null)
        {
            _toggleAudioSource.clip = _toggleSoundClip;
            _toggleAudioSource.Play();
        }
    }

    private void PlayOnSounds()
    {
        // Play electric shock sound
        if (_electricShockAudioSource != null && _electricShockClip != null)
        {
            _electricShockAudioSource.clip = _electricShockClip;
            _electricShockAudioSource.Play();
        }

        // Play power generator sound (looping)
        if (_powerGeneratorAudioSource != null && _powerGeneratorClip != null)
        {
            _powerGeneratorAudioSource.clip = _powerGeneratorClip;
            _powerGeneratorAudioSource.Play();
        }

        // Start electric arc random intervals
        if (_electricArcCoroutine != null)
        {
            StopCoroutine(_electricArcCoroutine);
        }
        _electricArcCoroutine = StartCoroutine(PlayElectricArcRandomly());
    }

    private void StopAllAudio()
    {
        // Stop power generator
        if (_powerGeneratorAudioSource != null && _powerGeneratorAudioSource.isPlaying)
        {
            _powerGeneratorAudioSource.Stop();
        }

        // Stop electric arc coroutine and audio
        if (_electricArcCoroutine != null)
        {
            StopCoroutine(_electricArcCoroutine);
            _electricArcCoroutine = null;
        }

        if (_electricArcAudioSource != null && _electricArcAudioSource.isPlaying)
        {
            _electricArcAudioSource.Stop();
        }
    }

    private IEnumerator PlayElectricArcRandomly()
    {
        while (_isOn)
        {
            // Wait for random delay (minimum 60 seconds)
            float delay = Random.Range(_arcMinDelay, _arcMaxDelay);
            yield return new WaitForSeconds(delay);

            // Play electric arc sound if lever is still on
            if (_isOn && _electricArcAudioSource != null && _electricArcClip != null)
            {
                _electricArcAudioSource.clip = _electricArcClip;
                _electricArcAudioSource.Play();
            }
        }
    }

    // Optional method to manually enable the animator
    public void EnableAnimator()
    {
        if (_leverAnimator != null)
        {
            _leverAnimator.enabled = true;
        }
    }

    // Clean up coroutine when object is destroyed
    private void OnDestroy()
    {
        if (_electricArcCoroutine != null)
        {
            StopCoroutine(_electricArcCoroutine);
        }
    }

    // Public methods for external control if needed
    public bool IsOn => _isOn;

    public void SetState(bool state)
    {
        if (_isOn != state)
        {
            ToggleState();
        }
    }
}