using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ClockController : MonoBehaviour
{
    [Header("Clock Hands")]
    public Transform hourHand;
    public Transform minuteHand;
    public Transform secondHand;

    [Header("Clock Settings")]
    public bool useSystemTime = true;
    public bool showSeconds = true;
    public float timeSpeed = 1f; // Multiplier for clock speed

    [Header("Custom Time (when not using system time)")]
    public int customHour = 12;
    public int customMinute = 0;
    public int customSecond = 0;

    [Header("Audio")]
    public AudioSource tickingAudioSource;
    public AudioClip tickSound;
    public bool enableTicking = true;
    public float tickVolume = 0.5f;

    [Header("Audio Source Settings")]
    public bool playOnAwake = false;
    public bool loop = false;
    public int priority = 128;
    public float pitch = 1f;
    public float stereoPan = 0f;
    [Range(0f, 1f)]
    public float spatialBlend = 1f; // 0 = 2D, 1 = 3D
    public float dopplerLevel = 1f;
    public float spread = 0f;
    public AudioRolloffMode volumeRolloff = AudioRolloffMode.Logarithmic;
    public float minDistance = 1f;
    public float maxDistance = 500f;

    [Header("Alarm")]
    public bool alarmEnabled = false;
    public int alarmHour = 12;
    public int alarmMinute = 0;
    public AudioClip alarmSound;
    public float alarmVolume = 1f;
    public float alarmDuration = 5f;

    [Header("Unity Events")]
    public UnityEvent OnClockTick;
    public UnityEvent OnMinuteChange;
    public UnityEvent OnHourChange;
    public UnityEvent OnAlarmTriggered;
    public UnityEvent<int> OnHourChanged; // Passes hour value
    public UnityEvent<int> OnMinuteChanged; // Passes minute value

    // Private variables
    private float currentTime;
    private int lastSecond = -1;
    private int lastMinute = -1;
    private int lastHour = -1;
    private bool alarmTriggered = false;
    private Coroutine alarmCoroutine;

    // Hand rotation offsets (in case your hands don't start at 12 o'clock)
    private Vector3 hourHandOffset = Vector3.zero;
    private Vector3 minuteHandOffset = Vector3.zero;
    private Vector3 secondHandOffset = Vector3.zero;

    void Start()
    {
        // Setup audio source if not assigned
        if (tickingAudioSource == null && enableTicking)
        {
            tickingAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (tickingAudioSource != null)
        {
            // Apply all serialized audio settings
            tickingAudioSource.clip = tickSound;
            tickingAudioSource.volume = tickVolume;
            tickingAudioSource.playOnAwake = playOnAwake;
            tickingAudioSource.loop = loop;
            tickingAudioSource.priority = priority;
            tickingAudioSource.pitch = pitch;
            tickingAudioSource.panStereo = stereoPan;
            tickingAudioSource.spatialBlend = spatialBlend;
            tickingAudioSource.dopplerLevel = dopplerLevel;
            tickingAudioSource.spread = spread;
            tickingAudioSource.rolloffMode = volumeRolloff;
            tickingAudioSource.minDistance = minDistance;
            tickingAudioSource.maxDistance = maxDistance;
        }

        // Initialize time
        if (useSystemTime)
        {
            DateTime now = DateTime.Now;
            currentTime = now.Hour * 3600f + now.Minute * 60f + now.Second + now.Millisecond / 1000f;
        }
        else
        {
            currentTime = customHour * 3600f + customMinute * 60f + customSecond;
        }

        UpdateClockHands();
    }

    void Update()
    {
        if (useSystemTime)
        {
            // Use system time
            DateTime now = DateTime.Now;
            currentTime = now.Hour * 3600f + now.Minute * 60f + now.Second + now.Millisecond / 1000f;
        }
        else
        {
            // Use custom time with speed multiplier
            currentTime += Time.deltaTime * timeSpeed;

            // Wrap around 24 hours
            if (currentTime >= 86400f) // 24 * 60 * 60
            {
                currentTime -= 86400f;
            }
        }

        UpdateClockHands();
        CheckForTimeEvents();
        CheckAlarm();
    }

    void UpdateClockHands()
    {
        // Convert current time to hours, minutes, seconds
        float totalSeconds = currentTime;
        int hours = Mathf.FloorToInt(totalSeconds / 3600f) % 12; // 12-hour format
        int minutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
        float seconds = totalSeconds % 60f;

        // Calculate rotations (Unity rotates clockwise, clocks go clockwise)
        // Each hour = 30 degrees, each minute = 6 degrees, each second = 6 degrees
        float hourAngle = (hours * 30f) + (minutes * 0.5f); // Hour hand moves gradually
        float minuteAngle = minutes * 6f + (seconds * 0.1f); // Minute hand moves gradually
        float secondAngle = seconds * 6f;

        // Apply rotations (assuming hands rotate around Z-axis)
        if (hourHand != null)
            hourHand.localRotation = Quaternion.Euler(hourHandOffset.x, hourHandOffset.y, hourHandOffset.z - hourAngle);

        if (minuteHand != null)
            minuteHand.localRotation = Quaternion.Euler(minuteHandOffset.x, minuteHandOffset.y, minuteHandOffset.z - minuteAngle);

        if (secondHand != null && showSeconds)
            secondHand.localRotation = Quaternion.Euler(secondHandOffset.x, secondHandOffset.y, secondHandOffset.z - secondAngle);
    }

    void CheckForTimeEvents()
    {
        int currentSecond = Mathf.FloorToInt(currentTime % 60f);
        int currentMinute = Mathf.FloorToInt((currentTime % 3600f) / 60f);
        int currentHour = Mathf.FloorToInt(currentTime / 3600f) % 24;

        // Check for second change (tick)
        if (currentSecond != lastSecond)
        {
            lastSecond = currentSecond;
            OnClockTick?.Invoke();

            // Play tick sound
            if (enableTicking && tickingAudioSource != null && tickSound != null)
            {
                tickingAudioSource.PlayOneShot(tickSound, tickVolume);
            }
        }

        // Check for minute change
        if (currentMinute != lastMinute)
        {
            lastMinute = currentMinute;
            OnMinuteChange?.Invoke();
            OnMinuteChanged?.Invoke(currentMinute);
        }

        // Check for hour change
        if (currentHour != lastHour)
        {
            lastHour = currentHour;
            OnHourChange?.Invoke();
            OnHourChanged?.Invoke(currentHour);
        }
    }

    void CheckAlarm()
    {
        if (!alarmEnabled || alarmTriggered) return;

        int currentHour = Mathf.FloorToInt(currentTime / 3600f) % 24;
        int currentMinute = Mathf.FloorToInt((currentTime % 3600f) / 60f);

        if (currentHour == alarmHour && currentMinute == alarmMinute)
        {
            TriggerAlarm();
        }
    }

    public void TriggerAlarm()
    {
        if (alarmTriggered) return;

        alarmTriggered = true;
        OnAlarmTriggered?.Invoke();

        if (alarmSound != null && tickingAudioSource != null)
        {
            alarmCoroutine = StartCoroutine(PlayAlarmCoroutine());
        }

        // Reset alarm trigger after a delay to allow it to trigger again the next day
        StartCoroutine(ResetAlarmTrigger());
    }

    IEnumerator PlayAlarmCoroutine()
    {
        float endTime = Time.time + alarmDuration;

        while (Time.time < endTime)
        {
            tickingAudioSource.PlayOneShot(alarmSound, alarmVolume);
            yield return new WaitForSeconds(alarmSound.length);
        }
    }

    IEnumerator ResetAlarmTrigger()
    {
        yield return new WaitForSeconds(60f); // Wait 1 minute before allowing alarm to trigger again
        alarmTriggered = false;
    }

    // Public methods for external control
    public void SetTime(int hour, int minute, int second = 0)
    {
        currentTime = hour * 3600f + minute * 60f + second;
        useSystemTime = false;
    }

    public void SetAlarm(int hour, int minute, bool enabled = true)
    {
        alarmHour = hour;
        alarmMinute = minute;
        alarmEnabled = enabled;
        alarmTriggered = false;
    }

    public void EnableAlarm(bool enable)
    {
        alarmEnabled = enable;
        if (!enable && alarmCoroutine != null)
        {
            StopCoroutine(alarmCoroutine);
        }
    }

    public void StopAlarm()
    {
        if (alarmCoroutine != null)
        {
            StopCoroutine(alarmCoroutine);
        }
        alarmTriggered = false;
    }

    public void SetTimeSpeed(float speed)
    {
        timeSpeed = speed;
    }

    public void ToggleTicking()
    {
        enableTicking = !enableTicking;
    }

    // Get current time values
    public int GetCurrentHour()
    {
        return Mathf.FloorToInt(currentTime / 3600f) % 24;
    }

    public int GetCurrentMinute()
    {
        return Mathf.FloorToInt((currentTime % 3600f) / 60f);
    }

    public int GetCurrentSecond()
    {
        return Mathf.FloorToInt(currentTime % 60f);
    }

    public string GetCurrentTimeString(bool use24Hour = false)
    {
        int hour = GetCurrentHour();
        int minute = GetCurrentMinute();
        int second = GetCurrentSecond();

        if (!use24Hour)
        {
            string ampm = hour >= 12 ? "PM" : "AM";
            hour = hour % 12;
            if (hour == 0) hour = 12;
            return $"{hour:D2}:{minute:D2}:{second:D2} {ampm}";
        }

        return $"{hour:D2}:{minute:D2}:{second:D2}";
    }
}