using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AlarmSystem : MonoBehaviour
{
    [Header("Alarm Settings")]
    public bool alarmEnabled = false;
    public int alarmHour = 12;
    public int alarmMinute = 0;
    public float alarmDuration = 5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip alarmSound;
    public float alarmVolume = 1f;

    [Header("Events")]
    public UnityEvent OnAlarmTriggered;
    public UnityEvent OnAlarmStarted;
    public UnityEvent OnAlarmStopped;

    [Header("References")]
    public TimeManager timeManager;

    // Private variables
    private bool alarmTriggered = false;
    private Coroutine alarmCoroutine;

    void Start()
    {
        // Auto-find TimeManager if not assigned
        if (timeManager == null)
            timeManager = FindObjectOfType<TimeManager>();

        if (timeManager == null)
        {
            Debug.LogError("AlarmSystem: No TimeManager found! Please assign one or add a TimeManager to the scene.");
            return;
        }

        // Subscribe to minute changes to check alarm
        timeManager.OnMinuteChanged.AddListener(CheckAlarm);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (timeManager != null)
            timeManager.OnMinuteChanged.RemoveListener(CheckAlarm);

        // Stop any running alarm coroutine
        if (alarmCoroutine != null)
            StopCoroutine(alarmCoroutine);
    }

    void CheckAlarm()
    {
        if (!alarmEnabled || alarmTriggered || timeManager == null)
            return;

        TimeData currentTime = timeManager.CurrentTime;

        if (currentTime.hour == alarmHour && currentTime.minute == alarmMinute)
        {
            TriggerAlarm();
        }
    }

    public void TriggerAlarm()
    {
        if (alarmTriggered) return;

        alarmTriggered = true;

        // Invoke events
        OnAlarmTriggered?.Invoke();
        OnAlarmStarted?.Invoke();

        // Play alarm sound if available
        if (alarmSound != null && audioSource != null)
        {
            alarmCoroutine = StartCoroutine(PlayAlarmCoroutine());
        }

        // Reset alarm trigger after a delay
        StartCoroutine(ResetAlarmTrigger());
    }

    IEnumerator PlayAlarmCoroutine()
    {
        float endTime = Time.time + alarmDuration;

        while (Time.time < endTime && alarmTriggered)
        {
            audioSource.PlayOneShot(alarmSound, alarmVolume);
            yield return new WaitForSeconds(alarmSound.length);
        }

        OnAlarmStopped?.Invoke();
    }

    IEnumerator ResetAlarmTrigger()
    {
        yield return new WaitForSeconds(60f); // Wait 1 minute before allowing alarm to trigger again
        alarmTriggered = false;
    }

    // Public methods for external control
    public void SetAlarm(int hour, int minute, bool enabled = true)
    {
        alarmHour = hour;
        alarmMinute = minute;
        alarmEnabled = enabled;
        alarmTriggered = false;

        Debug.Log($"Alarm set for {hour:D2}:{minute:D2}, Enabled: {enabled}");
    }

    public void EnableAlarm(bool enable)
    {
        alarmEnabled = enable;
        if (!enable)
        {
            StopAlarm();
        }
    }

    public void StopAlarm()
    {
        if (alarmCoroutine != null)
        {
            StopCoroutine(alarmCoroutine);
            alarmCoroutine = null;
        }

        alarmTriggered = false;
        OnAlarmStopped?.Invoke();
    }

    public void SetAlarmVolume(float volume)
    {
        alarmVolume = Mathf.Clamp01(volume);
    }

    public void SetAlarmDuration(float duration)
    {
        alarmDuration = Mathf.Max(0.1f, duration);
    }

    public void SetAlarmSound(AudioClip newAlarmSound)
    {
        alarmSound = newAlarmSound;
    }

    // Getters
    public bool IsAlarmEnabled() => alarmEnabled;
    public bool IsAlarmTriggered() => alarmTriggered;
    public string GetAlarmTimeString() => $"{alarmHour:D2}:{alarmMinute:D2}";
}