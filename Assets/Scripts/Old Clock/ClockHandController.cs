using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockHandController : MonoBehaviour
{
    [Header("Clock Hands")]
    public Transform hourHand;
    public Transform minuteHand;
    public Transform secondHand;

    [Header("Display Settings")]
    public bool showSeconds = true;

    [Header("Hand Offsets")]
    public Vector3 hourHandOffset = new Vector3(0, 0, 90);
    public Vector3 minuteHandOffset = new Vector3(0, 0, 90);
    public Vector3 secondHandOffset = new Vector3(0, 0, -90);

    [Header("References")]
    public TimeManager timeManager;

    void Start()
    {
        // Auto-find TimeManager if not assigned
        if (timeManager == null)
            timeManager = FindObjectOfType<TimeManager>();

        if (timeManager == null)
        {
            Debug.LogError("ClockHandController: No TimeManager found! Please assign one or add a TimeManager to the scene.");
            return;
        }

        // Subscribe to time changes
        timeManager.OnTimeChanged.AddListener(UpdateClockHands);

        // Initial update
        UpdateClockHands(timeManager.CurrentTime);
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (timeManager != null)
            timeManager.OnTimeChanged.RemoveListener(UpdateClockHands);
    }

    void UpdateClockHands(TimeData timeData)
    {
        // Convert time to 12-hour format for display
        int hours = timeData.hour % 12;
        int minutes = timeData.minute;
        float seconds = timeData.second + (timeData.milliseconds / 1000f);

        // Calculate rotations (each hour = 30°, each minute = 6°, each second = 6°)
        float hourAngle = (hours * 30f) + (minutes * 0.5f); // Hour hand moves gradually
        float minuteAngle = minutes * 6f + (seconds * 0.1f); // Minute hand moves gradually  
        float secondAngle = seconds * 6f;

        // Apply rotations with offsets
        if (hourHand != null)
            hourHand.localRotation = Quaternion.Euler(hourAngle, hourHandOffset.y, hourHandOffset.z);

        if (minuteHand != null)
            minuteHand.localRotation = Quaternion.Euler(minuteAngle, minuteHandOffset.y, minuteHandOffset.z);

        if (secondHand != null && showSeconds)
            secondHand.localRotation = Quaternion.Euler(secondAngle, secondHandOffset.y, secondHandOffset.z);
    }

    // Public methods for external control
    public void SetShowSeconds(bool show)
    {
        showSeconds = show;
        if (!show && secondHand != null)
        {
            secondHand.gameObject.SetActive(false);
        }
        else if (show && secondHand != null)
        {
            secondHand.gameObject.SetActive(true);
        }
    }

    public void SetHandOffsets(Vector3 hourOffset, Vector3 minuteOffset, Vector3 secondOffset)
    {
        hourHandOffset = hourOffset;
        minuteHandOffset = minuteOffset;
        secondHandOffset = secondOffset;
    }
}