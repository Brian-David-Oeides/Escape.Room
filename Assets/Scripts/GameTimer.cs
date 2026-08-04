using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimer : MonoSingleton<GameTimer>
{
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

    protected override void Awake()
    {
        base.Awake();  // MonoSingleton sets up Instance
        DontDestroyOnLoad(gameObject);
    }

    public override void Init()
    {
        InitializeTimer();
        GameLog.Log("[GameTimer] Initialized in Awake phase");
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

            GameLog.Log("⏰ TIMER EXPIRED! Game Over!");

            // Debug: Check if event has subscribers
            if (OnTimerExpired != null)
            {
                GameLog.Log($"[GameTimer] OnTimerExpired has subscribers. Invoking event now...");
                OnTimerExpired();
                GameLog.Log($"[GameTimer] OnTimerExpired event invoked successfully");
            }
            else
            {
                GameLog.LogWarning("[GameTimer] OnTimerExpired event has NO subscribers! Event not invoked.");
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

        GameLog.Log($"[GameTimer] InitializeTimer() called - Mode: {_timerMode}, Duration: {_countdownDuration}s");
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
            GameLog.LogWarning("Cannot resume timer when mode is Disabled");
            return;
        }

        if (_timerExpired)
        {
            GameLog.LogWarning("Cannot resume timer - timer has expired");
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
        GameLog.Log($"[GameTimer] SetTimerMode() called - changing from {_timerMode} to {mode}");
        _timerMode = mode;
        InitializeTimer();
    }

    /// <summary>
    /// Restore timer state from save data
    /// </summary>
    public void RestoreTimerState(float remainingTime, float duration, bool wasRunning)
    {
        GameLog.Log($"[GameTimer] RestoreTimerState called - Remaining: {remainingTime}s, Duration: {duration}s, Running: {wasRunning}");

        _remainingTime = remainingTime;
        _countdownDuration = duration > 0 ? duration : _countdownDuration; // Use saved duration if valid
        _elapsedTime = 0f; // Reset elapsed time on load
        _timerExpired = remainingTime <= 0;
        _isRunning = wasRunning && !_timerExpired; // Don't resume if expired

        GameLog.Log($"[GameTimer] Timer state restored - Mode: {_timerMode}, Remaining: {GetFormattedRemainingTime()}");
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
            GameLog.Log($"⏰ Added {seconds} seconds. Remaining: {GetFormattedRemainingTime()}");
        }
    }

    public void DebugSetRemainingTime(float seconds)
    {
        if (_timerMode == TimerMode.CountDown)
        {
            _remainingTime = Mathf.Clamp(seconds, 0f, _countdownDuration);
            _timerExpired = false;
            GameLog.Log($"⏰ Set remaining time to: {GetFormattedRemainingTime()}");
        }
    }

    #region Save/Load System

    /// <summary>
    /// Save timer state to SaveData
    /// Called by SaveManager during save operation
    /// </summary>
    public void SaveState(SaveData data)
    {
        data.timerElapsedTime = _elapsedTime;
        data.timerRemainingTime = _remainingTime;
        data.timerIsRunning = _isRunning;
        data.timerExpired = _timerExpired;
        data.timerMode = (int)_timerMode;
        data.timerCountdownDuration = _countdownDuration;

        GameLog.Log($"[GameTimer] SaveState() called - Mode: {_timerMode}, Remaining: {GetFormattedRemainingTime()}, Expired: {_timerExpired}");
    }

    /// <summary>
    /// Load timer state from SaveData
    /// Called by GameManager after scene load
    /// Includes validation to prevent save file tampering
    /// </summary>
    public void LoadState(SaveData data)
    {
        // Validate and restore timer mode
        _timerMode = (TimerMode)Mathf.Clamp(data.timerMode, 0, 2);

        // Validate and restore countdown duration
        _countdownDuration = Mathf.Max(0f, data.timerCountdownDuration);

        // Validate and clamp remaining time (prevent tampering)
        _remainingTime = Mathf.Clamp(data.timerRemainingTime, 0f, _countdownDuration);

        // Validate and restore elapsed time
        _elapsedTime = Mathf.Max(0f, data.timerElapsedTime);

        // Restore expired state
        _timerExpired = data.timerExpired;

        // Restore running state (but don't run if expired)
        _isRunning = data.timerIsRunning && !_timerExpired;

        GameLog.Log($"[GameTimer] LoadState() called - Mode: {_timerMode}, Remaining: {GetFormattedRemainingTime()}, Running: {_isRunning}, Expired: {_timerExpired}");
    }

    #endregion
}