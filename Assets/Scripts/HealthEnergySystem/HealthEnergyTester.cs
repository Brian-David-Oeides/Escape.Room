using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary test script to monitor Health/Energy values in console
/// Attach to any GameObject in the scene
/// </summary>

#if UNITY_EDITOR
public class HealthEnergyTester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private float updateInterval = 1f; // Print every second

    [Header("Manual Testing")]
    [SerializeField] private bool testDamage = false;
    [SerializeField] private float damageAmount = 10f;

    [SerializeField] private bool testRestoreHealth = false;
    [SerializeField] private float healthRestoreAmount = 20f;

    [SerializeField] private bool testRestoreEnergy = false;
    [SerializeField] private float energyRestoreAmount = 30f;

    private float timer = 0f;

    private void Start()
    {
        if (HealthEnergyManager.Instance == null)
        {
            GameLog.LogError("❌ HealthEnergyManager not found in scene!");
        }
        else
        {
            GameLog.Log("✅ HealthEnergyManager found and ready!");
        }
    }

    private void Update()
    {
        if (HealthEnergyManager.Instance == null) return;

        // Print status every interval
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            PrintStatus();
        }

        // Manual test buttons (toggle in Inspector during Play mode)
        if (testDamage)
        {
            testDamage = false;
            HealthEnergyManager.Instance.TakeDamage(damageAmount);
            GameLog.Log($"🔨 Applied {damageAmount} damage manually");
        }

        if (testRestoreHealth)
        {
            testRestoreHealth = false;
            HealthEnergyManager.Instance.RestoreHealth(healthRestoreAmount);
            GameLog.Log($"💚 Restored {healthRestoreAmount} health manually");
        }

        if (testRestoreEnergy)
        {
            testRestoreEnergy = false;
            HealthEnergyManager.Instance.RestoreEnergy(energyRestoreAmount);
            GameLog.Log($"⚡ Restored {energyRestoreAmount} energy manually");
        }
    }

    private void PrintStatus()
    {
        var manager = HealthEnergyManager.Instance;

        float health = manager.GetCurrentHealth();
        float maxHealth = manager.GetMaxHealth();
        float healthPercent = manager.GetHealthPercentage();

        float energy = manager.GetCurrentEnergy();
        float maxEnergy = manager.GetMaxEnergy();
        float energyPercent = manager.GetEnergyPercentage();

        GameLog.Log($"📊 STATUS | Health: {health:F1}/{maxHealth} ({healthPercent:F0}%) | Energy: {energy:F1}/{maxEnergy} ({energyPercent:F0}%)");

        // Warnings
        if (manager.IsLowEnergy())
        {
            GameLog.Log("⚠️ LOW ENERGY!");
        }

        if (manager.IsLowHealth())
        {
            GameLog.Log("⚠️ LOW HEALTH!");
        }

        if (manager.IsDead())
        {
            GameLog.Log("💀 DEAD!");
        }
    }
}
#endif