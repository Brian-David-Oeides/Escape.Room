using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class JournalPositioner : MonoBehaviour
{
    #region Variables

    [Header("Distance and Offset")]
    public float distanceFromHead = 0.8f;
    public Vector3 verticalOffset = new Vector3(0, -0.1f, 0);

    [Header("Rotation Settings")]
    public Vector3 rotationOffset = new Vector3(90f, 0, 0);
    public bool useCustomRotation = false;
    public Vector3 customRotation = new Vector3(0, 0, 0);

    [Header("Animation Settings")]
    public float moveSpeed = 2.0f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio Settings")]
    public AudioClip pickupSound;
    public AudioClip putdownSound;
    [Range(0f, 1f)]
    public float audioVolume = 1.0f;
    public bool playAudioOnPickup = true;
    public bool playAudioOnPutdown = true;

    [Header("Gaze Settings")]
    public float gazeTime = 0.2f; // Time in seconds to gaze before showing prompt

    [Header("UI Prompt Settings")]
    public GameObject viewPromptUI; // UI element showing "Press A to view"

    // State management
    private enum _JournalState
    {
        AtOriginalPosition,
        ShowingPrompt,
        MovingToUser,
        InFrontOfUser,
        MovingToOriginal
    }

    private _JournalState currentState = _JournalState.AtOriginalPosition;

    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;

    private bool _isGazing = false;
    private float _gazeTimer = 0f;
    private bool _isMoving = false;

    private AudioSource _audioSource;
    private PageTurnInteractable _pageTurner;

    #endregion

    #region Unity Events

    private void Awake()
    {
        rotationOffset = new Vector3(90f, 0, 0);

        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _targetPosition = _originalPosition;
        _targetRotation = _originalRotation;

        SetupAudioSource();

        // Get reference to the PageTurnInteractable component
        _pageTurner = GetComponent<PageTurnInteractable>();
        if (_pageTurner == null)
        {
            Debug.LogWarning("PageTurnInteractable component not found on " + gameObject.name);
        }

        // Hide prompt UI initially
        if (viewPromptUI != null)
        {
            viewPromptUI.SetActive(false);
        }
    }

    private void Update()
    {
        CheckForGaze();
        HandleMovement();
        CheckButtonInputs();
    }

    #endregion

    #region Custom Methods

    private void SetupAudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.spatialBlend = 1.0f;
        _audioSource.volume = audioVolume;
        _audioSource.playOnAwake = false;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.maxDistance = 10f;
    }

    

    private void CheckButtonInputs()
    {
        // Check for A button (primary button on right controller)
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool aButtonPressed))
        {
            if (aButtonPressed && currentState == _JournalState.ShowingPrompt)
            {
                Debug.Log("A button detected - Moving journal to user");
                HideViewPrompt();
                StartMoveToUser();
            }
        }

        // Check for B button (secondary button on right controller)  
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bButtonPressed))
        {
            if (bButtonPressed && currentState == _JournalState.InFrontOfUser)
            {
                Debug.Log("B button detected - Returning journal to original position");
                StartReturnToOriginal();
            }
            else if (bButtonPressed && currentState == _JournalState.ShowingPrompt)
            {
                Debug.Log("B button detected - Dismissing prompt without viewing");
                HideViewPrompt();
            }
        }
    }

    private void CheckForGaze()
    {
        // Only check for gaze when in original position
        if (currentState != _JournalState.AtOriginalPosition)
        {
            Debug.Log($"Not checking gaze - current state: {currentState}");
            return;
        }

        Transform cameraTransform = Camera.main?.transform;
        if (cameraTransform == null) return;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, 10f))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                Debug.Log($"Raycast hit journal - isGazing: {_isGazing}, gazeTimer: {_gazeTimer}");

                if (!_isGazing)
                {
                    _isGazing = true;
                    _gazeTimer = 0f;
                    Debug.Log("Started gazing at journal");
                }

                _gazeTimer += Time.deltaTime;

                if (_gazeTimer >= gazeTime && currentState == _JournalState.AtOriginalPosition)
                {
                    //Debug.Log("Gaze time reached - calling ShowViewPrompt()");
                    ShowViewPrompt();
                }
            }
            else
            {
                //Debug.Log("Raycast didn't hit anything");
                HandleGazeExit();
            }
        }
        else
        {
            HandleGazeExit();
        }
    }

    private void HandleGazeExit()
    {
        if (_isGazing && currentState == _JournalState.ShowingPrompt)
        {
            HideViewPrompt();
        }
        _isGazing = false;
        _gazeTimer = 0f;
    }

    private void ShowViewPrompt()
    {
        //Debug.Log("ShowViewPrompt() called");
        currentState = _JournalState.ShowingPrompt;
        if (viewPromptUI != null)
        {
            //Debug.Log("Activating viewPromptUI");
            viewPromptUI.SetActive(true);

            // Update the text to show A to View
            Text promptText = viewPromptUI.GetComponentInChildren<Text>();
            if (promptText != null)
            {
                promptText.text = "A to View";
            }
        }
        else
        {
            //Debug.LogError("viewPromptUI is null! Make sure you assigned the UI Canvas in the inspector.");
        }
        //Debug.Log("Showing view prompt - Press A to view journal");
    }

    // show the dismiss prompt when journal is in front of user
    private void ShowDismissPrompt()
    {
        if (viewPromptUI != null)
        {
            viewPromptUI.SetActive(true);

            // Update the text to show B to Dismiss
            Text promptText = viewPromptUI.GetComponentInChildren<Text>();
            if (promptText != null)
            {
                promptText.text = "B to Dismiss";
            }
        }
    }

    private void HideViewPrompt()
    {
        currentState = _JournalState.AtOriginalPosition;
        if (viewPromptUI != null)
        {
            viewPromptUI.SetActive(false);
        }
        Debug.Log("Hiding view prompt");
    }

    private void HandleMovement()
    {
        if (_isMoving)
        {
            float step = moveSpeed * Time.deltaTime;

            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, step);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, step * 90f);

            if (Vector3.Distance(transform.position, _targetPosition) < 0.01f &&
                Quaternion.Angle(transform.rotation, _targetRotation) < 1f)
            {
                transform.position = _targetPosition;
                transform.rotation = _targetRotation;
                _isMoving = false;

                // Handle state transitions based on movement completion
                if (currentState == _JournalState.MovingToUser)
                {
                    currentState = _JournalState.InFrontOfUser;

                    // Disable journal collider to prevent gaze interference
                    DisableJournalCollider();

                    if (_pageTurner != null)
                    {
                        _pageTurner.EnablePageTurning();
                    }

                    ShowDismissPrompt(); // Show B to Dismiss prompt
                    // Debug.Log("Journal positioned in front of user - Press B to return");
                }
                else if (currentState == _JournalState.MovingToOriginal)
                {
                    currentState = _JournalState.AtOriginalPosition;

                    // Re-enable journal collider for gaze detection
                    EnableJournalCollider();

                    // Debug.Log("Journal returned to original position");
                }
            }
        }
    }

    private void StartMoveToUser()
    {
        Transform head = Camera.main?.transform;
        if (head == null)
        {
            return;
        }

        _targetPosition = head.position + head.forward * distanceFromHead + verticalOffset;

        if (useCustomRotation)
        {
            _targetRotation = Quaternion.Euler(customRotation);
        }
        else
        {
            Vector3 directionToUser = head.position - _targetPosition;
            Quaternion baseRotation = Quaternion.LookRotation(directionToUser.normalized);
            Quaternion offsetRotation = Quaternion.Euler(rotationOffset);
            _targetRotation = baseRotation * offsetRotation;
        }

        currentState = _JournalState.MovingToUser;
        _isMoving = true;

        // disable collider during movement to prevent interference
        DisableJournalCollider();

        PlayPickupSound();
    }

    private void StartReturnToOriginal()
    {
        _targetPosition = _originalPosition;
        _targetRotation = _originalRotation;
        currentState = _JournalState.MovingToOriginal;
        _isMoving = true;

        // Hide all UI prompts
        HideAllPrompts();

        // Disable page turning and reset pages
        if (_pageTurner != null)
        {
            _pageTurner.DisablePageTurning();
            _pageTurner.ResetPages();
        }

        PlayPutdownSound();
    }

    private void HideAllPrompts()
    {
        if (viewPromptUI != null)
        {
            viewPromptUI.SetActive(false);
            Debug.Log("UI prompt hidden");
        }
    }

    private void DisableJournalCollider()
    {
        Collider journalCollider = GetComponent<Collider>();
        if (journalCollider != null)
        {
            journalCollider.enabled = false;
            // Debug.Log("Journal collider disabled - preventing gaze interference");
        }
    }

    private void EnableJournalCollider()
    {
        Collider journalCollider = GetComponent<Collider>();
        if (journalCollider != null)
        {
            journalCollider.enabled = true;
            // Debug.Log("Journal collider enabled - gaze detection restored");
        }
    }

    private void PlayPickupSound()
    {
        if (playAudioOnPickup && pickupSound != null && _audioSource != null)
        {
            _audioSource.clip = pickupSound;
            _audioSource.volume = audioVolume;
            _audioSource.Play();
        }
    }

    private void PlayPutdownSound()
    {
        if (playAudioOnPutdown && putdownSound != null && _audioSource != null)
        {
            _audioSource.clip = putdownSound;
            _audioSource.volume = audioVolume;
            _audioSource.Play();
        }
    }

    // Keep these methods for backward compatibility
    public void MoveInFrontOfUser()
    {
        StartMoveToUser();
    }

    public void ReturnToOriginalPosition()
    {
        StartReturnToOriginal();
    }

    #endregion
}