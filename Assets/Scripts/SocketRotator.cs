using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketRotator : MonoBehaviour
{
    [Header("Socketed Object Settings")]
    [Tooltip("The XRGrabInteractable object to monitor for rotation input")]
    public XRGrabInteractable socketedObject;

    [Tooltip("The point around which the object should rotate (e.g., the socket base)")]
    public Transform rotationPivot;

    [Tooltip("Maximum Y rotation in degrees allowed")]
    public float maxYRotation = 45f;

    [Tooltip("Speed at which the object rotates based on input")]
    public float rotationSpeed = 50f;

    [Header("Events")]
    [Tooltip("UnityEvent triggered when max rotation is reached")]
    public UnityEvent onMaxRotationReached;

    private float _currentYRotation = 0f;
    private bool _isGrabbed = false;
    private bool _eventFired = false;
    private XRBaseInteractor _interactor;

    private void OnEnable()
    {
        if (socketedObject != null)
        {
            socketedObject.selectEntered.AddListener(OnGrab);
            socketedObject.selectExited.AddListener(OnRelease);
        }
    }

    private void OnDisable()
    {
        if (socketedObject != null)
        {
            socketedObject.selectEntered.RemoveListener(OnGrab);
            socketedObject.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        _interactor = args.interactorObject as XRBaseInteractor;
        _isGrabbed = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        _interactor = null;
    }

    private void Update()
    {
        if (!_isGrabbed || _interactor == null || rotationPivot == null)
            return;

        // Use lateral controller movement to determine rotation amount
        Vector3 handDirection = _interactor.transform.position - rotationPivot.position;
        float input = handDirection.x;

        float deltaRotation = input * rotationSpeed * Time.deltaTime;
        _currentYRotation += deltaRotation;
        _currentYRotation = Mathf.Clamp(_currentYRotation, 0f, maxYRotation);

        // Apply rotation
        socketedObject.transform.position = rotationPivot.position;
        socketedObject.transform.rotation = Quaternion.Euler(0f, _currentYRotation, 0f) * rotationPivot.rotation;

        // Trigger event once
        if (_currentYRotation >= maxYRotation && !_eventFired)
        {
            onMaxRotationReached.Invoke();
            _eventFired = true;
        }
    }
}

