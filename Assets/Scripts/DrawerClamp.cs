using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class DrawerClamp : MonoBehaviour
{
    [Tooltip("Minimum Z distance (fully closed) relative to start")]
    public float minLocalZ = 0f;

    [Tooltip("Maximum Z distance (fully open) relative to start")]
    public float maxLocalZ = 0.25f;

    private Rigidbody rb;
    private Vector3 initialLocalPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialLocalPosition = transform.localPosition;
    }

    void FixedUpdate()
    {
        Vector3 localPos = transform.localPosition;
        float localZ = localPos.z - initialLocalPosition.z;
        float clampedZ = Mathf.Clamp(localZ, minLocalZ, maxLocalZ);
        transform.localPosition = new Vector3(localPos.x, localPos.y, initialLocalPosition.z + clampedZ);
    }
}

