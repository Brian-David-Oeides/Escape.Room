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
    [SerializeField] private float energyDrainRate = 1f; // Energy lost per second

    [Header("Cascading Damage")]
    [SerializeField] private float lowEnergyThreshold = 20f; // When to start draining health
    [SerializeField] private float healthDrainWhenLowEnergy = 2f; // Health lost per second when energy is low

    [Header("Collision Damage Settings")]
    [SerializeField] private float collisionDamageCooldown = 1f; // Prevent rapid damage
    private float lastCollisionDamageTime = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Events
    public System.Action<float> OnHealthChanged; // float = current health (0-100)
    public System.Action<float> OnEnergyChanged; // float = current energy (0-100)
    public System.Action OnPlayerDied;
    public System.Action OnLowHealth; // Triggered at 25% health
    public System.Action OnLowEnergy; // Triggered at low energy threshold

    private bool isDead = false;
    private bool hasTriggeredLowHealth = false;
    private bool hasTriggeredLowEnergy = false;

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

    private void Update()
    {
        // Only drain during gameplay
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Playing)
        {
            DrainEnergy(Time.deltaTime);
            CheckCascadingHealthDrain();
        }
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
        OnHealthChanged?.Invoke(currentHealth);
        DebugLog($"Health set to: {currentHealth}/{maxHealth}");
    }

    #endregion

    #region Energy Methods

    /// <summary>
    /// Drain energy over time
    /// </summary>
    private void DrainEnergy(float deltaTime)
    {
        if (isDead) return;

        currentEnergy -= energyDrainRate * deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

        OnEnergyChanged?.Invoke(currentEnergy);

        // Check for low energy warning
        if (currentEnergy <= lowEnergyThreshold && !hasTriggeredLowEnergy)
        {
            hasTriggeredLowEnergy = true;
            OnLowEnergy?.Invoke();
            DebugLog("⚠️ LOW ENERGY WARNING!");
        }

        // Reset flag if energy restored above threshold
        if (currentEnergy > lowEnergyThreshold)
        {
            hasTriggeredLowEnergy = false;
        }
    }

    /// <summary>
    /// When energy is depleted, start draining health
    /// </summary>
    private void CheckCascadingHealthDrain()
    {
        if (isDead) return;

        // If energy is at or below low threshold, drain health
        if (currentEnergy <= 0f)
        {
            currentHealth -= healthDrainWhenLowEnergy * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            OnHealthChanged?.Invoke(currentHealth);

            // Check for death
            if (currentHealth <= 0f && !isDead)
            {
                Die();
            }
        }
    }

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

    #region Settings (for difficulty adjustment)

    public void SetEnergyDrainRate(float rate)
    {
        energyDrainRate = rate;
        DebugLog($"Energy drain rate set to: {rate}/second");
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
            Debug.Log($"[HealthEnergyManager] {message}");
        }
    }

    #endregion
}
