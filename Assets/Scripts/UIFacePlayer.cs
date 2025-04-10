using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFacePlayer : MonoBehaviour
{
    public Transform playerCamera;
    public float distanceFromCamera = 2f;

    void OnEnable()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }

        Vector3 forward = playerCamera.forward;
        Vector3 targetPosition = playerCamera.position + forward * distanceFromCamera;

        transform.position = targetPosition;

        // Face the player
        Vector3 lookDirection = transform.position - playerCamera.position;
        lookDirection.y = 0; // keep UI upright
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }
}

