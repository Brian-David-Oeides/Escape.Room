using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuFacePlayer : MonoBehaviour
{
    public float distance = 2f;

    void Start()
    {
        StartCoroutine(PlaceInFrontOfCamera());
    }

    IEnumerator PlaceInFrontOfCamera()
    {
        // Wait for XR camera to initialize
        yield return new WaitUntil(() => Camera.main != null);

        // Extra frame delay to make sure XR rig is ready
        yield return new WaitForEndOfFrame();

        Transform cam = Camera.main.transform;
        Vector3 forward = cam.forward;
        Vector3 spawnPos = cam.position + forward * distance;

        transform.position = spawnPos;

        Vector3 lookDir = transform.position - cam.position;
        lookDir.y = 0; // keep upright
        transform.rotation = Quaternion.LookRotation(lookDir);
    }
}
