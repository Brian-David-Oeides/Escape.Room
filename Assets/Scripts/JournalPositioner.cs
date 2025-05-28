using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class JournalPositioner : MonoBehaviour
{
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
    public float gazeTime = 1.0f;

    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;

    private bool _isGazing = false;
    private float _gazeTimer = 0f;
    private bool _hasTriggered = false;
    private bool _isMoving = false;
    private bool _movingToUser = false;

    private AudioSource _audioSource;

    // Add debugging fields
    [Header("Debug Info")]
    [SerializeField] private Vector3 lastFrameRotationOffset;
    [SerializeField] private bool debugRotationChanges = true;

    private void Awake()
    {
        // Force set the rotation offset
        rotationOffset = new Vector3(90f, 0, 0);
        lastFrameRotationOffset = rotationOffset;

        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _targetPosition = _originalPosition;
        _targetRotation = _originalRotation;

        // Set up audio source
        SetupAudioSource();

        Debug.Log($"Awake: Rotation offset set to {rotationOffset}");
    }

    private void Start()
    {
        // Double-check in Start as well
        rotationOffset = new Vector3(90f, 0, 0);
        Debug.Log($"Start: Rotation offset confirmed as {rotationOffset}");
    }

    private void SetupAudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure audio source for 3D spatial audio
        _audioSource.spatialBlend = 1.0f; // Full 3D
        _audioSource.volume = audioVolume;
        _audioSource.playOnAwake = false;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.maxDistance = 10f;

        //Debug.Log("Audio source configured for journal");
    }

    private void Update()
    {
        // Check for rotation offset changes
        if (debugRotationChanges && lastFrameRotationOffset != rotationOffset)
        {
            Debug.LogWarning($"Rotation offset changed from {lastFrameRotationOffset} to {rotationOffset}");
            Debug.LogWarning($"Stack trace: {System.Environment.StackTrace}");

            // Force it back to correct value
            rotationOffset = new Vector3(90f, 0, 0);
        }
        lastFrameRotationOffset = rotationOffset;

        CheckForGaze();
        HandleMovement();
    }

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
                    _isGazing = true;
                    _gazeTimer = 0f;
                    //Debug.Log("Started gazing at journal");
                }

                _gazeTimer += Time.deltaTime;

                if (_gazeTimer >= gazeTime && !_hasTriggered && !_isMoving)
                {
                    //Debug.Log("Gaze time reached - moving journal");
                    StartMoveToUser();
                    _hasTriggered = true;
                }
            }
            else
            {
                if (_isGazing)
                {
                    //Debug.Log("Stopped gazing at journal");
                    _isGazing = false;
                    _gazeTimer = 0f;

                    if (_hasTriggered && !_isMoving)
                    {
                        StartReturnToOriginal();
                        _hasTriggered = false;
                    }
                }
            }
        }
        else
        {
            if (_isGazing)
            {
                //Debug.Log("No longer looking at anything");
                _isGazing = false;
                _gazeTimer = 0f;

                if (_hasTriggered && !_isMoving)
                {
                    StartReturnToOriginal();
                    _hasTriggered = false;
                }
            }
        }
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
                //Debug.Log("Movement completed");
            }
        }
    }

    private void StartMoveToUser()
    {
        Transform head = Camera.main?.transform;
        if (head == null)
        {
            //Debug.LogError("Main Camera not found!");
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

        _isMoving = true;
        _movingToUser = true;

        // Play pickup sound
        PlayPickupSound();

        //Debug.Log("Starting smooth move to user with pickup sound");
    }

    private void StartReturnToOriginal()
    {
        _targetPosition = _originalPosition;
        _targetRotation = _originalRotation;
        _isMoving = true;
        _movingToUser = false;

        // Play putdown sound
        PlayPutdownSound();

        //Debug.Log("Starting smooth return to original position with putdown sound");
    }

    private void PlayPickupSound()
    {
        if (playAudioOnPickup && pickupSound != null && _audioSource != null)
        {
            _audioSource.clip = pickupSound;
            _audioSource.volume = audioVolume;
            _audioSource.Play();
            //Debug.Log("Playing pickup sound");
        }
    }

    private void PlayPutdownSound()
    {
        if (playAudioOnPutdown && putdownSound != null && _audioSource != null)
        {
            _audioSource.clip = putdownSound;
            _audioSource.volume = audioVolume;
            _audioSource.Play();
            //Debug.Log("Playing putdown sound");
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
}