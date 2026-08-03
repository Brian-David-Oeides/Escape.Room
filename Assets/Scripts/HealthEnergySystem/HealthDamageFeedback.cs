using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

/// <summary>
/// Handles visual, audio, and haptic feedback for player damage
/// - Instant red flash on damage
/// - Persistent red vignette that pulses with heartbeat rhythm
/// - Heartbeat audio that syncs with visual pulse
/// - Haptic feedback synced to heartbeat
/// All systems can be toggled on/off individually
/// </summary>
/// 

public class HealthDamageFeedback : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Image component used for screen overlay effects")]
    [SerializeField] private Image damageOverlay;

    [Header("Visual Settings")]
    [Tooltip("Enable/disable visual damage effects")]
    [SerializeField] private bool visualFXEnabled = true;

    [Tooltip("Color of the damage flash and vignette")]
    [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 0.5f); // Red with 50% alpha

    [Tooltip("Duration of instant damage flash in seconds")]
    [SerializeField] private float flashDuration = 0.3f;

    [Tooltip("Maximum alpha for flash effect")]
    [SerializeField] private float flashMaxAlpha = 0.6f;

    [Tooltip("Maximum alpha for vignette at critical health")]
    [SerializeField] private float vignetteMaxAlpha = 0.5f;

    [Header("Heartbeat Pulse Settings")]
    [Tooltip("Health thresholds for heartbeat intensity (High, Medium, Low, Critical)")]
    [SerializeField] private float highHealthThreshold = 75f;
    [SerializeField] private float mediumHealthThreshold = 50f;
    [SerializeField] private float lowHealthThreshold = 25f;

    [Tooltip("Heartbeat rate in BPM for each health tier")]
    [SerializeField] private float heartbeatBPM_High = 60f;      // Slow/none
    [SerializeField] private float heartbeatBPM_Medium = 80f;    // Moderate
    [SerializeField] private float heartbeatBPM_Low = 110f;      // Fast
    [SerializeField] private float heartbeatBPM_Critical = 140f; // Rapid

    [Tooltip("Pulse intensity (how much alpha changes during pulse)")]
    [SerializeField] private float pulseIntensity = 0.15f;

    [Header("Audio Settings")]
    [Tooltip("Enable/disable heartbeat audio")]
    [SerializeField] private bool audioFXEnabled = true;

    [Tooltip("Heartbeat audio clips (can use multiple for variation)")]
    [SerializeField] private AudioClip[] heartbeatSounds;

    [Tooltip("Volume multiplier for heartbeat audio")]
    [SerializeField] private float heartbeatVolume = 0.5f;

    [Header("Haptic Settings")]
    [Tooltip("Enable/disable haptic feedback")]
    [SerializeField] private bool hapticFXEnabled = true;

    [Tooltip("Haptic intensity for heartbeat pulse (0-1)")]
    [SerializeField] private float hapticIntensity = 0.3f;

    [Tooltip("Haptic duration for each pulse")]
    [SerializeField] private float hapticDuration = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Private variables
    private AudioSource audioSource;
    private float currentVignetteAlpha = 0f;
    private float targetVignetteAlpha = 0f;
    private float currentHeartbeatBPM = 60f;
    private float lastHeartbeatTime = 0f;
    private bool isFlashing = false;
    private Coroutine flashCoroutine;
    private XRBaseController[] controllers;

    private void Awake()
    {
        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound (not spatial)
        audioSource.volume = heartbeatVolume;

        // Make sure we have an overlay image
        if (damageOverlay == null)
        {
            GameLog.LogError("[HealthDamageFeedback] Damage overlay Image not assigned!");
        }
        else
        {
            // Start with invisible overlay
            SetOverlayAlpha(0f);
        }
    }

    private void Start()
    {
        // Subscribe to health changes
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.OnHealthChanged += OnHealthChanged;
        }

        // Find VR controllers for haptics
        controllers = FindObjectsOfType<XRBaseController>();

        if (showDebugLogs)
        {
            GameLog.Log("[HealthDamageFeedback] System initialized");
            GameLog.Log($"[HealthDamageFeedback] Visual: {visualFXEnabled}, Audio: {audioFXEnabled}, Haptics: {hapticFXEnabled}");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void Update()
    {
        // Only process if game is playing
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
            return;

        // Update vignette and heartbeat
        UpdateVignetteAndHeartbeat();
    }

    /// <summary>
    /// Called when player health changes
    /// </summary>
    private void OnHealthChanged(float newHealth)
    {
        // Trigger instant flash effect
        TriggerDamageFlash();

        // Update vignette intensity based on health
        UpdateVignetteIntensity();

        if (showDebugLogs)
        {
            GameLog.Log($"[HealthDamageFeedback] Health changed: {newHealth:F1}%");
        }
    }

    /// <summary>
    /// Trigger instant red flash effect
    /// </summary>
    public void TriggerDamageFlash()
    {
        if (!visualFXEnabled || damageOverlay == null) return;

        // Stop any existing flash
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        // Start new flash
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// Flash coroutine
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / flashDuration;

            // Fade in quickly, fade out slowly
            float flashAlpha = Mathf.Lerp(flashMaxAlpha, 0f, Mathf.Pow(progress, 2f));

            // Add flash alpha to vignette alpha
            SetOverlayAlpha(targetVignetteAlpha + flashAlpha);

            yield return null;
        }

        isFlashing = false;
        flashCoroutine = null;
    }

    /// <summary>
    /// Update vignette intensity based on current health
    /// </summary>
    private void UpdateVignetteIntensity()
    {
        if (HealthEnergyManager.Instance == null) return;

        float healthPercentage = HealthEnergyManager.Instance.GetHealthPercentage();

        // Calculate base vignette alpha based on health
        if (healthPercentage >= highHealthThreshold)
        {
            targetVignetteAlpha = 0f; // No vignette at high health
            currentHeartbeatBPM = heartbeatBPM_High;
        }
        else if (healthPercentage >= mediumHealthThreshold)
        {
            float t = 1f - ((healthPercentage - mediumHealthThreshold) / (highHealthThreshold - mediumHealthThreshold));
            targetVignetteAlpha = Mathf.Lerp(0f, vignetteMaxAlpha * 0.3f, t);
            currentHeartbeatBPM = heartbeatBPM_Medium;
        }
        else if (healthPercentage >= lowHealthThreshold)
        {
            float t = 1f - ((healthPercentage - lowHealthThreshold) / (mediumHealthThreshold - lowHealthThreshold));
            targetVignetteAlpha = Mathf.Lerp(vignetteMaxAlpha * 0.3f, vignetteMaxAlpha * 0.6f, t);
            currentHeartbeatBPM = heartbeatBPM_Low;
        }
        else
        {
            float t = 1f - (healthPercentage / lowHealthThreshold);
            targetVignetteAlpha = Mathf.Lerp(vignetteMaxAlpha * 0.6f, vignetteMaxAlpha, t);
            currentHeartbeatBPM = heartbeatBPM_Critical;
        }
    }

    /// <summary>
    /// Update vignette and trigger heartbeat effects
    /// </summary>
    private void UpdateVignetteAndHeartbeat()
    {
        if (HealthEnergyManager.Instance == null) return;

        float healthPercentage = HealthEnergyManager.Instance.GetHealthPercentage();

        // Don't pulse if health is too high
        if (healthPercentage >= highHealthThreshold)
        {
            // Just maintain base vignette (which should be 0)
            currentVignetteAlpha = Mathf.Lerp(currentVignetteAlpha, targetVignetteAlpha, Time.deltaTime * 5f);
            if (!isFlashing)
            {
                SetOverlayAlpha(currentVignetteAlpha);
            }
            return;
        }

        // Calculate heartbeat interval from BPM
        float heartbeatInterval = 60f / currentHeartbeatBPM;

        // Check if it's time for a heartbeat
        if (Time.time - lastHeartbeatTime >= heartbeatInterval)
        {
            lastHeartbeatTime = Time.time;
            TriggerHeartbeat();
        }

        // Smooth vignette alpha transitions
        currentVignetteAlpha = Mathf.Lerp(currentVignetteAlpha, targetVignetteAlpha, Time.deltaTime * 5f);

        // Apply pulse effect
        float pulsePhase = (Time.time - lastHeartbeatTime) / heartbeatInterval;
        float pulseAmount = 0f;

        // Create a pulse wave (quick spike, slow decay)
        if (pulsePhase < 0.2f)
        {
            pulseAmount = Mathf.Sin(pulsePhase * Mathf.PI / 0.2f) * pulseIntensity;
        }

        // Apply vignette with pulse
        if (!isFlashing)
        {
            SetOverlayAlpha(currentVignetteAlpha + pulseAmount);
        }
    }

    /// <summary>
    /// Trigger all heartbeat effects (visual pulse, audio, haptics)
    /// </summary>
    private void TriggerHeartbeat()
    {
        // Play heartbeat audio
        if (audioFXEnabled && heartbeatSounds != null && heartbeatSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = heartbeatSounds[Random.Range(0, heartbeatSounds.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, heartbeatVolume);
            }
        }

        // Trigger haptic feedback
        if (hapticFXEnabled && controllers != null)
        {
            foreach (XRBaseController controller in controllers)
            {
                if (controller != null)
                {
                    controller.SendHapticImpulse(hapticIntensity, hapticDuration);
                }
            }
        }

        if (showDebugLogs)
        {
            float healthPercentage = HealthEnergyManager.Instance.GetHealthPercentage();
            GameLog.Log($"[HealthDamageFeedback] Heartbeat at {currentHeartbeatBPM:F0} BPM (Health: {healthPercentage:F1}%)");
        }
    }

    /// <summary>
    /// Set overlay alpha
    /// </summary>
    private void SetOverlayAlpha(float alpha)
    {
        if (damageOverlay == null || !visualFXEnabled) return;

        Color color = damageColor;
        color.a = Mathf.Clamp01(alpha);
        damageOverlay.color = color;
    }

    #region Public Toggle Methods (for Settings Menu)

    /// <summary>
    /// Enable or disable visual damage effects
    /// </summary>
    public void SetVisualFXEnabled(bool enabled)
    {
        visualFXEnabled = enabled;

        if (!enabled && damageOverlay != null)
        {
            SetOverlayAlpha(0f);
        }

        if (showDebugLogs)
        {
            GameLog.Log($"[HealthDamageFeedback] Visual FX {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }

    /// <summary>
    /// Enable or disable audio effects
    /// </summary>
    public void SetAudioFXEnabled(bool enabled)
    {
        audioFXEnabled = enabled;

        if (!enabled && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (showDebugLogs)
        {
            GameLog.Log($"[HealthDamageFeedback] Audio FX {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }

    /// <summary>
    /// Enable or disable haptic feedback
    /// </summary>
    public void SetHapticFXEnabled(bool enabled)
    {
        hapticFXEnabled = enabled;

        if (showDebugLogs)
        {
            GameLog.Log($"[HealthDamageFeedback] Haptic FX {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }

    /// <summary>
    /// Set heartbeat audio volume
    /// </summary>
    public void SetHeartbeatVolume(float volume)
    {
        heartbeatVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = heartbeatVolume;
        }
    }

    /// <summary>
    /// Get current visual FX state
    /// </summary>
    public bool IsVisualFXEnabled() => visualFXEnabled;

    /// <summary>
    /// Get current audio FX state
    /// </summary>
    public bool IsAudioFXEnabled() => audioFXEnabled;

    /// <summary>
    /// Get current haptic FX state
    /// </summary>
    public bool IsHapticFXEnabled() => hapticFXEnabled;

    #endregion
}