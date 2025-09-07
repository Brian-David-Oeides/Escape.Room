using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngledBasedGlimmer : MonoBehaviour
{
    [Header("Glimmer Settings")]
    public ParticleSystem glimmerParticles;
    public Transform watchSurface;

    [Tooltip("Minimum angle from direct view before glimmer can trigger. 0-15° = looking directly at watch (no glimmer zone)")]
    public float minTriggerAngle = 30f; // No glimmer below this angle

    [Tooltip("Minimum head rotation required to check for glimmer trigger. Higher = less sensitive to small movements")]
    public float rotationThreshold = 10f; // Minimum rotation change to check

    [Tooltip("Time delay between possible glimmer triggers. Higher = less frequent glimmers")]
    public float cooldownTime = 1f; // Prevent multiple triggers too quickly

    private Camera playerCamera;
    private float lastWatchAngle;
    private float lastGlimmerTime;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        if (watchSurface == null)
            watchSurface = transform.parent;

        if (glimmerParticles == null)
            glimmerParticles = GetComponent<ParticleSystem>();

        // Initialize last angle
        lastWatchAngle = GetCurrentWatchAngle();
    }

    void Update()
    {
        if (playerCamera == null || glimmerParticles == null || watchSurface == null)
            return;

        // Check cooldown
        if (Time.time - lastGlimmerTime < cooldownTime)
            return;

        // Calculate current angle between head direction and watch direction
        float currentWatchAngle = GetCurrentWatchAngle();

        // Check if head rotated significantly relative to watch
        float angleDelta = Mathf.Abs(currentWatchAngle - lastWatchAngle);

        if (angleDelta >= rotationThreshold)
        {
            // Only trigger if current angle is above minimum threshold
            if (currentWatchAngle >= minTriggerAngle)
            {
                Debug.Log($"Head rotation relative to watch: {currentWatchAngle:F1} degrees - Glimmer triggered!");
                glimmerParticles.Emit(1);
                lastGlimmerTime = Time.time;
            }
            else
            {
                Debug.Log($"Head rotation detected but angle too small: {currentWatchAngle:F1} degrees (min: {minTriggerAngle})");
            }

            lastWatchAngle = currentWatchAngle;
        }
    }

    float GetCurrentWatchAngle()
    {
        // Direction from camera to watch
        Vector3 cameraToWatch = (watchSurface.position - playerCamera.transform.position).normalized;

        // Camera's forward direction (where head is looking)
        Vector3 headDirection = playerCamera.transform.forward;

        // Angle between head direction and watch direction
        return Vector3.Angle(headDirection, cameraToWatch);
    }
}