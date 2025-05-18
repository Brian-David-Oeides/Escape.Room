using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionClueUI : MonoBehaviour
{
    [Tooltip("How far in front of the player the UI should appear")]
    public float distanceFromHead = 0.5f;

    private void OnEnable()
    {
        // Find the camera (player's head)
        Transform head = Camera.main.transform;

        // Position the canvas in front of the head
        transform.position = head.position + head.forward * distanceFromHead;

        // Face the canvas toward the head
        transform.rotation = Quaternion.LookRotation(transform.position - head.position);
    }
}

