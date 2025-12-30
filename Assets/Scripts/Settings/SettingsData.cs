using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Difficulty presets for the game
/// </summary>
public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}

/// <summary>
/// Timer mode options
/// </summary>
public enum TimerMode
{
    Disabled,   // No timer
    CountUp,    // Count up from 0 (stopwatch)
    CountDown   // Count down from duration (game over when reaches 0)
}

/// <summary>
/// Data structure for all game settings
/// Serialized to JSON and saved to disk
/// </summary>
[System.Serializable]
public class SettingsData
{
    [Header("Difficulty Settings")]
    public DifficultyLevel difficulty = DifficultyLevel.Normal;

    // Timer settings
    public bool timerEnabled = true;
    public TimerMode timerMode = TimerMode.CountDown;
    public float timerDuration = 600f; // 10 minutes default (in seconds)

    // Difficulty-based values
    public float playerMaxHealth = 100f;
    public float energyDrainRate = 1f; // Energy per second
    public int consumableObjectCount = 10; // Number of candies available
    public float damageMultiplier = 1f; // Global damage multiplier (0.5x Easy, 1.0x Normal, 2.0x Hard)

    [Header("UI Settings")]
    public bool showHealthEnergyText = true;
    public bool healthEnergyHapticsEnabled = true;

    [Header("Audio Settings")]
    public float masterVolume = 1f; // 0 to 1
    public float sfxVolume = 1f;
    public float musicVolume = 1f;

    [Header("Movement Settings")]
    public float movementSpeed = 2f;
    public float snapTurnAngle = 45f;

    [Header("Hint System Settings")]
    public bool hintsEnabled = true;
    public bool showClueProgress = true;
    public int maxManualHints = 5;
    public float hintCooldownSeconds = 60f;

    // Constructor with defaults
    public SettingsData()
    {
        ApplyDifficultyPreset(DifficultyLevel.Normal);
    }

    /// <summary>
    /// Apply a difficulty preset to all difficulty-related settings
    /// </summary>
    public void ApplyDifficultyPreset(DifficultyLevel level)
    {
        difficulty = level;

        switch (level)
        {
            case DifficultyLevel.Easy:
                timerEnabled = true;
                timerMode = TimerMode.CountDown;
                timerDuration = 1800f; // 30 minutes (more forgiving)
                playerMaxHealth = 150f;
                energyDrainRate = 0f; // No energy drain
                consumableObjectCount = 15; // More candies
                damageMultiplier = 0.5f; // Half damage
                // Hint settings for Easy
                maxManualHints = 5;
                hintCooldownSeconds = 60f;
                break;

            case DifficultyLevel.Normal:
                timerEnabled = true;
                timerMode = TimerMode.CountDown;
                timerDuration = 900f; // 15 minutes
                playerMaxHealth = 100f;
                energyDrainRate = 1f; // Normal drain
                consumableObjectCount = 10; // Normal amount
                damageMultiplier = 1f; // Normal damage
                // Hint settings for Normal
                maxManualHints = 3;
                hintCooldownSeconds = 120f;
                break;

            case DifficultyLevel.Hard:
                timerEnabled = true;
                timerMode = TimerMode.CountDown;
                timerDuration = 600f; // 10 minutes (challenging)
                playerMaxHealth = 75f;
                energyDrainRate = 2f; // Faster drain
                consumableObjectCount = 5; // Fewer candies
                damageMultiplier = 2f; // Double damage
                // Hint settings for Hard
                maxManualHints = 1;
                hintCooldownSeconds = 180f;
                break;
        }

        // Debug.Log($"Applied {level} difficulty preset");
    }

    /// <summary>
    /// Validate settings are within acceptable ranges
    /// </summary>
    public void ValidateSettings()
    {
        // Clamp volumes between 0 and 1
        masterVolume = Mathf.Clamp01(masterVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        musicVolume = Mathf.Clamp01(musicVolume);

        // Clamp movement settings
        movementSpeed = Mathf.Clamp(movementSpeed, 0.5f, 5f);
        snapTurnAngle = Mathf.Clamp(snapTurnAngle, 15f, 90f);

        // Clamp difficulty values
        timerDuration = Mathf.Max(60f, timerDuration); // Minimum 1 minute
        playerMaxHealth = Mathf.Clamp(playerMaxHealth, 25f, 200f);
        energyDrainRate = Mathf.Clamp(energyDrainRate, 0f, 5f);
        consumableObjectCount = Mathf.Clamp(consumableObjectCount, 1, 20);
        damageMultiplier = Mathf.Clamp(damageMultiplier, 0.1f, 5f); // 0.1x to 5x

        // Validate hint settings
        maxManualHints = Mathf.Clamp(maxManualHints, 0, 10);
        hintCooldownSeconds = Mathf.Max(0f, hintCooldownSeconds);
    }

    /// <summary>
    /// Get timer duration in minutes (for UI display)
    /// </summary>
    public int GetTimerDurationInMinutes()
    {
        return Mathf.RoundToInt(timerDuration / 60f);
    }

    /// <summary>
    /// Set timer duration from minutes (for UI input)
    /// </summary>
    public void SetTimerDurationInMinutes(int minutes)
    {
        timerDuration = Mathf.Max(1, minutes) * 60f;
    }
}