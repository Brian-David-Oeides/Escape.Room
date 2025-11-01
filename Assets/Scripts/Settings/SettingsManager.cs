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
                Debug.LogError($"Failed to load settings: {e.Message}");
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
            Debug.LogError($"Failed to save settings: {e.Message}");
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

        // TODO: Apply timer duration when TimerManager exists
        // if (TimerManager.Instance != null)
        // {
        //     TimerManager.Instance.SetTimerDuration(currentSettings.timerDuration);
        // }

        // TODO: Apply consumable count when ConsumableManager exists
        // if (ConsumableManager.Instance != null)
        // {
        //     ConsumableManager.Instance.SetConsumableCount(currentSettings.consumableObjectCount);
        // }

        DebugLog("Difficulty settings applied");
    }

    #endregion

    #region Individual Setting Getters/Setters

    // Difficulty
    public void SetDifficulty(DifficultyLevel difficulty)
    {
        currentSettings.ApplyDifficultyPreset(difficulty);
        ApplyDifficultySettings();
        SaveSettings();
        OnDifficultyChanged?.Invoke(difficulty);
        DebugLog($"Difficulty changed to {difficulty}");
    }

    public DifficultyLevel GetDifficulty() => currentSettings.difficulty;

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

    // Difficulty values (read-only from UI, set via difficulty preset)
    public float GetTimerDuration() => currentSettings.timerDuration;
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
            Debug.Log($"[SettingsManager] {message}");
        }
    }

    #endregion
}