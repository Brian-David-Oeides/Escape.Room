using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Applies damage to the player on collision.
/// Attach this to any object that should damage the player when touched.
/// </summary>
/// 

public class DamageSource : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Base damage amount (will be multiplied by velocity and difficulty)")]
    [SerializeField] private float baseDamage = 10f;

    [Tooltip("Cooldown time between damage applications (prevents spam damage)")]
    [SerializeField] private float damageCooldown = 1f;

    [Header("Velocity-Based Damage Multiplier")]
    [Tooltip("Enable velocity-based damage scaling")]
    [SerializeField] private bool useVelocityMultiplier = true;

    [Tooltip("Minimum collision velocity (m/s) for minimum multiplier")]
    [SerializeField] private float minVelocity = 0.5f;

    [Tooltip("Maximum collision velocity (m/s) for maximum multiplier")]
    [SerializeField] private float maxVelocity = 5f;

    [Tooltip("Damage multiplier at minimum velocity")]
    [SerializeField] private float minDamageMultiplier = 0.5f;

    [Tooltip("Damage multiplier at maximum velocity")]
    [SerializeField] private float maxDamageMultiplier = 2f;

    [Header("Damage Type (Optional - for future expansion)")]
    [SerializeField] private DamageType damageType = DamageType.Generic;

    [Header("Feedback Settings")]
    [Tooltip("Play haptic feedback on damage")]
    [SerializeField] private bool useHapticFeedback = true;

    [Tooltip("Haptic intensity (0-1)")]
    [SerializeField] private float hapticIntensity = 0.5f;

    [Tooltip("Haptic duration in seconds")]
    [SerializeField] private float hapticDuration = 0.2f;

    [Header("Health-Based Audio (4 clips)")]
    [Tooltip("Audio for 100-75% health (mild damage)")]
    [SerializeField] private AudioClip damageSound_Mild;

    [Tooltip("Audio for 74-50% health (moderate damage)")]
    [SerializeField] private AudioClip damageSound_Moderate;

    [Tooltip("Audio for 49-25% health (serious damage)")]
    [SerializeField] private AudioClip damageSound_Serious;

    [Tooltip("Audio for 24-0% health (critical damage)")]
    [SerializeField] private AudioClip damageSound_Critical;

    [Tooltip("Particle effect to spawn on damage (optional)")]
    [SerializeField] private GameObject damageParticles;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Private variables
    private float lastDamageTime = -999f;
    private AudioSource audioSource;

    private void Awake()
    {
        // Get or add AudioSource component for sound effects
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && HasAnyDamageSound())
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
    }

    /// <summary>
    /// Check if any damage sound is assigned
    /// </summary>
    private bool HasAnyDamageSound()
    {
        return damageSound_Mild != null ||
               damageSound_Moderate != null ||
               damageSound_Serious != null ||
               damageSound_Critical != null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if enough time has passed since last damage
        if (Time.time - lastDamageTime < damageCooldown)
        {
            return; // Still in cooldown
        }

        // Check if we hit the player (XR Origin or PlayerController)
        if (IsPlayer(collision.gameObject))
        {
            ApplyDamage(collision);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if enough time has passed since last damage
        if (Time.time - lastDamageTime < damageCooldown)
        {
            return; // Still in cooldown
        }

        // Check if we hit the player (XR Origin or PlayerController)
        if (IsPlayer(other.gameObject))
        {
            ApplyDamage(other);
        }
    }

    private bool IsPlayer(GameObject obj)
    {
        // Check multiple ways to identify the player
        // 1. Check for XR Origin component
        if (obj.GetComponentInParent<XROrigin>() != null)
            return true;

        // 2. Check for PlayerController component
        if (obj.GetComponentInParent<PlayerController>() != null)
            return true;

        // 3. Check by tag
        if (obj.CompareTag("Player"))
            return true;

        // 4. Check by name (XR Origin is often named this)
        if (obj.name.Contains("XR Origin") || obj.name.Contains("XROrigin"))
            return true;

        return false;
    }

    private void ApplyDamage(Collision collision)
    {
        float collisionVelocity = collision.relativeVelocity.magnitude;
        ApplyDamageInternal(collision.GetContact(0).point, collisionVelocity);
    }

    private void ApplyDamage(Collider other)
    {
        // Triggers don't have velocity data, use 0 (base damage only)
        ApplyDamageInternal(other.ClosestPoint(transform.position), 0f);
    }

    private void ApplyDamageInternal(Vector3 contactPoint, float collisionVelocity)
    {
        // Update cooldown timer
        lastDamageTime = Time.time;

        // Calculate final damage based on velocity and difficulty
        float finalDamage = CalculateFinalDamage(collisionVelocity);

        // Apply damage through HealthEnergyManager
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.TakeDamage(finalDamage);

            if (showDebugLogs)
            {
                if (useVelocityMultiplier && collisionVelocity > 0)
                {
                    GameLog.Log($"[DamageSource] Applied {finalDamage:F1} damage (Base: {baseDamage}, Velocity: {collisionVelocity:F2} m/s). Cooldown: {damageCooldown}s");
                }
                else
                {
                    GameLog.Log($"[DamageSource] Applied {finalDamage:F1} damage (Base only). Cooldown: {damageCooldown}s");
                }
            }
        }
        else
        {
            GameLog.LogError("[DamageSource] HealthEnergyManager instance not found!");
        }

        // Trigger feedback effects
        PlayHapticFeedback();
        PlayDamageSound();
        SpawnDamageParticles(contactPoint);
    }

    /// <summary>
    /// Calculate final damage based on base damage, collision velocity, and difficulty multiplier
    /// Formula: finalDamage = baseDamage × velocityMultiplier × difficultyMultiplier
    /// </summary>
    private float CalculateFinalDamage(float collisionVelocity)
    {
        float damage = baseDamage;

        // Apply velocity multiplier if enabled
        if (useVelocityMultiplier && collisionVelocity > 0)
        {
            // Clamp velocity to min/max range
            float clampedVelocity = Mathf.Clamp(collisionVelocity, minVelocity, maxVelocity);

            // Calculate velocity percentage (0 to 1)
            float velocityPercent = (clampedVelocity - minVelocity) / (maxVelocity - minVelocity);

            // Interpolate between min and max multiplier
            float velocityMultiplier = Mathf.Lerp(minDamageMultiplier, maxDamageMultiplier, velocityPercent);

            // Apply velocity multiplier
            damage *= velocityMultiplier;

            if (showDebugLogs)
            {
                GameLog.Log($"[DamageSource] Velocity: {collisionVelocity:F2} m/s → Velocity Multiplier: {velocityMultiplier:F2}x");
            }
        }

        // Apply global difficulty multiplier from HealthEnergyManager (Easy: 0.5x, Normal: 1.0x, Hard: 2.0x)
        if (HealthEnergyManager.Instance != null)
        {
            float difficultyMultiplier = HealthEnergyManager.Instance.GetDamageMultiplier();
            damage *= difficultyMultiplier;

            if (showDebugLogs)
            {
                GameLog.Log($"[DamageSource] Base: {baseDamage} → After modifiers: {damage:F1} (Difficulty Multiplier: {difficultyMultiplier}x)");
            }
        }

        return damage;
    }

    private void PlayHapticFeedback()
    {
        if (!useHapticFeedback) return;

        // Try to find XR controllers and send haptic impulse
        XRBaseController[] controllers = FindObjectsOfType<XRBaseController>();

        foreach (XRBaseController controller in controllers)
        {
            controller.SendHapticImpulse(hapticIntensity, hapticDuration);

            if (showDebugLogs)
            {
                GameLog.Log($"[DamageSource] Sent haptic feedback to {controller.name}");
            }
        }
    }

    private void PlayDamageSound()
    {
        if (audioSource == null) return;

        // Get current health percentage from HealthEnergyManager
        float healthPercentage = 100f;
        if (HealthEnergyManager.Instance != null)
        {
            healthPercentage = HealthEnergyManager.Instance.GetHealthPercentage();
        }

        // Select appropriate audio clip based on health percentage
        AudioClip clipToPlay = null;

        if (healthPercentage >= 75f)
        {
            clipToPlay = damageSound_Mild;
            if (showDebugLogs)
                GameLog.Log($"[DamageSource] Playing MILD damage sound (Health: {healthPercentage:F1}%)");
        }
        else if (healthPercentage >= 50f)
        {
            clipToPlay = damageSound_Moderate;
            if (showDebugLogs)
                GameLog.Log($"[DamageSource] Playing MODERATE damage sound (Health: {healthPercentage:F1}%)");
        }
        else if (healthPercentage >= 25f)
        {
            clipToPlay = damageSound_Serious;
            if (showDebugLogs)
                GameLog.Log($"[DamageSource] Playing SERIOUS damage sound (Health: {healthPercentage:F1}%)");
        }
        else
        {
            clipToPlay = damageSound_Critical;
            if (showDebugLogs)
                GameLog.Log($"[DamageSource] Playing CRITICAL damage sound (Health: {healthPercentage:F1}%)");
        }

        // Play the selected clip if it exists
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
        else if (showDebugLogs)
        {
            GameLog.LogWarning("[DamageSource] No audio clip assigned for current health tier");
        }
    }

    private void SpawnDamageParticles(Vector3 position)
    {
        if (damageParticles != null)
        {
            GameObject particles = Instantiate(damageParticles, position, Quaternion.identity);

            // Auto-destroy particles after 2 seconds
            Destroy(particles, 2f);

            if (showDebugLogs)
            {
                GameLog.Log("[DamageSource] Spawned damage particles");
            }
        }
    }

    // Public method to manually trigger damage (for testing or special cases)
    public void TriggerDamage()
    {
        if (Time.time - lastDamageTime >= damageCooldown)
        {
            ApplyDamageInternal(transform.position, 0f);
        }
    }

    // Enum for damage types (for future expansion)
    public enum DamageType
    {
        Generic,
        Fire,
        Electric,
        Sharp,
        Blunt,
        Poison,
        Cold
    }
}