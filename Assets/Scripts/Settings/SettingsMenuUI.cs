using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Settings Menu UI
/// Handles user input and communicates with SettingsManager
/// </summary>
/// 

public class SettingsMenuUI : MonoBehaviour
{
    [Header("Settings Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject hintsPanel;

    [Header("Difficulty Settings")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private TextMeshProUGUI timerDurationText;
    [SerializeField] private TextMeshProUGUI maxHealthText;
    [SerializeField] private TextMeshProUGUI energyDrainText;
    [SerializeField] private TextMeshProUGUI consumablesText;
    [SerializeField] private TextMeshProUGUI damageMultiplierText;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;

    [Header("Gameplay Settings")]
    [SerializeField] private Slider movementSpeedSlider;
    [SerializeField] private TextMeshProUGUI movementSpeedText;
    [SerializeField] private Slider snapTurnAngleSlider;
    [SerializeField] private TextMeshProUGUI snapTurnAngleText;
    [SerializeField] private Toggle healthEnergyTextToggle;
    [SerializeField] private Toggle hapticsToggle;

    [Header("Hint Settings")]
    [SerializeField] private Toggle hintsEnabledToggle;
    [SerializeField] private Toggle showClueProgressToggle;
    [SerializeField] private Slider maxManualHintsSlider;
    [SerializeField] private TextMeshProUGUI maxManualHintsText;
    [SerializeField] private Slider hintCooldownSlider;
    [SerializeField] private TextMeshProUGUI hintCooldownText;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button backButton;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Start()
    {
        // Setup button listeners
        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplySettings);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetToDefaults);

        if (backButton != null)
            backButton.onClick.AddListener(OnBack);

        // Setup slider listeners
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (movementSpeedSlider != null)
            movementSpeedSlider.onValueChanged.AddListener(OnMovementSpeedChanged);

        if (snapTurnAngleSlider != null)
            snapTurnAngleSlider.onValueChanged.AddListener(OnSnapTurnAngleChanged);

        // Setup dropdown listener
        if (difficultyDropdown != null)
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);

        // Setup toggle listeners
        if (healthEnergyTextToggle != null)
            healthEnergyTextToggle.onValueChanged.AddListener(OnHealthEnergyTextToggled);

        if (hapticsToggle != null)
            hapticsToggle.onValueChanged.AddListener(OnHapticsToggled);

        // Setup hint toggles
        if (hintsEnabledToggle != null)
            hintsEnabledToggle.onValueChanged.AddListener(OnHintsEnabledToggled);

        if (showClueProgressToggle != null)
            showClueProgressToggle.onValueChanged.AddListener(OnShowClueProgressToggled);

        // Setup hint sliders
        if (maxManualHintsSlider != null)
            maxManualHintsSlider.onValueChanged.AddListener(OnMaxManualHintsChanged);

        if (hintCooldownSlider != null)
            hintCooldownSlider.onValueChanged.AddListener(OnHintCooldownChanged);

        // Load current settings
        LoadCurrentSettings();

        // DON'T hide here - let PauseMenuManager/MainMenuHandler control visibility
        // This prevents the panel from auto-disabling when enabled by button click
    }

    #region Show/Hide

    /// <summary>
    /// Show the settings menu
    /// </summary>
    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            LoadCurrentSettings();
            DebugLog("Settings menu opened");
        }
    }

    /// <summary>
    /// Hide the settings menu
    /// </summary>
    public void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            DebugLog("Settings menu closed");
        }
    }

    /// <summary>
    /// Show specific settings panel (for tabbed interface)
    /// </summary>
    public void ShowPanel(string panelName)
    {
        // Hide all panels first
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (hintsPanel != null) hintsPanel.SetActive(false);

        // Show requested panel
        switch (panelName.ToLower())
        {
            case "difficulty":
                if (difficultyPanel != null) difficultyPanel.SetActive(true);
                break;
            case "audio":
                if (audioPanel != null) audioPanel.SetActive(true);
                break;
            case "gameplay":
                if (gameplayPanel != null) gameplayPanel.SetActive(true);
                break;
            case "hints":
                if (hintsPanel != null) hintsPanel.SetActive(true);
                break;
        }

        DebugLog($"Showing {panelName} panel");
    }

    #endregion

    #region Load Settings

    /// <summary>
    /// Load current settings from SettingsManager and update UI
    /// </summary>
    private void LoadCurrentSettings()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("[SettingsMenuUI] SettingsManager not found!");
            return;
        }

        // Load difficulty settings
        LoadDifficultySettings();

        // Load audio settings
        LoadAudioSettings();

        // Load gameplay settings
        LoadGameplaySettings();

        // Load hint settings
        LoadHintSettings();

        DebugLog("Settings loaded from SettingsManager");
    }

    private void LoadDifficultySettings()
    {
        if (SettingsManager.Instance == null) return;

        // Set difficulty dropdown
        if (difficultyDropdown != null)
        {
            difficultyDropdown.value = (int)SettingsManager.Instance.GetDifficulty();
        }

        // Update difficulty info display
        UpdateDifficultyInfoDisplay();
    }

    private void LoadAudioSettings()
    {
        if (SettingsManager.Instance == null) return;

        // Master volume
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = SettingsManager.Instance.GetMasterVolume();
            UpdateMasterVolumeText(masterVolumeSlider.value);
        }

        // SFX volume
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = SettingsManager.Instance.GetSFXVolume();
            UpdateSFXVolumeText(sfxVolumeSlider.value);
        }

        // Music volume
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = SettingsManager.Instance.GetMusicVolume();
            UpdateMusicVolumeText(musicVolumeSlider.value);
        }
    }

    private void LoadGameplaySettings()
    {
        if (SettingsManager.Instance == null) return;

        // Movement speed
        if (movementSpeedSlider != null)
        {
            movementSpeedSlider.value = SettingsManager.Instance.GetMovementSpeed();
            UpdateMovementSpeedText(movementSpeedSlider.value);
        }

        // Snap turn angle
        if (snapTurnAngleSlider != null)
        {
            snapTurnAngleSlider.value = SettingsManager.Instance.GetSnapTurnAngle();
            UpdateSnapTurnAngleText(snapTurnAngleSlider.value);
        }

        // Health/Energy text toggle
        if (healthEnergyTextToggle != null)
        {
            healthEnergyTextToggle.isOn = SettingsManager.Instance.GetHealthEnergyTextVisible();
        }

        // Haptics toggle
        if (hapticsToggle != null)
        {
            hapticsToggle.isOn = SettingsManager.Instance.GetHealthEnergyHapticsEnabled();
        }
    }

    /// <summary>
    /// Load hint settings from SettingsManager
    /// </summary>
    private void LoadHintSettings()
    {
        if (SettingsManager.Instance == null) return;

        // Hints enabled toggle
        if (hintsEnabledToggle != null)
        {
            hintsEnabledToggle.isOn = SettingsManager.Instance.GetHintsEnabled();
        }

        // Show clue progress toggle
        if (showClueProgressToggle != null)
        {
            showClueProgressToggle.isOn = SettingsManager.Instance.GetShowClueProgress();
        }

        // Max manual hints slider
        if (maxManualHintsSlider != null)
        {
            maxManualHintsSlider.value = SettingsManager.Instance.GetMaxManualHints();
            UpdateMaxManualHintsText(maxManualHintsSlider.value);
        }

        // Hint cooldown slider
        if (hintCooldownSlider != null)
        {
            hintCooldownSlider.value = SettingsManager.Instance.GetHintCooldown();
            UpdateHintCooldownText(hintCooldownSlider.value);
        }

        DebugLog("Hint settings loaded");
    }

    #endregion

    #region Difficulty Callbacks

    private void OnDifficultyChanged(int index)
    {
        DifficultyLevel difficulty = (DifficultyLevel)index;

        // Play audio feedback
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetDifficulty(difficulty);
            UpdateDifficultyInfoDisplay();
            DebugLog($"Difficulty changed to: {difficulty}");
        }
    }

    private void UpdateDifficultyInfoDisplay()
    {
        if (SettingsManager.Instance == null) return;

        // Update timer duration display
        if (timerDurationText != null)
        {
            float minutes = SettingsManager.Instance.GetTimerDuration() / 60f;
            timerDurationText.text = $"{minutes:F0} min";
        }

        // Update max health display
        if (maxHealthText != null)
        {
            maxHealthText.text = $"{SettingsManager.Instance.GetPlayerMaxHealth():F0}";
        }

        // Update energy drain display
        if (energyDrainText != null)
        {
            float drainRate = SettingsManager.Instance.GetEnergyDrainRate();
            if (drainRate == 0)
                energyDrainText.text = "None";
            else
                energyDrainText.text = $"{drainRate:F1}/s";
        }

        // Update consumables count display
        if (consumablesText != null)
        {
            consumablesText.text = $"{SettingsManager.Instance.GetConsumableObjectCount()}";
        }

        // Update damage multiplier display
        if (damageMultiplierText != null)
        {
            float multiplier = SettingsManager.Instance.GetDamageMultiplier();
            damageMultiplierText.text = $"{multiplier:F1}x";
        }
    }

    #endregion

    #region Audio Callbacks

    private void OnMasterVolumeChanged(float value)
    {
        UpdateMasterVolumeText(value);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetMasterVolume(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        UpdateSFXVolumeText(value);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetSFXVolume(value);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        UpdateMusicVolumeText(value);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetMusicVolume(value);
        }
    }

    // Increment/Decrement methods for VR buttons
    public void IncrementMasterVolume()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = Mathf.Min(1f, masterVolumeSlider.value + 0.1f);
        }
    }

    public void DecrementMasterVolume()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = Mathf.Max(0f, masterVolumeSlider.value - 0.1f);
        }
    }

    public void IncrementSFXVolume()
    {
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = Mathf.Min(1f, sfxVolumeSlider.value + 0.1f);
        }
    }

    public void DecrementSFXVolume()
    {
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = Mathf.Max(0f, sfxVolumeSlider.value - 0.1f);
        }
    }

    public void IncrementMusicVolume()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = Mathf.Min(1f, musicVolumeSlider.value + 0.1f);
        }
    }

    public void DecrementMusicVolume()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = Mathf.Max(0f, musicVolumeSlider.value - 0.1f);
        }
    }

    private void UpdateMasterVolumeText(float value)
    {
        if (masterVolumeText != null)
            masterVolumeText.text = $"{(value * 100):F0}%";
    }

    private void UpdateSFXVolumeText(float value)
    {
        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{(value * 100):F0}%";
    }

    private void UpdateMusicVolumeText(float value)
    {
        if (musicVolumeText != null)
            musicVolumeText.text = $"{(value * 100):F0}%";
    }

    #endregion

    #region Gameplay Callbacks

    private void OnMovementSpeedChanged(float value)
    {
        UpdateMovementSpeedText(value);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetMovementSpeed(value);
        }
    }

    private void OnSnapTurnAngleChanged(float value)
    {
        UpdateSnapTurnAngleText(value);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetSnapTurnAngle(value);
        }
    }

    private void OnHealthEnergyTextToggled(bool isOn)
    {
        // Play audio feedback
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetHealthEnergyTextVisible(isOn);
            DebugLog($"Health/Energy text visibility: {isOn}");
        }
    }

    private void OnHapticsToggled(bool isOn)
    {
        // Play audio feedback
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetHealthEnergyHapticsEnabled(isOn);
            DebugLog($"Haptics enabled: {isOn}");
        }
    }

    // Increment/Decrement methods for VR buttons
    public void IncrementMovementSpeed()
    {
        if (movementSpeedSlider != null)
        {
            movementSpeedSlider.value = Mathf.Min(5f, movementSpeedSlider.value + 0.5f);
        }
    }

    public void DecrementMovementSpeed()
    {
        if (movementSpeedSlider != null)
        {
            movementSpeedSlider.value = Mathf.Max(0.5f, movementSpeedSlider.value - 0.5f);
        }
    }

    public void IncrementSnapTurnAngle()
    {
        if (snapTurnAngleSlider != null)
        {
            snapTurnAngleSlider.value = Mathf.Min(90f, snapTurnAngleSlider.value + 15f);
        }
    }

    public void DecrementSnapTurnAngle()
    {
        if (snapTurnAngleSlider != null)
        {
            snapTurnAngleSlider.value = Mathf.Max(15f, snapTurnAngleSlider.value - 15f);
        }
    }

    private void UpdateMovementSpeedText(float value)
    {
        if (movementSpeedText != null)
            movementSpeedText.text = $"{value:F1} m/s";
    }

    private void UpdateSnapTurnAngleText(float value)
    {
        if (snapTurnAngleText != null)
            snapTurnAngleText.text = $"{value:F0}°";
    }

    #endregion

    #region Hint Callbacks

    /// <summary>
    /// Called when hints enabled toggle changes
    /// </summary>
    private void OnHintsEnabledToggled(bool isOn)
    {
        // Play audio feedback
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetHintsEnabled(isOn);
            DebugLog($"Hints enabled: {isOn}");
        }
    }

    /// <summary>
    /// Called when show clue progress toggle changes
    /// </summary>
    private void OnShowClueProgressToggled(bool isOn)
    {
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetShowClueProgress(isOn);
            DebugLog($"Show clue progress: {isOn}");
        }
    }

    /// <summary>
    /// Called when max manual hints slider changes
    /// </summary>
    private void OnMaxManualHintsChanged(float value)
    {
        UpdateMaxManualHintsText(value);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetMaxManualHints((int)value);
        }
    }

    /// <summary>
    /// Called when hint cooldown slider changes
    /// </summary>
    private void OnHintCooldownChanged(float value)
    {
        UpdateHintCooldownText(value);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetHintCooldown(value);
        }
    }

    /// <summary>
    /// Increment max manual hints (for VR +/- buttons)
    /// </summary>
    public void IncrementMaxManualHints()
    {
        if (maxManualHintsSlider != null)
        {
            maxManualHintsSlider.value = Mathf.Min(10f, maxManualHintsSlider.value + 1f);
        }
    }

    /// <summary>
    /// Decrement max manual hints (for VR +/- buttons)
    /// </summary>
    public void DecrementMaxManualHints()
    {
        if (maxManualHintsSlider != null)
        {
            maxManualHintsSlider.value = Mathf.Max(0f, maxManualHintsSlider.value - 1f);
        }
    }

    /// <summary>
    /// Increment hint cooldown by 30 seconds (for VR +/- buttons)
    /// </summary>
    public void IncrementHintCooldown()
    {
        if (hintCooldownSlider != null)
        {
            hintCooldownSlider.value = Mathf.Min(600f, hintCooldownSlider.value + 30f);
        }
    }

    /// <summary>
    /// Decrement hint cooldown by 30 seconds (for VR +/- buttons)
    /// </summary>
    public void DecrementHintCooldown()
    {
        if (hintCooldownSlider != null)
        {
            hintCooldownSlider.value = Mathf.Max(0f, hintCooldownSlider.value - 30f);
        }
    }

    /// <summary>
    /// Update max manual hints text display
    /// </summary>
    private void UpdateMaxManualHintsText(float value)
    {
        if (maxManualHintsText != null)
            maxManualHintsText.text = $"{(int)value}";
    }

    /// <summary>
    /// Update hint cooldown text display (shows as MM:SS)
    /// </summary>
    private void UpdateHintCooldownText(float value)
    {
        if (hintCooldownText != null)
        {
            int minutes = (int)(value / 60f);
            int seconds = (int)(value % 60f);
            hintCooldownText.text = $"{minutes}:{seconds:D2}";
        }
    }

    #endregion

    #region Button Callbacks

    private void OnApplySettings()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings();
            DebugLog("Settings applied and saved");
        }

        HideSettings();
    }

    private void OnResetToDefaults()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ResetToDefaults();
            LoadCurrentSettings();
            DebugLog("Settings reset to defaults");
        }
    }

    private void OnBack()
    {
        HideSettings();
    }

    #endregion

    #region Debug

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SettingsMenuUI] {message}");
        }
    }

    #endregion
}