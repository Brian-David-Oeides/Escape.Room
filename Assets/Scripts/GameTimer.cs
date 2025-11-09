using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    // Timer Mode Enum
    public enum TimerMode
    {
        Disabled,      // No timer active
        CountUp,       // Count elapsed time (existing behavior)
        CountDown      // Count down from duration to zero
    }

    [Header("Timer Configuration")]
    [SerializeField] private TimerMode _timerMode = TimerMode.CountUp;
    [SerializeField] private float _countdownDuration = 1800f; // Default: 30 minutes (in seconds)

    [Header("Timer State")]
    private float _elapsedTime = 0f;
    private float _remainingTime = 0f;
    private bool _isRunning = true;
    private bool _timerExpired = false;

    // Events
    public event Action OnTimerExpired;  // Triggered when countdown reaches zero
    public event Action<float> OnTimerTick; // Triggered every second (optional for UI updates)

    // Properties
    public TimerMode Mode => _timerMode;
    public float ElapsedTime => _elapsedTime;
    public float RemainingTime => _remainingTime;
    public bool IsRunning => _isRunning;
    public bool TimerExpired => _timerExpired;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeTimer();
    }

    private void Update()
    {
        if (!_isRunning || _timerMode == TimerMode.Disabled)
            return;

        switch (_timerMode)
        {
            case TimerMode.CountUp:
                UpdateCountUpTimer();
                break;

            case TimerMode.CountDown:
                UpdateCountDownTimer();
                break;
        }
    }

    private void UpdateCountUpTimer()
    {
        _elapsedTime += Time.deltaTime;
    }

    private void UpdateCountDownTimer()
    {
        if (_timerExpired)
            return;

        _remainingTime -= Time.deltaTime;
        _elapsedTime += Time.deltaTime; // Track total elapsed time even in countdown mode

        // Check if timer expired
        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            _timerExpired = true;
            _isRunning = false;

            Debug.Log("⏰ TIMER EXPIRED! Game Over!");

            // Debug: Check if event has subscribers
            if (OnTimerExpired != null)
            {
                Debug.Log($"[GameTimer] OnTimerExpired has subscribers. Invoking event now...");
                OnTimerExpired();
                Debug.Log($"[GameTimer] OnTimerExpired event invoked successfully");
            }
            else
            {
                Debug.LogWarning("[GameTimer] OnTimerExpired event has NO subscribers! Event not invoked.");
            }
        }
    }

    private void InitializeTimer()
    {
        _elapsedTime = 0f;
        _timerExpired = false;

        if (_timerMode == TimerMode.CountDown)
        {
            _remainingTime = _countdownDuration;
        }

        Debug.Log($"[GameTimer] InitializeTimer() called - Mode: {_timerMode}, Duration: {_countdownDuration}s");
    }

    // Public Control Methods
    public void StopTimer()
    {
        _isRunning = false;
    }

    public void PauseTimer()
    {
        _isRunning = false;
    }

    public void ResumeTimer()
    {
        if (_timerMode == TimerMode.Disabled)
        {
            Debug.LogWarning("Cannot resume timer when mode is Disabled");
            return;
        }

        if (_timerExpired)
        {
            Debug.LogWarning("Cannot resume timer - timer has expired");
            return;
        }

        _isRunning = true;
    }

    public void ResetTimer()
    {
        InitializeTimer();
        _isRunning = true;
    }

    // Configuration Methods (for Settings System integration)
    public void SetTimerMode(TimerMode mode)
    {
        Debug.Log($"[GameTimer] SetTimerMode() called - changing from {_timerMode} to {mode}");
        _timerMode = mode;
        InitializeTimer();
    }

    public void SetCountdownDuration(float durationInSeconds)
    {
        _countdownDuration = durationInSeconds;

        if (_timerMode == TimerMode.CountDown)
        {
            _remainingTime = durationInSeconds;
        }
    }

    public void SetCountdownDuration(int durationInMinutes)
    {
        SetCountdownDuration(durationInMinutes * 60f);
    }

    // Formatting Methods
    public string GetFormattedTime()
    {
        float timeToFormat = (_timerMode == TimerMode.CountDown) ? _remainingTime : _elapsedTime;
        return FormatTime(timeToFormat);
    }

    public string GetFormattedElapsedTime()
    {
        return FormatTime(_elapsedTime);
    }

    public string GetFormattedRemainingTime()
    {
        return FormatTime(_remainingTime);
    }

    private string FormatTime(float timeInSeconds)
    {
        // Prevent negative time display
        timeInSeconds = Mathf.Max(0f, timeInSeconds);

        int hours = Mathf.FloorToInt(timeInSeconds / 3600f);
        int minutes = Mathf.FloorToInt((timeInSeconds % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);

        // If countdown mode and less than 1 hour, hide hours for cleaner display
        if (_timerMode == TimerMode.CountDown && hours == 0)
        {
            return $"{minutes:00}:{seconds:00}";
        }

        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    // Utility Methods
    public float GetTimePercentage()
    {
        if (_timerMode != TimerMode.CountDown || _countdownDuration <= 0)
            return 0f;

        return Mathf.Clamp01(_remainingTime / _countdownDuration);
    }

    public bool IsTimeCritical(float warningThresholdInSeconds = 300f) // Default: 5 minutes
    {
        if (_timerMode != TimerMode.CountDown)
            return false;

        return _remainingTime <= warningThresholdInSeconds && _remainingTime > 0f;
    }

    // Debug Methods
    public void DebugAddTime(float seconds)
    {
        if (_timerMode == TimerMode.CountDown)
        {
            _remainingTime += seconds;
            _remainingTime = Mathf.Min(_remainingTime, _countdownDuration);
            Debug.Log($"⏰ Added {seconds} seconds. Remaining: {GetFormattedRemainingTime()}");
        }
    }

    public void DebugSetRemainingTime(float seconds)
    {
        if (_timerMode == TimerMode.CountDown)
        {
            _remainingTime = Mathf.Clamp(seconds, 0f, _countdownDuration);
            _timerExpired = false;
            Debug.Log($"⏰ Set remaining time to: {GetFormattedRemainingTime()}");
        }
    }
}