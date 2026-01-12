using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class SwitchToggle : MonoBehaviour, ISaveable
{
    [SerializeField] private bool _isOn = false;

    [Header("Puzzle Lock")]
    [SerializeField] private bool startLocked = true;
    [SerializeField] private string saveID = "switch_main";

    private bool isUnlocked = false;

    [Header("Hint System")]
    [SerializeField] private string puzzleID = "light_switch";
    [SerializeField] private int hintThreshold = 2;

    [Header("Animation")]
    [SerializeField] private Animator _switchAnimator;
    [SerializeField] private string _animBoolParameterName = "SwitchOn";

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _switchOnSound;
    [SerializeField] private AudioClip _switchOffSound;

    [Header("Events")]
    public UnityEvent OnSwitchOn;
    public UnityEvent OnSwitchOff;

    private XRSimpleInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();

        if (_interactable != null)
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);

            // NEW: Start locked if configured
            if (startLocked)
            {
                // Keep interactable enabled to detect failed attempts
                // Lock state is checked in OnSelectEntered instead
                Debug.Log("[SwitchToggle] Switch locked - waiting for flashlight beam");
            }
            else
            {
                isUnlocked = true;
            }
        }
        else
        {
            Debug.LogError("No XRSimpleInteractable found on this GameObject!");
        }

        if (_switchAnimator == null)
        {
            _switchAnimator = GetComponent<Animator>();
        }

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        UpdateAnimationState();
    }

    /// <summary>
    /// Called by FlashLightSwitchActivator when beam hits switch
    /// Permanently unlocks the switch
    /// </summary>
    public void UnlockSwitch()
    {
        if (!isUnlocked && _interactable != null)
        {
            _interactable.enabled = true;
            isUnlocked = true;
            Debug.Log($"[SwitchToggle] Switch {saveID} permanently unlocked!");
        }
    }


    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Check if switch is locked
        if (startLocked && !isUnlocked)
        {
            // Player tried to use locked switch - register failed attempt
            ClueManager.Instance?.RegisterFailedAttempt(puzzleID, hintThreshold);
            Debug.Log($"[SwitchToggle] Attempted to use locked switch - hint tracking registered");
            return; // Don't toggle
        }

        ToggleState();
    }

    public void ToggleState()
    {
        _isOn = !_isOn;
        UpdateAnimationState();
        PlaySwitchSound();

        if (_isOn)
        {
            OnSwitchOn?.Invoke();
        }
        else
        {
            OnSwitchOff?.Invoke();
        }
    }

    private void UpdateAnimationState()
    {
        if (_switchAnimator != null)
        {
            _switchAnimator.SetBool(_animBoolParameterName, _isOn);
        }
    }

    private void PlaySwitchSound()
    {
        if (_audioSource != null)
        {
            AudioClip clipToPlay = _isOn ? _switchOnSound : _switchOffSound;

            if (clipToPlay != null)
            {
                _audioSource.PlayOneShot(clipToPlay);
            }
            else
            {
                Debug.LogWarning($"Missing audio clip for switch {(_isOn ? "ON" : "OFF")} state!");
            }
        }
        else
        {
            Debug.LogWarning("No AudioSource assigned to SwitchToggle!");
        }
    }

    // Public method to manually turn on the switch
    public void TurnOn()
    {
        if (!_isOn)
        {
            ToggleState();
        }
    }

    // Public method to manually turn off the switch
    public void TurnOff()
    {
        if (_isOn)
        {
            ToggleState();
        }
    }

    #region ISaveable Implementation

    public string SaveID => saveID;

    public void SaveState(SaveData saveData)
    {
        // Save unlock state
        if (isUnlocked && !saveData.completedPuzzleIDs.Contains(saveID))
        {
            saveData.completedPuzzleIDs.Add(saveID);
            Debug.Log($"[SwitchToggle] Saved unlock state for {saveID}");
        }
    }

    public void LoadState(SaveData saveData)
    {
        // Check if switch was unlocked
        isUnlocked = saveData.completedPuzzleIDs.Contains(saveID);

        if (isUnlocked)
        {
            // Restore unlocked state
            if (_interactable != null)
            {
                _interactable.enabled = true;
            }
            Debug.Log($"[SwitchToggle] Loaded unlocked state for {saveID}");
        }
        else if (startLocked)
        {
            // Still locked
            if (_interactable != null)
            {
                _interactable.enabled = false;
            }
            Debug.Log($"[SwitchToggle] {saveID} still locked");
        }
    }

    #endregion
}
