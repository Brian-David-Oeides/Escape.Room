using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages all game settings
/// Handles loading, saving, and applying settings to game systems
/// </summary>
///

public class SettingsManager : MonoSingleton<SettingsManager>
{
    [Header("Settings")]
    [SerializeField] private SettingsData currentSettings;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Events for settings changes
    public System.Action<SettingsData> OnSettingsChanged;
    public System.Action<DifficultyLevel> OnDifficultyChanged;
    public System.Action<float, float, float> OnVolumeChanged; // master, sfx, music
    public System.Action<bool, TimerMode, float> OnTimerSettingsChanged; // enabled, mode, duration
    public System.Action<bool, int, float> OnHintSettingsChanged; // enabled, maxHints, cooldown

    private string SettingsFilePath => Path.Combine(Application.persistentDataPath, "GameSettings.json");

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Init()
    {
        DontDestroyOnLoad(gameObject);

        // Load settings or create defaults
        LoadSettings();

        // Apply settings to all systems
        ApplyAllSettings();

        DebugLog("SettingsManager initialized");
    }

    #region Loading & Saving

    /// <summary>
    /// Load settings from disk, or create default settings if none exist
    /// </summary>
    public void LoadSettings()
    {
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                currentSettings = JsonUtility.FromJson<SettingsData>(json);
                currentSettings.ValidateSettings();
                DebugLog("Settings loaded from disk");
            }
            catch (Exception e)
            {
                GameLog.LogError($"Failed to load settings: {e.Message}");
                CreateDefaultSettings();
            }
        }
        else
        {
            CreateDefaultSettings();
        }
    }

    /// <summary>
    /// Save current settings to disk
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            currentSettings.ValidateSettings();
            string json = JsonUtility.ToJson(currentSettings, true);
            File.WriteAllText(SettingsFilePath, json);
            DebugLog("Settings saved to disk");
        }
        catch (Exception e)
        {
            GameLog.LogError($"Failed to save settings: {e.Message}");
        }
    }

    /// <summary>
    /// Create and save default settings
    /// </summary>
    private void CreateDefaultSettings()
    {
        currentSettings = new SettingsData();
        SaveSettings();
        DebugLog("Created default settings");
    }

    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    public void ResetToDefaults()
    {
        CreateDefaultSettings();
        ApplyAllSettings();
        OnSettingsChanged?.Invoke(currentSettings);
        DebugLog("Settings reset to defaults");
    }

    #endregion

    #region Apply Settings

    /// <summary>
    /// Apply all settings to all game systems
    /// </summary>
    public void ApplyAllSettings()
    {
        ApplyAudioSettings();
        ApplyMovementSettings();
        ApplyDifficultySettings();
        ApplyTimerSettings();
        ApplyHintSettings();

        // Notify listeners
        OnSettingsChanged?.Invoke(currentSettings);

        DebugLog("All settings applied to game systems");
    }

    /// <summary>
    /// Apply audio settings to AudioManager
    /// </summary>
    private void ApplyAudioSettings()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(currentSettings.masterVolume);
            AudioManager.Instance.SetSFXVolume(currentSettings.sfxVolume);
            AudioManager.Instance.SetMusicVolume(currentSettings.musicVolume);
            DebugLog("Audio settings applied");
        }

        OnVolumeChanged?.Invoke(currentSettings.masterVolume, currentSettings.sfxVolume, currentSettings.musicVolume);
    }

    /// <summary>
    /// Apply movement settings to PlayerController
    /// </summary>
    private void ApplyMovementSettings()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementSpeed(currentSettings.movementSpeed);
            PlayerController.Instance.SetSnapTurnAngle(currentSettings.snapTurnAngle);
            DebugLog("Movement settings applied");
        }
    }

    /// <summary>
    /// Apply difficulty settings to game systems
    /// </summary>
    private void ApplyDifficultySettings()
    {
        // Apply to HealthEnergyManager
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.SetMaxHealth(currentSettings.playerMaxHealth);
            HealthEnergyManager.Instance.SetEnergyDrainRate(currentSettings.energyDrainRate);
            HealthEnergyManager.Instance.SetDamageMultiplier(currentSettings.damageMultiplier);
            HealthEnergyManager.Instance.SetUITextVisible(currentSettings.showHealthEnergyText);
            HealthEnergyManager.Instance.SetHapticsEnabled(currentSettings.healthEnergyHapticsEnabled);
            DebugLog("Health/Energy settings applied");
        }

        // TODO: Apply consumable count when ConsumableManager exists
        // if (ConsumableManager.Instance != null)
        // {
        //     ConsumableManager.Instance.SetConsumableCount(currentSettings.consumableObjectCount);
        // }

        DebugLog("Difficulty settings applied");
    }

    /// <summary>
    /// Apply timer settings to GameTimer
    /// </summary>
    /// <param name="skipIfRunning">If true, skip applying settings if timer is already running (prevents reset on load)</param>
    public void ApplyTimerSettings(bool skipIfRunning = false)
    {
        if (GameTimer.Instance != null)
        {
            // CRITICAL FIX: Don't reset timer if it's already running (from save load)
            if (skipIfRunning && GameTimer.Instance.IsRunning)
            {
                GameLog.Log("[SettingsManager] Timer already running - skipping settings application to preserve loaded state");
                return;
            }

            // Convert SettingsData TimerMode enum to GameTimer.TimerMode enum
            GameTimer.TimerMode timerMode = GameTimer.TimerMode.Disabled;

            if (currentSettings.timerEnabled)
            {
                switch (currentSettings.timerMode)
                {
                    case TimerMode.CountUp:
                        timerMode = GameTimer.TimerMode.CountUp;
                        break;
                    case TimerMode.CountDown:
                        timerMode = GameTimer.TimerMode.CountDown;
                        break;
                    case TimerMode.Disabled:
                        timerMode = GameTimer.TimerMode.Disabled;
                        break;
                }
            }
            else
            {
                // If timer is disabled in settings, force Disabled mode
                timerMode = GameTimer.TimerMode.Disabled;
            }

            GameTimer.Instance.SetTimerMode(timerMode);
            GameTimer.Instance.SetCountdownDuration(currentSettings.timerDuration);

            DebugLog($"Timer settings applied: Mode={timerMode}, Duration={currentSettings.timerDuration}s ({currentSettings.GetTimerDurationInMinutes()} minutes)");
        }

        OnTimerSettingsChanged?.Invoke(currentSettings.timerEnabled, currentSettings.timerMode, currentSettings.timerDuration);
    }

    /// <summary>
    /// Apply hint settings to ClueManager
    /// </summary>
    private void ApplyHintSettings()
    {
        // Apply to ClueManager
        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.hintsEnabled = currentSettings.hintsEnabled;
            DebugLog("Hint settings applied to ClueManager");
        }

        // Fire event (HintsMenuUI will listen to this to update max hints and cooldown)
        OnHintSettingsChanged?.Invoke(
            currentSettings.hintsEnabled,
            currentSettings.maxManualHints,
            currentSettings.hintCooldownSeconds
        );
    }

    #endregion

    #region Individual Setting Getters/Setters

    // Difficulty
    public void SetDifficulty(DifficultyLevel difficulty)
    {
        currentSettings.ApplyDifficultyPreset(difficulty);
        ApplyDifficultySettings();
        ApplyTimerSettings(); // Timer settings are part of difficulty preset
        ApplyHintSettings(); // Hint settings are part of difficulty preset
        SaveSettings();
        OnDifficultyChanged?.Invoke(difficulty);
        DebugLog($"Difficulty changed to {difficulty}");
    }

    public DifficultyLevel GetDifficulty() => currentSettings.difficulty;

    // Timer Settings
    public void SetTimerEnabled(bool enabled)
    {
        currentSettings.timerEnabled = enabled;
        ApplyTimerSettings();
        SaveSettings();
        DebugLog($"Timer enabled: {enabled}");
    }

    public void SetTimerMode(TimerMode mode)
    {
        currentSettings.timerMode = mode;
        ApplyTimerSettings();
        SaveSettings();
        DebugLog($"Timer mode: {mode}");
    }

    public void SetTimerDuration(float durationInSeconds)
    {
        currentSettings.timerDuration = Mathf.Max(60f, durationInSeconds);
        ApplyTimerSettings();
        SaveSettings();
        DebugLog($"Timer duration: {currentSettings.timerDuration}s");
    }

    public void SetTimerDurationInMinutes(int minutes)
    {
        currentSettings.SetTimerDurationInMinutes(minutes);
        ApplyTimerSettings();
        SaveSettings();
        DebugLog($"Timer duration: {minutes} minutes");
    }

    public bool GetTimerEnabled() => currentSettings.timerEnabled;
    public TimerMode GetTimerMode() => currentSettings.timerMode;
    public float GetTimerDuration() => currentSettings.timerDuration;
    public int GetTimerDurationInMinutes() => currentSettings.GetTimerDurationInMinutes();

    // Audio
    public void SetMasterVolume(float volume)
    {
        currentSettings.masterVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
        SaveSettings();
    }

    public void SetSFXVolume(float volume)
    {
        currentSettings.sfxVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
        SaveSettings();
    }

    public void SetMusicVolume(float volume)
    {
        currentSettings.musicVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
        SaveSettings();
    }

    public float GetMasterVolume() => currentSettings.masterVolume;
    public float GetSFXVolume() => currentSettings.sfxVolume;
    public float GetMusicVolume() => currentSettings.musicVolume;

    // Movement
    public void SetMovementSpeed(float speed)
    {
        currentSettings.movementSpeed = Mathf.Clamp(speed, 0.5f, 5f);
        ApplyMovementSettings();
        SaveSettings();
    }

    public void SetSnapTurnAngle(float angle)
    {
        currentSettings.snapTurnAngle = Mathf.Clamp(angle, 15f, 90f);
        ApplyMovementSettings();
        SaveSettings();
    }

    public float GetMovementSpeed() => currentSettings.movementSpeed;
    public float GetSnapTurnAngle() => currentSettings.snapTurnAngle;

    // UI
    public void SetHealthEnergyTextVisible(bool visible)
    {
        currentSettings.showHealthEnergyText = visible;
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.SetUITextVisible(visible);
        }
        SaveSettings();
    }

    public void SetHealthEnergyHapticsEnabled(bool enabled)
    {
        currentSettings.healthEnergyHapticsEnabled = enabled;
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.SetHapticsEnabled(enabled);
        }
        SaveSettings();
    }

    public bool GetHealthEnergyTextVisible() => currentSettings.showHealthEnergyText;
    public bool GetHealthEnergyHapticsEnabled() => currentSettings.healthEnergyHapticsEnabled;

    // Hint System Settings
    public void SetHintsEnabled(bool enabled)
    {
        currentSettings.hintsEnabled = enabled;
        ApplyHintSettings();
        SaveSettings();
        DebugLog($"Hints enabled: {enabled}");
    }

    public void SetShowClueProgress(bool show)
    {
        currentSettings.showClueProgress = show;
        SaveSettings();
        DebugLog($"Show clue progress: {show}");
    }

    public void SetMaxManualHints(int maxHints)
    {
        currentSettings.maxManualHints = Mathf.Clamp(maxHints, 0, 10);
        ApplyHintSettings();
        SaveSettings();
        DebugLog($"Max manual hints: {maxHints}");
    }

    public void SetHintCooldown(float cooldownSeconds)
    {
        currentSettings.hintCooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        ApplyHintSettings();
        SaveSettings();
        DebugLog($"Hint cooldown: {cooldownSeconds}s");
    }

    public bool GetHintsEnabled() => currentSettings.hintsEnabled;
    public bool GetShowClueProgress() => currentSettings.showClueProgress;
    public int GetMaxManualHints() => currentSettings.maxManualHints;
    public float GetHintCooldown() => currentSettings.hintCooldownSeconds;

    // Difficulty values (read-only from UI, set via difficulty preset)
    public float GetPlayerMaxHealth() => currentSettings.playerMaxHealth;
    public float GetEnergyDrainRate() => currentSettings.energyDrainRate;
    public int GetConsumableObjectCount() => currentSettings.consumableObjectCount;
    public float GetDamageMultiplier() => currentSettings.damageMultiplier;

    // Get full settings data
    public SettingsData GetCurrentSettings() => currentSettings;

    #endregion

    #region Helper Methods

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            GameLog.Log($"[SettingsManager] {message}");
        }
    }

    #endregion
}