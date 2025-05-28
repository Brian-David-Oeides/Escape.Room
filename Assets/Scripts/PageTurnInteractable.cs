using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class PageTurnInteractable : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationAngle = 120f;
    public float rotationSpeed = 250f;
    private bool isFlipped = false;
    private Quaternion targetRotation;

    [Header("Interaction Event")]
    public UnityEvent onPageFlipped;

    private void Start()
    {
        targetRotation = transform.localRotation;
    }

    public void TriggerPageFlip()
    {
        isFlipped = !isFlipped;
        float direction = isFlipped ? rotationAngle : 0f;
        targetRotation = Quaternion.Euler(0, 0, direction);
        onPageFlipped?.Invoke();
    }

    private void Update()
    {
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}

