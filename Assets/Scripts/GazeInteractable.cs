using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GazeInteractable : MonoBehaviour
{
    [Header("Gaze Settings")]
    public float gazeTime = 0.2f;

    [Header("UI References")]
    public GameObject viewPromptUI;

    [Header("Audio")]
    public AudioClip gazeStartSound;
    public AudioClip activateSound;

    [Header("Events")]
    public UnityEvent onGazeStarted;
    public UnityEvent onGazeEnded;
    public UnityEvent onActivated; // When A button is pressed
    public UnityEvent onDismissed; // When B button is pressed during prompt

    #region Private Variables

    private enum GazeState
    {
        Idle,
        ShowingPrompt
    }

    private GazeState currentState = GazeState.Idle;
    private bool _isGazing = false;
    private float _gazeTimer = 0f;
    private AudioSource _audioSource;

    #endregion

    #region Unity Events

    private void Awake()
    {
        SetupAudioSource();

        if (viewPromptUI != null)
            viewPromptUI.SetActive(false);

        // Validate collider
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("GazeInteractable requires a Collider component on " + gameObject.name);
        }
    }

    private void Update()
    {
        CheckForGaze();
        CheckButtonInputs();
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

    #region Gaze Detection

    private void CheckForGaze()
    {
        Transform cameraTransform = Camera.main?.transform;
        if (cameraTransform == null) return;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, 10f))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                if (!_isGazing)
                {
                    StartGazing();
                }

                _gazeTimer += Time.deltaTime;

                if (_gazeTimer >= gazeTime && currentState == GazeState.Idle)
                {
                    ShowPrompt();
                }
            }
            else
            {
                HandleGazeExit();
            }
        }
        else
        {
            HandleGazeExit();
        }
    }

    private void StartGazing()
    {
        _isGazing = true;
        _gazeTimer = 0f;

        PlaySound(gazeStartSound);
        onGazeStarted?.Invoke();

        Debug.Log("Started gazing at " + gameObject.name);
    }

    private void HandleGazeExit()
    {
        if (_isGazing)
        {
            _isGazing = false;
            _gazeTimer = 0f;

            if (currentState == GazeState.ShowingPrompt)
            {
                HidePrompt();
            }

            onGazeEnded?.Invoke();
        }
    }

    #endregion

    #region Prompt Management

    private void ShowPrompt()
    {
        currentState = GazeState.ShowingPrompt;

        if (viewPromptUI != null)
        {
            viewPromptUI.SetActive(true);

            // Update prompt text
            Text promptText = viewPromptUI.GetComponentInChildren<Text>();
            if (promptText != null)
                promptText.text = "A to View";
        }

        Debug.Log("Showing view prompt for " + gameObject.name);
    }

    private void HidePrompt()
    {
        currentState = GazeState.Idle;

        if (viewPromptUI != null)
            viewPromptUI.SetActive(false);
    }

    #endregion

    #region Input Handling

    private void CheckButtonInputs()
    {
        // A button - Activate
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand)
            .TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool aButtonPressed))
        {
            if (aButtonPressed && currentState == GazeState.ShowingPrompt)
            {
                ActivateInteraction();
            }
        }

        // B button - Dismiss prompt
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand)
            .TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bButtonPressed))
        {
            if (bButtonPressed && currentState == GazeState.ShowingPrompt)
            {
                DismissPrompt();
            }
        }
    }

    private void ActivateInteraction()
    {
        HidePrompt();
        PlaySound(activateSound);
        onActivated?.Invoke();

        Debug.Log("Activated interaction on " + gameObject.name);
    }

    private void DismissPrompt()
    {
        HidePrompt();
        onDismissed?.Invoke();

        Debug.Log("Dismissed prompt on " + gameObject.name);
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

    public void ForceShowPrompt()
    {
        if (currentState == GazeState.Idle)
            ShowPrompt();
    }

    public void ForceHidePrompt()
    {
        if (currentState == GazeState.ShowingPrompt)
            HidePrompt();
    }

    public bool IsShowingPrompt()
    {
        return currentState == GazeState.ShowingPrompt;
    }

    #endregion
}