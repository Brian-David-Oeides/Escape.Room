using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDial : MonoBehaviour
{
    #region Variables

    [Header("Combination Settings")]
    [Range(0, 99)] public int[] correctCombination = new int[3];
    public float numberTolerance = 1f; // lowered slightly for better precision

    [Header("References")]
    public Animator doorAnimator;
    public AudioSource audioSource;

    [Header("Audio Clips")]
    public AudioClip dialTickSound;
    public AudioClip correctNumberSound;
    public AudioClip unlockSound;

    [Header("Audio Settings")]
    public float minTickVolume = 0.2f;
    public float maxTickVolume = 1f;
    public float maxRotationSpeed = 720f;

    [HideInInspector]
    public int currentDialNumber; // unified source for dial number

    private float _previousAngle;
    private int _lastPassedNumber = -1;
    private int _currentCombinationIndex = 0;
    private bool _isUnlocked = false;

    private float _correctNumberCooldown = 1.0f;
    private float _correctNumberTimer = 0f;

    [SerializeField] private XRGrabInteractable grabInteractable;

    // event for SafeDialUI
    public delegate void DialNumberChanged(int currentNumber);
    public event DialNumberChanged OnDialNumberChanged;

    #endregion
    #region Unity Events

    private void Awake()
    {
        // fallback if forgot to manually assign XR GRabInteractable
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
        else
        {
            Debug.LogError($"[SafeDial] Missing XRGrabInteractable on {gameObject.name}! Haptics and interaction will not work.");
        }
    }

    private void Update()
    {
        if (_isUnlocked) return;

        float currentAngle = transform.localEulerAngles.z;
        float deltaAngle = Mathf.DeltaAngle(_previousAngle, currentAngle);

        float rotationSpeed = Mathf.Abs(deltaAngle) / Time.deltaTime;

        // --- calculate the dial number once and store it ---
        float dialValue = Mathf.Repeat(currentAngle, 360f);
        currentDialNumber = Mathf.RoundToInt(dialValue / 3.6f);
        currentDialNumber = Mathf.Clamp(currentDialNumber, 0, 99); // safe clamp

        // play tick sound and notify UI when passing a new number
        if (currentDialNumber != _lastPassedNumber)
        {
            PlayTickSound(rotationSpeed);
            OnDialNumberChanged?.Invoke(currentDialNumber);
            _lastPassedNumber = currentDialNumber;
        }

        // check if landed on correct number
        _correctNumberTimer -= Time.deltaTime;

        if (_correctNumberTimer <= 0f)
        {
            if (Mathf.Abs(currentDialNumber - correctCombination[_currentCombinationIndex]) <= numberTolerance)
            {
                PlaySound(correctNumberSound);
                _correctNumberTimer = _correctNumberCooldown;
                _currentCombinationIndex++;

                if (_currentCombinationIndex >= correctCombination.Length)
                {
                    UnlockSafe();
                }
            }
        }

        _previousAngle = currentAngle;
    }

    #endregion

    #region Custom Methods

    private void OnGrab(SelectEnterEventArgs args)
    {
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = true;

        // notify UI
        FindObjectOfType<SafeDialUI>()?.OnDialGrabbed();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = true;

        // Notify UI
        FindObjectOfType<SafeDialUI>()?.OnDialReleased();
    }

   

    private void UnlockSafe()
    {
        _isUnlocked = true;
        PlaySound(unlockSound);

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("OpenDoor");
        }
    }

    private void PlayTickSound(float rotationSpeed)
    {
        if (audioSource != null && dialTickSound != null)
        {
            float volume = Mathf.Lerp(minTickVolume, maxTickVolume, Mathf.Clamp01(rotationSpeed / maxRotationSpeed));
            audioSource.PlayOneShot(dialTickSound, volume);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Public Getters

    // --- PUBLIC GETTERS for SafeDialUI ---
    public bool IsUnlocked => _isUnlocked;
    public int CurrentCombinationIndex => _currentCombinationIndex;
    public int[] CorrectCombination => correctCombination;
    public float NumberTolerance => numberTolerance;

    #endregion

}