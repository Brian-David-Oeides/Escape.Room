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

public class SocketRotator : MonoBehaviour
{
    [Header("References")]
    public XRGrabInteractable wrench;
    public Transform socketPivot;
    public Collider socketTrigger;

    [Header("Rotation Settings")]
    public float maxRotationX = 90f;  // Changed to X
    public float rotationSpeed = 75f;

    [Header("Events")]
    public UnityEvent onFullyTurned;

    // Control variables
    private XRBaseInteractor interactor;
    private float currentXRotation = 0f;  // Changed to X
    private bool isGrabbed = false;
    private bool isSocketed = false;
    private bool eventFired = false;

    // Tracking variables
    private float _grabStartRotation = 0f;
    private float _wrenchStartRotation = 0f;
    private Vector3 _lastHandPosition = Vector3.zero;

    private void Start()
    {
        // If you have an XRSocketInteractor component, ensure it knows about our custom locking
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

            // IMPORTANT: If we already reached max rotation, ensure the wrench stays locked
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

    // Add this to enforce locking from outside scripts
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

    // Add this to run in Update() to keep checking if it needs fixing
    private void LateUpdate()
    {
        if (eventFired && isSocketed)
        {
            // Force lock position even after other scripts might have changed it
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
            eventFired = true;

            // Set to exact max rotation when event is fired
            currentXRotation = maxRotationX;
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
}