using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Drains energy from HealthEnergyManager when player teleports
/// Attach to the Teleportation Provider GameObject
/// </summary>
/// 

public class TeleportEnergyCost : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private TeleportationProvider teleportProvider;

    private void Start()
    {
        // Get the TeleportationProvider component
        teleportProvider = GetComponent<TeleportationProvider>();

        if (teleportProvider == null)
        {
            Debug.LogError("[TeleportEnergyCost] No TeleportationProvider found on this GameObject!");
            return;
        }

        // Subscribe to teleportation events
        teleportProvider.endLocomotion += OnTeleportComplete;

        DebugLog("TeleportEnergyCost initialized and listening for teleports");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (teleportProvider != null)
        {
            teleportProvider.endLocomotion -= OnTeleportComplete;
        }
    }

    /// <summary>
    /// Called when teleportation completes
    /// </summary>
    private void OnTeleportComplete(LocomotionSystem locomotionSystem)
    {
        // Drain energy from HealthEnergyManager
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.OnPlayerTeleport();
            DebugLog("Teleport completed - energy drained");
        }
        else
        {
            Debug.LogWarning("[TeleportEnergyCost] HealthEnergyManager not found! Energy not drained.");
        }
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[TeleportEnergyCost] {message}");
        }
    }
}
