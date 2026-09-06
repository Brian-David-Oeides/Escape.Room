using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;

/// <summary>
/// Safe dial combination lock puzzle with save system integration.
/// Inherits from PuzzleBase for automatic ISaveable implementation.
/// Requires correct 3-number sequence to unlock.
/// </summary>
/// 

public class SafeDial : PuzzleBase // CHANGE 1: Inherit from PuzzleBase instead of MonoBehaviour
{
    #region Variables

    [Header("Combination Settings")]
    [Range(0, 99)] public int[] correctCombination = new int[3];
    public float numberTolerance = 1f;

    [Header("References")]
    public Animator doorAnimator;
    public AudioSource audioSource;

    [SerializeField] 
    private MonoBehaviour hoverVisual;  // Reference to SafeDialHoverVisual

    [Header("Audio Clips")]
    public AudioClip dialTickSound;
    public AudioClip correctNumberSound;
    public AudioClip unlockSound;

    // CHANGE 2: Remove OnSafeUnlocked UnityEvent (use OnPuzzleCompleted from PuzzleBase)
    // [Header("Events")]
    // public UnityEvent OnSafeUnlocked;  ← REMOVED

    [Header("Audio Settings")]
    public float minTickVolume = 0.2f;
    public float maxTickVolume = 1f;
    public float maxRotationSpeed = 720f;

    [Header("Puzzle Lock")]
    [SerializeField]
    [Tooltip("Should dial be locked until both keys are inserted?")]
    private bool requireKeysInserted = true;

    private bool _keysInserted = false;

    [HideInInspector]
    public int currentDialNumber;

    private float _previousAngle;
    private int _lastPassedNumber = -1;
    private int _currentCombinationIndex = 0;
    private bool _isUnlocked = false;  // Keep for internal logic

    private float _correctNumberCooldown = 1.0f;
    private float _correctNumberTimer = 0f;

    private float _wrongNumberDetectionTime = 1.5f; // How long player must hold wrong number
    private float _wrongNumberTimer = 0f;
    private int _lastCheckedNumber = -1;
    private bool _wrongNumberDetected = false;
    private bool _hasBeenGrabbed = false; // Gates wrong-number detection - an untouched dial should never accumulate a "wrong attempt"

    [SerializeField] private XRGrabInteractable grabInteractable;

    // event for SafeDialUI
    public delegate void DialNumberChanged(int currentNumber);
    public event DialNumberChanged OnDialNumberChanged;

    #endregion

    #region Unity Events

    private void Awake()
    {
        // fallback if forgot to manually assign XR GrabInteractable
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);

            // Lock dial if required
            if (requireKeysInserted)
            {
                grabInteractable.enabled = false;
                if (hoverVisual != null)
                {
                    hoverVisual.enabled = false;
                }
                GameLog.Log("[SafeDial] Dial locked - keys must be inserted first");
            }
        }
        else
        {
            GameLog.LogError($"[SafeDial] Missing XRGrabInteractable on {gameObject.name}!");
        }
    }

    protected override void Start()  // CHANGE 3: Add override and call base.Start()
    {
        base.Start(); // CRITICAL: Loads saved completion state!

        // If already unlocked from save, disable dial interaction
        if (isCompleted)
        {
            _isUnlocked = true;
            DisableDialInteraction();
        }
    }

    /// <summary>
    /// Called via UnityEvent when both bronze keys are inserted
    /// Enables the dial XRGrabInteractable
    /// </summary>
    public void EnableDialInteraction()
    {
        if (grabInteractable != null)
        {
            _keysInserted = true;
            grabInteractable.enabled = true;

            if (hoverVisual != null)
            {
                hoverVisual.enabled = true;
            }

            GameLog.Log("[SafeDial] Dial unlocked! Keys inserted - can now rotate dial.");
        }
    }

    /// <summary>
    /// Called via UnityEvent when a key is removed (optional - for re-locking)
    /// Disables the dial if user removes a key
    /// </summary>
    public void DisableDialInteraction()
    {
        if (requireKeysInserted && grabInteractable != null && !_isUnlocked)
        {
            _keysInserted = false;
            grabInteractable.enabled = false;
            GameLog.Log("[SafeDial] Dial locked! Key removed.");
        }
    }

    private void Update()
    {
        // CHANGE 4: Use isCompleted from PuzzleBase instead of just _isUnlocked
        if (isCompleted || _isUnlocked) return;

        float currentAngle = transform.localEulerAngles.z;
        float deltaAngle = Mathf.DeltaAngle(_previousAngle, currentAngle);

        float rotationSpeed = Mathf.Abs(deltaAngle) / Time.deltaTime;

        // calculate the dial number once and store it
        float dialValue = Mathf.Repeat(currentAngle, 360f);
        currentDialNumber = Mathf.RoundToInt(dialValue / 3.6f);
        currentDialNumber = Mathf.Clamp(currentDialNumber, 0, 99);

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
            int expectedNumber = correctCombination[_currentCombinationIndex];

            // Check if on correct number
            if (Mathf.Abs(currentDialNumber - expectedNumber) <= numberTolerance)
            {
                PlaySound(correctNumberSound);
                _correctNumberTimer = _correctNumberCooldown;
                _currentCombinationIndex++;

                // Reset wrong number detection
                _wrongNumberTimer = 0f;
                _wrongNumberDetected = false;

                if (_currentCombinationIndex >= correctCombination.Length)
                {
                    UnlockSafe();
                }
            }
            // Check if player is holding on a CLEARLY wrong number
            // Gated on _hasBeenGrabbed: an untouched dial resting at its default/restored
            // rotation must never accumulate wrong-number dwell time on its own - only
            // genuine player-held rotation should count toward a failed attempt.
            else if (_hasBeenGrabbed && Mathf.Abs(currentDialNumber - expectedNumber) > numberTolerance * 3)
            {
                // Player is far from correct number
                if (currentDialNumber == _lastCheckedNumber)
                {
                    // Still on same wrong number - increment timer
                    _wrongNumberTimer += Time.deltaTime;

                    if (_wrongNumberTimer >= _wrongNumberDetectionTime && !_wrongNumberDetected)
                    {
                        // Player held wrong number long enough - count as failed attempt
                        RegisterFailedAttempt();
                        _wrongNumberDetected = true;
                        _wrongNumberTimer = 0f;
                    }
                }
                else
                {
                    // Moved to different number - reset timer
                    _lastCheckedNumber = currentDialNumber;
                    _wrongNumberTimer = 0f;
                    _wrongNumberDetected = false;
                }
            }
            else
            {
                // Number is close but not quite right (within tolerance * 3) - reset detection
                _wrongNumberTimer = 0f;
                _wrongNumberDetected = false;
            }
        }

        _previousAngle = currentAngle;
    }

    #endregion

    #region Custom Methods

    private void OnGrab(SelectEnterEventArgs args)
    {
        // CHANGE 5: Prevent grabbing if already completed
        if (isCompleted)
        {
            DebugLog("Safe already unlocked - ignoring dial interaction");
            return;
        }

        _hasBeenGrabbed = true;

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

        // CHANGE 6: Call CompletePuzzle() instead of OnSafeUnlocked.Invoke()
        CompletePuzzle();

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("OpenDoor");
        }
    }

    /// <summary>
    /// Override CompletePuzzle to disable dial interaction when solved
    /// </summary>
    public override void CompletePuzzle()
    {
        DebugLog($"Safe unlocked! Combination: [{string.Join(", ", correctCombination)}]");

        // Call base to handle save system and fire OnPuzzleCompleted event
        base.CompletePuzzle();

        // Disable dial interaction
        DisableDialInteraction();
    }

    /// <summary>
    /// Override ApplyCompletedStateVisuals to disable dial when loading completed state
    /// </summary>
    protected override void ApplyCompletedStateVisuals()
    {
        // Call base to handle colliders/renderers
        base.ApplyCompletedStateVisuals();

        // Disable dial interaction
        _isUnlocked = true;
        DisableDialInteraction();

        // Ensure door animator is in unlocked state
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

    private void RegisterFailedAttempt()
    {
        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.RegisterFailedAttempt(puzzleID);
            DebugLog($"Wrong number detected: {currentDialNumber} (expected: {correctCombination[_currentCombinationIndex]})");
        }
    }

    #endregion

    #region Public Getters

    // PUBLIC GETTERS for SafeDialUI
    public bool IsUnlocked => _isUnlocked || isCompleted;  // CHANGE 7: Include isCompleted check
    public int CurrentCombinationIndex => _currentCombinationIndex;
    public int[] CorrectCombination => correctCombination;
    public float NumberTolerance => numberTolerance;

    #endregion
}