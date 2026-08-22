/*
 * SocketRotator.cs
 * 
 * Copyright © 2025 Brian David
 * All Rights Reserved
 *
 * A Unity script for VR interactions that allows a wrench or tool to be 
 * rotated within a socket based on hand movement in VR, with precise 
 * control over rotation limits and locking behavior.
 * 
 * Created: May 12, 2025
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketRotator : MonoBehaviour, ISaveable
{
    #region Variables

    [Header("References")]
    public XRGrabInteractable wrench;
    public Transform socketPivot;
    public Collider socketTrigger;

    [Header("Save System")]
    [SerializeField] private string socketID = "socket_valve";

    [Header("Hint System")]
    [SerializeField] private string puzzleID = "wrench_valve";
    [SerializeField] private int hintThreshold = 1;

    [Header("Rotation Settings")]
    public float maxRotationX = 90f;  
    public float rotationSpeed = 75f;

    [Header("Events")]
    public UnityEvent onFullyTurned;

    // Control variables
    private XRBaseInteractor interactor;
    private float currentXRotation = 0f;  
    private bool isGrabbed = false;
    private bool isSocketed = false;
    private bool eventFired = false;

    // Tracking variables
    private float _grabStartRotation = 0f;
    private float _wrenchStartRotation = 0f;
    private Vector3 _lastHandPosition = Vector3.zero;

    #endregion

    #region Unity Events

    private void Start()
    {
        // Listen for XRSocketInteractor component when object is socketed
        XRSocketInteractor socketInteractor = GetComponent<XRSocketInteractor>();
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(OnSocketed);
        }
    }

    private void OnEnable()
    {
        wrench.selectEntered.AddListener(OnGrab);
        wrench.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        wrench.selectEntered.RemoveListener(OnGrab);
        wrench.selectExited.RemoveListener(OnRelease);
    }

    private void LateUpdate()
    {
        if (eventFired && isSocketed)
        {
            // Force lock position (CustomsocketInteractor.cs was updating it)
            EnforceLocking();
        }
    }

    private void Update()
    {
        if (!isGrabbed || !isSocketed || interactor == null) return;

        if (eventFired)
        {
            // Keep the wrench locked at max rotation
            wrench.transform.position = socketPivot.position;
            wrench.transform.rotation = Quaternion.Euler(
                socketPivot.rotation.eulerAngles.x + maxRotationX,
                socketPivot.rotation.eulerAngles.y,
                socketPivot.rotation.eulerAngles.z);
            return;
        }

        // Get current hand position in socket's local space
        Vector3 handPos = socketPivot.InverseTransformPoint(interactor.transform.position);

        if (_lastHandPosition == Vector3.zero)
        {
            _lastHandPosition = handPos;
            return;
        }

        // Calculate rotation based on hand movement around the pivot
        // Now tracking movement in YZ plane (around X axis)
        float angle = CalculateRotationAngle(handPos);
        float rotationDelta = angle * rotationSpeed * Time.deltaTime;

        // Update rotation within constraints
        currentXRotation = Mathf.Clamp(currentXRotation + rotationDelta, 0f, maxRotationX);

        // Apply the rotation to the wrench while keeping it at the socket position
        wrench.transform.position = socketPivot.position;
        wrench.transform.rotation = Quaternion.Euler(
            socketPivot.rotation.eulerAngles.x + currentXRotation,
            socketPivot.rotation.eulerAngles.y,
            socketPivot.rotation.eulerAngles.z);

        _lastHandPosition = handPos;

        // Trigger event once fully rotated
        if (currentXRotation >= maxRotationX && !eventFired)
        {
            onFullyTurned.Invoke();
            PuzzleManager.Instance?.RegisterPuzzleCompletion(puzzleID);
            eventFired = true;
            currentXRotation = maxRotationX;
        }
    }

    #endregion

    #region Custom Methods

    private void OnSocketed(SelectEnterEventArgs args)
    {
        // Fix the comparison to check if the interactable object is our wrench
        IXRInteractable interactableObj = args.interactableObject;

        if (eventFired && interactableObj != null &&
            interactableObj.transform.gameObject == wrench.gameObject)
        {
            // Lock the wrench at max rotation position
            wrench.transform.position = socketPivot.position;
            wrench.transform.rotation = Quaternion.Euler(
                socketPivot.rotation.eulerAngles.x + maxRotationX,
                socketPivot.rotation.eulerAngles.y,
                socketPivot.rotation.eulerAngles.z);

            // Make sure physics don't affect it
            Rigidbody rb = wrench.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        interactor = args.interactorObject as XRBaseInteractor;
        isGrabbed = true;

        if (isSocketed)
        {
            // Keep the wrench socketed but allow it to rotate
            wrench.trackPosition = false;
            wrench.trackRotation = false;

            // Reset rotation tracking variables
            _lastHandPosition = socketPivot.InverseTransformPoint(interactor.transform.position);
            _wrenchStartRotation = currentXRotation;
        }
    }

    public bool IsWrenchLocked()
    {
        return eventFired;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        interactor = null;
        _lastHandPosition = Vector3.zero;

        // Check if wrench was released inside the socket
        if (socketTrigger.bounds.Contains(wrench.transform.position))
        {
            isSocketed = true;

            // IMPORTANT: If already reached max rotation, lock the wrench 
            // at the correct rotation even when released
            if (eventFired)
            {
                // Keep the rotation locked at max
                wrench.transform.position = socketPivot.position;
                wrench.transform.rotation = Quaternion.Euler(
                    socketPivot.rotation.eulerAngles.x + maxRotationX,
                    socketPivot.rotation.eulerAngles.y,
                    socketPivot.rotation.eulerAngles.z);

                // Make the wrench kinematic to prevent physics from affecting it
                Rigidbody rb = wrench.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
            }
            else if (currentXRotation > 10f) // NEW: Player tried but didn't complete rotation
            {
                ClueManager.Instance?.RegisterFailedAttempt(puzzleID);
                Debug.Log($"[SocketRotator] Failed attempt registered - rotation was {currentXRotation:F2}° (needed {maxRotationX}°)");
            }
        }
        else
        {
            isSocketed = false;
            wrench.trackPosition = true;
            wrench.trackRotation = true;

            // Only reset the event fired state if removed from socket
            eventFired = false;
            currentXRotation = 0f;
        }
    }

    public void Sabotage()
    {
        currentXRotation = 0f;
        eventFired = false;

        if (wrench != null && socketPivot != null)
        {
            // Reset wrench visual transform back to the socket's un-turned base rotation
            wrench.transform.position = socketPivot.position;
            wrench.transform.rotation = Quaternion.Euler(
                socketPivot.rotation.eulerAngles.x,
                socketPivot.rotation.eulerAngles.y,
                socketPivot.rotation.eulerAngles.z);

            // Un-lock physics and interaction so the wrench can be grabbed and turned again
            Rigidbody rb = wrench.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            wrench.enabled = true;
        }

        PuzzleManager.Instance?.UnregisterPuzzleCompletion(puzzleID);
    }

    // enforce locking from outside scripts
    public void EnforceLocking()
    {
        if (eventFired)
        {
            wrench.transform.position = socketPivot.position;
            wrench.transform.rotation = Quaternion.Euler(
                socketPivot.rotation.eulerAngles.x + maxRotationX,
                socketPivot.rotation.eulerAngles.y,
                socketPivot.rotation.eulerAngles.z);

            Rigidbody rb = wrench.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }  

    // Calculate rotation based on hand movement relative to the socket
    private float CalculateRotationAngle(Vector3 handPos)
    {
        // Convert hand position to angle in the YZ plane (around X axis)
        float handAngle = Mathf.Atan2(handPos.z, handPos.y) * Mathf.Rad2Deg;
        float lastHandAngle = Mathf.Atan2(_lastHandPosition.z, _lastHandPosition.y) * Mathf.Rad2Deg;

        // Get the delta rotation (how much the hand has moved angularly)
        float deltaAngle = Mathf.DeltaAngle(lastHandAngle, handAngle);

        return deltaAngle;
    }

    #endregion

    #region ISaveable Implementation

    public string SaveID => socketID;

    public void SaveState(SaveData saveData)
    {
        // Save socket state using MoveableObjectData
        // Format: currentXRotation|eventFired|isSocketed
        string customData = $"{currentXRotation}|{eventFired}|{isSocketed}";

        MoveableObjectData objectState = new MoveableObjectData(
            socketID,
            wrench.transform.position,
            wrench.transform.rotation,
            wrench.gameObject.activeSelf,
            customData
        );

        saveData.moveableObjects.Add(objectState);

        Debug.Log($"[SocketRotator] Saved state for {socketID}: rotation={currentXRotation:F2}, completed={eventFired}, socketed={isSocketed}");
    }

    public void LoadState(SaveData saveData)
    {
        // Find this socket's saved state
        MoveableObjectData savedState = saveData.moveableObjects.Find(obj => obj.objectID == socketID);

        if (savedState != null && !string.IsNullOrEmpty(savedState.customData))
        {
            // Parse the customData string
            string[] parts = savedState.customData.Split('|');

            if (parts.Length == 3)
            {
                float.TryParse(parts[0], out currentXRotation);
                bool.TryParse(parts[1], out eventFired);
                bool.TryParse(parts[2], out isSocketed);

                Debug.Log($"[SocketRotator] Loaded state for {socketID}: rotation={currentXRotation:F2}, completed={eventFired}, socketed={isSocketed}");

                // Restore the socket state
                RestoreSocketState();
            }
        }
        else
        {
            Debug.Log($"[SocketRotator] No saved state found for {socketID} - using defaults");
        }
    }

    /// <summary>
    /// Restore socket rotation state when loading from save
    /// </summary>
    private void RestoreSocketState()
    {
        if (wrench == null || socketPivot == null)
        {
            Debug.LogWarning("[SocketRotator] Missing wrench or socket pivot reference!");
            return;
        }

        // If wrench was socketed when saved
        if (isSocketed)
        {
            // Position wrench at socket pivot
            wrench.transform.position = socketPivot.position;

            // Apply the saved rotation
            wrench.transform.rotation = Quaternion.Euler(
                socketPivot.rotation.eulerAngles.x + currentXRotation,
                socketPivot.rotation.eulerAngles.y,
                socketPivot.rotation.eulerAngles.z);

            // If puzzle was completed (fully turned)
            if (eventFired)
            {
                // Lock wrench at max rotation
                currentXRotation = maxRotationX;

                wrench.transform.rotation = Quaternion.Euler(
                    socketPivot.rotation.eulerAngles.x + maxRotationX,
                    socketPivot.rotation.eulerAngles.y,
                    socketPivot.rotation.eulerAngles.z);

                // Make wrench kinematic to prevent physics
                Rigidbody rb = wrench.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                // Disable grab interactable so it can't be removed
                wrench.enabled = false;

                Debug.Log($"[SocketRotator] Wrench locked at max rotation from save");
            }

            Debug.Log($"[SocketRotator] Wrench restored to socket at {currentXRotation:F2}° rotation");
        }
    }

    #endregion
}