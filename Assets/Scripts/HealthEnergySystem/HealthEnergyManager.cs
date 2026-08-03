using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages player health and energy with cascading damage system
/// Health: Damaged by collisions
/// Energy: Drains over time, when depleted -> health drains
/// Both can be restored by consuming food items
/// </summary>
 
public class HealthEnergyManager : MonoSingleton<HealthEnergyManager>
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;

    [Space(10)]
    [Header("Energy Drain Rates")]
    [SerializeField] private bool useMovementBasedDrain = true;
    [Tooltip("Energy drain when standing still")]
    [SerializeField] private float standingDrainRate = 0.01f;
    [Tooltip("Energy drain when walking/moving")]
    [SerializeField] private float movingDrainRate = 0.05f;
    [Tooltip("Energy cost per teleport")]
    [SerializeField] private float teleportEnergyCost = 1f;
    [Tooltip("Movement speed threshold to detect if player is moving (m/s)")]
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("Cascading Damage")]
    [SerializeField] private float lowEnergyThreshold = 20f; // When to trigger low energy warning
    [SerializeField] private float lowHealthThreshold = 25f; // When to trigger low health warning
    [SerializeField] private float healthDrainWhenLowEnergy = 2f; // Health lost per second when energy is depleted

    [Header("Collision Damage Settings")]
    [SerializeField] private float damageMultiplier = 1f; // Global difficulty multiplier for all damage
    [SerializeField] private float collisionDamageCooldown = 1f;
    private float lastCollisionDamageTime = 0f;

    [Header("UI Settings")]
    [SerializeField] private bool showHealthEnergyText = true;
    [SerializeField] private bool hapticsEnabled = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Events
    public System.Action<float> OnHealthChanged;
    public System.Action<float> OnEnergyChanged;
    public System.Action OnPlayerDied;
    public System.Action OnLowHealth;
    public System.Action OnLowEnergy;

    // Movement tracking
    private Vector3 lastPlayerPosition;
    private float currentDrainRate;
    private bool isPlayerMoving = false;

    private bool isDead = false;
    private bool hasTriggeredLowHealth = false;
    private bool hasTriggeredLowEnergy = false;

    private bool hasBeenInitializedFromSave = false;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Init()
    {
        DontDestroyOnLoad(gameObject);

        // Initialize to full health and energy
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;

        DebugLog("HealthEnergyManager initialized");
        DebugLog($"Starting Health: {currentHealth}/{maxHealth}");
        DebugLog($"Starting Energy: {currentEnergy}/{maxEnergy}");
    }

    /// <summary>
    /// Reset health and energy to starting values (for new game)
    /// </summary>
    public void ResetToStartingValues()
    {
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        isDead = false;
        hasTriggeredLowHealth = false;
        hasTriggeredLowEnergy = false;

        // Reset movement tracking
        if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            lastPlayerPosition = PlayerController.Instance.XROrigin.transform.position;
        }
        currentDrainRate = standingDrainRate;
        isPlayerMoving = false;

        // Trigger UI update
        OnHealthChanged?.Invoke(currentHealth);
        OnEnergyChanged?.Invoke(currentEnergy);

        GameLog.Log($"[HealthEnergyManager] Reset to starting values: Health {currentHealth}/{maxHealth}, Energy {currentEnergy}/{maxEnergy}");
    }

    private void Start()
    {
        // Don't reset if we've been initialized from a save
        if (hasBeenInitializedFromSave)
        {
            GameLog.Log("[HealthEnergyManager] Skipping Start() - already initialized from save");

            // Still need to initialize movement tracking
            if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
            {
                lastPlayerPosition = PlayerController.Instance.XROrigin.transform.position;
            }

            hasBeenInitializedFromSave = false; // Reset flag
            return;
        }

        // Initialize values (only for new game)
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        isDead = false;

        // Initialize movement tracking
        if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            lastPlayerPosition = PlayerController.Instance.XROrigin.transform.position;
        }

        currentDrainRate = standingDrainRate;

        GameLog.Log($"[HealthEnergyManager] HealthEnergyManager initialized");
        GameLog.Log($"[HealthEnergyManager] Starting Health: {currentHealth}/{maxHealth}");
        GameLog.Log($"[HealthEnergyManager] Starting Energy: {currentEnergy}/{maxEnergy}");
        GameLog.Log($"[HealthEnergyManager] Movement-based drain: {(useMovementBasedDrain ? "ENABLED" : "DISABLED")}");
        if (useMovementBasedDrain)
        {
            GameLog.Log($"[HealthEnergyManager] Standing drain: {standingDrainRate}/sec, Moving drain: {movingDrainRate}/sec");
        }
    }

    private void Update()
    {
        // Only drain during gameplay
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Playing)
        {
            if (!isDead)
            {
                // Update movement-based drain rate
                if (useMovementBasedDrain)
                {
                    UpdateMovementBasedDrain();
                }
                else
                {
                    // Use legacy standing drain rate (backward compatibility)
                    currentDrainRate = standingDrainRate;
                }

                // Drain energy over time
                if (currentEnergy > 0)
                {
                    currentEnergy -= currentDrainRate * Time.deltaTime;
                    currentEnergy = Mathf.Max(0, currentEnergy);
                    OnEnergyChanged?.Invoke(currentEnergy);

                    // Check for low energy warning
                    if (currentEnergy <= lowEnergyThreshold && currentEnergy > 0)
                    {
                        if (!hasTriggeredLowEnergy)
                        {
                            hasTriggeredLowEnergy = true;
                            OnLowEnergy?.Invoke();
                            GameLog.Log("[HealthEnergyManager] ⚠️ LOW ENERGY WARNING!");
                        }
                    }
                }

                // When energy is depleted, start draining health
                if (currentEnergy <= 0 && currentHealth > 0)
                {
                    currentHealth -= healthDrainWhenLowEnergy * Time.deltaTime;
                    currentHealth = Mathf.Max(0, currentHealth);
                    OnHealthChanged?.Invoke(currentHealth);

                    // Check for low health warning
                    if (currentHealth <= lowHealthThreshold && currentHealth > 0)
                    {
                        if (!hasTriggeredLowHealth)
                        {
                            hasTriggeredLowHealth = true;
                            OnLowHealth?.Invoke();
                            GameLog.Log("[HealthEnergyManager] ⚠️ LOW HEALTH WARNING!");
                        }
                    }

                    // Check for death
                    if (currentHealth <= 0)
                    {
                        Die();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Detect player movement and adjust energy drain rate accordingly
    /// </summary>
    private void UpdateMovementBasedDrain()
    {
        if (PlayerController.Instance == null || PlayerController.Instance.XROrigin == null)
        {
            currentDrainRate = standingDrainRate;
            return;
        }

        Vector3 currentPosition = PlayerController.Instance.XROrigin.transform.position;

        // Calculate movement speed
        float movementSpeed = Vector3.Distance(currentPosition, lastPlayerPosition) / Time.deltaTime;

        // Determine if player is moving
        bool wasMoving = isPlayerMoving;
        isPlayerMoving = movementSpeed > movementThreshold;

        // Update drain rate based on movement
        if (isPlayerMoving)
        {
            currentDrainRate = movingDrainRate;

            // Log when player starts moving (optional, can remove if too spammy)
            if (!wasMoving)
            {
                GameLog.Log($"[HealthEnergyManager] Player moving - drain rate: {movingDrainRate}/sec");
            }
        }
        else
        {
            currentDrainRate = standingDrainRate;

            // Log when player stops moving (optional, can remove if too spammy)
            if (wasMoving)
            {
                GameLog.Log($"[HealthEnergyManager] Player standing - drain rate: {standingDrainRate}/sec");
            }
        }

        // Update last position
        lastPlayerPosition = currentPosition;
    }

    /// <summary>
    /// Manually initialize movement tracking (called after loading save data)
    /// </summary>
    public void InitializeMovementTracking()
    {
        if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            lastPlayerPosition = PlayerController.Instance.XROrigin.transform.position;
            isPlayerMoving = false;
            currentDrainRate = standingDrainRate;

            GameLog.Log($"[HealthEnergyManager] Movement tracking initialized at position: {lastPlayerPosition}");
        }
        else
        {
            GameLog.LogWarning("[HealthEnergyManager] Could not initialize movement tracking - XR Origin not found");
        }
    }

    /// <summary>
    /// Called by teleport system to drain energy per teleport
    /// </summary>
    public void OnPlayerTeleport()
    {
        if (isDead || currentEnergy <= 0) return;

        currentEnergy -= teleportEnergyCost;
        currentEnergy = Mathf.Max(0, currentEnergy);
        OnEnergyChanged?.Invoke(currentEnergy);

        GameLog.Log($"[HealthEnergyManager] Teleport cost {teleportEnergyCost} energy! Energy: {currentEnergy:F1}/{maxEnergy}");
    }


    #region Health Methods

    /// <summary>
    /// Apply damage to health (from collisions)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        // Check cooldown to prevent rapid damage
        if (Time.time - lastCollisionDamageTime < collisionDamageCooldown)
        {
            return;
        }

        lastCollisionDamageTime = Time.time;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        DebugLog($"Took {damage} damage! Health: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth);

        // Check for low health warning
        if (currentHealth <= maxHealth * 0.25f && !hasTriggeredLowHealth)
        {
            hasTriggeredLowHealth = true;
            OnLowHealth?.Invoke();
            DebugLog("⚠️ LOW HEALTH WARNING!");
        }

        // Check for death
        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// Restore health (from consuming food)
    /// </summary>
    public void RestoreHealth(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        DebugLog($"Restored {amount} health! Health: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth);

        // Reset low health flag if healed enough
        if (currentHealth > maxHealth * 0.25f)
        {
            hasTriggeredLowHealth = false;
        }
    }

    /// <summary>
    /// Set health directly (used when loading save)
    /// </summary>
    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        hasBeenInitializedFromSave = true;

        // CRITICAL: Reset death state if health is restored
        if (currentHealth > 0)
        {
            isDead = false;
            hasTriggeredLowHealth = false;
            GameLog.Log("[HealthEnergyManager] Death state reset - player is alive");
        }

        OnHealthChanged?.Invoke(currentHealth);
        DebugLog($"Health set to: {currentHealth}/{maxHealth}");
    }

    #endregion

    #region Energy Methods

    /// <summary>
    /// Restore energy (from consuming food)
    /// </summary>
    public void RestoreEnergy(float amount)
    {
        if (isDead) return;

        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

        DebugLog($"Restored {amount} energy! Energy: {currentEnergy}/{maxEnergy}");

        OnEnergyChanged?.Invoke(currentEnergy);
    }

    /// <summary>
    /// Set energy directly (used when loading save)
    /// </summary>
    public void SetEnergy(float value)
    {
        currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
        hasBeenInitializedFromSave = true;

        // Reset low energy warning flag
        if (currentEnergy > lowEnergyThreshold)
        {
            hasTriggeredLowEnergy = false;
        }

        OnEnergyChanged?.Invoke(currentEnergy);
        DebugLog($"Energy set to: {currentEnergy}/{maxEnergy}");
    }

    #endregion

    #region Death & Reset

    private void Die()
    {
        isDead = true;
        currentHealth = 0f;

        DebugLog("💀 PLAYER DIED!");

        OnHealthChanged?.Invoke(currentHealth);
        OnPlayerDied?.Invoke();

        // Trigger Game Over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    /// <summary>
    /// Reset to full health and energy (for new game or respawn)
    /// </summary>
    public void ResetToFull()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        hasTriggeredLowHealth = false;
        hasTriggeredLowEnergy = false;

        OnHealthChanged?.Invoke(currentHealth);
        OnEnergyChanged?.Invoke(currentEnergy);

        DebugLog("Health and Energy reset to full");
    }

    #endregion

    #region Getters

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (currentHealth / maxHealth) * 100f;

    public float GetCurrentEnergy() => currentEnergy;
    public float GetMaxEnergy() => maxEnergy;
    public float GetEnergyPercentage() => (currentEnergy / maxEnergy) * 100f;

    public bool IsDead() => isDead;
    public bool IsLowHealth() => currentHealth <= maxHealth * 0.25f;
    public bool IsLowEnergy() => currentEnergy <= lowEnergyThreshold;

    #endregion

    #region Settings (for SettingsManager)

    /// <summary>
    /// Set the maximum health value (from difficulty settings)
    /// </summary>
    public void SetMaxHealth(float health)
    {
        maxHealth = Mathf.Max(25f, health);

        // Adjust current health if it exceeds new max
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth);
        }

        DebugLog($"Max health set to: {maxHealth}");
    }

    /// <summary>
    /// Set the energy drain rate (from difficulty settings)
    /// This sets the standing drain rate
    /// </summary>
    public void SetEnergyDrainRate(float rate)
    {
        standingDrainRate = Mathf.Max(0f, rate);

        // Also scale the moving drain rate proportionally
        if (rate == 0f)
        {
            movingDrainRate = 0f; // No drain at all
        }
        else
        {
            movingDrainRate = standingDrainRate * 5f; // Moving is 5x standing
        }

        DebugLog($"Energy drain rate set - Standing: {standingDrainRate}/sec, Moving: {movingDrainRate}/sec");
    }

    /// <summary>
    /// Set global damage multiplier (from difficulty settings)
    /// </summary>
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0.1f, multiplier);
        DebugLog($"Damage multiplier set to: {damageMultiplier}x");
    }

    /// <summary>
    /// Set whether health/energy text is visible in UI
    /// </summary>
    public void SetUITextVisible(bool visible)
    {
        showHealthEnergyText = visible;
        DebugLog($"UI text visibility: {visible}");

        // TODO: Apply to UI when HealthEnergyUI exists
        // if (HealthEnergyUI.Instance != null)
        // {
        //     HealthEnergyUI.Instance.SetTextVisible(visible);
        // }
    }

    /// <summary>
    /// Set whether haptics are enabled for health/energy events
    /// </summary>
    public void SetHapticsEnabled(bool enabled)
    {
        hapticsEnabled = enabled;
        DebugLog($"Haptics enabled: {enabled}");
    }

    public bool GetUITextVisible() => showHealthEnergyText;
    public bool GetHapticsEnabled() => hapticsEnabled;
    public float GetDamageMultiplier() => damageMultiplier;

    /// <summary>
    /// Set the standing drain rate (when not moving)
    /// </summary>
    public void SetStandingDrainRate(float rate)
    {
        standingDrainRate = rate;
        DebugLog($"Standing drain rate set to: {rate}/second");
    }

    /// <summary>
    /// Set the moving drain rate (when walking)
    /// </summary>
    public void SetMovingDrainRate(float rate)
    {
        movingDrainRate = rate;
        DebugLog($"Moving drain rate set to: {rate}/second");
    }

    /// <summary>
    /// Set both drain rates at once (for difficulty presets)
    /// </summary>
    public void SetDrainRates(float standingRate, float movingRate)
    {
        standingDrainRate = standingRate;
        movingDrainRate = movingRate;
        DebugLog($"Drain rates set - Standing: {standingRate}/sec, Moving: {movingRate}/sec");
    }

    /// <summary>
    /// Enable or disable movement-based drain
    /// </summary>
    public void SetMovementBasedDrain(bool enabled)
    {
        useMovementBasedDrain = enabled;
        DebugLog($"Movement-based drain: {(enabled ? "ENABLED" : "DISABLED")}");
    }

    public void SetHealthDrainWhenLowEnergy(float rate)
    {
        healthDrainWhenLowEnergy = rate;
        DebugLog($"Health drain (low energy) set to: {rate}/second");
    }

    public void SetLowEnergyThreshold(float threshold)
    {
        lowEnergyThreshold = threshold;
        DebugLog($"Low energy threshold set to: {threshold}");
    }

    #endregion

    #region Debug

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            GameLog.Log($"[HealthEnergyManager] {message}");
        }
    }

    #endregion
}
