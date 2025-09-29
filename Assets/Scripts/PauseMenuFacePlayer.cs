using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuFacePlayer : MonoBehaviour
{
    public float distance = 1.5f;

    // Position the menu every time it's enabled
    private void OnEnable()
    {
        PositionInFrontOfPlayer();
    }

    private void PositionInFrontOfPlayer()
    {
        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            Vector3 forward = cam.forward;
            Vector3 spawnPos = cam.position + forward * distance;

            transform.position = spawnPos;

            Vector3 lookDir = transform.position - cam.position;
            lookDir.y = 0; // Keep upright
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}