using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    public bool useSystemTime = true;
    public float timeSpeed = 1f;

    [Header("Custom Time (when not using system time)")]
    public int customHour = 12;
    public int customMinute = 0;
    public int customSecond = 0;

    [Header("Events")]
    public UnityEvent<TimeData> OnTimeChanged;
    public UnityEvent OnSecondChanged;
    public UnityEvent OnMinuteChanged;
    public UnityEvent OnHourChanged;
    public UnityEvent<int> OnSecondChanged_Int;
    public UnityEvent<int> OnMinuteChanged_Int;
    public UnityEvent<int> OnHourChanged_Int;

    // Private variables
    private float currentTime;
    private TimeData lastTimeData;
    private bool initialized = false;

    // Public properties
    public TimeData CurrentTime => new TimeData(currentTime);
    public float CurrentTimeInSeconds => currentTime;

    void Start()
    {
        InitializeTime();
        initialized = true;
    }

    void Update()
    {
        UpdateTime();
        CheckForTimeChanges();
    }

    void InitializeTime()
    {
        if (useSystemTime)
        {
            DateTime now = DateTime.Now;
            currentTime = now.Hour * 3600f + now.Minute * 60f + now.Second + now.Millisecond / 1000f;
        }
        else
        {
            currentTime = customHour * 3600f + customMinute * 60f + customSecond;
        }

        lastTimeData = new TimeData(currentTime);
    }

    void UpdateTime()
    {
        if (useSystemTime)
        {
            DateTime now = DateTime.Now;
            currentTime = now.Hour * 3600f + now.Minute * 60f + now.Second + now.Millisecond / 1000f;
        }
        else
        {
            currentTime += Time.deltaTime * timeSpeed;

            // Wrap around 24 hours
            if (currentTime >= 86400f) // 24 * 60 * 60
            {
                currentTime -= 86400f;
            }
        }
    }

    void CheckForTimeChanges()
    {
        if (!initialized) return;

        TimeData currentTimeData = new TimeData(currentTime);

        // Always notify of time change
        OnTimeChanged?.Invoke(currentTimeData);

        // Check for second change
        if (currentTimeData.second != lastTimeData.second)
        {
            OnSecondChanged?.Invoke();
            OnSecondChanged_Int?.Invoke(currentTimeData.second);
        }

        // Check for minute change
        if (currentTimeData.minute != lastTimeData.minute)
        {
            OnMinuteChanged?.Invoke();
            OnMinuteChanged_Int?.Invoke(currentTimeData.minute);
        }

        // Check for hour change
        if (currentTimeData.hour != lastTimeData.hour)
        {
            OnHourChanged?.Invoke();
            OnHourChanged_Int?.Invoke(currentTimeData.hour);
        }

        lastTimeData = currentTimeData;
    }

    // Public methods for external control
    public void SetTime(int hour, int minute, int second = 0)
    {
        currentTime = hour * 3600f + minute * 60f + second;
        useSystemTime = false;
        lastTimeData = new TimeData(currentTime);
    }

    public void SetTimeSpeed(float speed)
    {
        timeSpeed = speed;
    }

    public void SetUseSystemTime(bool useSystem)
    {
        useSystemTime = useSystem;
        if (useSystem)
        {
            InitializeTime();
        }
    }

    // Utility methods
    public bool IsTimeEqual(int hour, int minute, int second = -1)
    {
        TimeData current = new TimeData(currentTime);
        if (second == -1)
            return current.hour == hour && current.minute == minute;
        else
            return current.hour == hour && current.minute == minute && current.second == second;
    }

    public string GetTimeString(bool use24Hour = false)
    {
        return new TimeData(currentTime).ToString(use24Hour);
    }
}
