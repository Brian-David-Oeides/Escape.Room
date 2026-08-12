using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuddleHazard : MonoBehaviour, ISaveable
{
    [Header("Puddle State")]
    [SerializeField] private bool isPuddlePresent = true;

    [Header("References")]
    [SerializeField] private GameObject puddleVisual;
    [SerializeField] private ParticleSystem waterDripParticles;
    [SerializeField] private LeverToggle leverToggle;
    [SerializeField] private Transform sparkSpawnPoint;

    [Header("Damage Settings")]
    [SerializeField] private float electricShockDamage = 35f;
    [SerializeField] private AudioClip electricShockSound;
    [SerializeField] private GameObject sparkParticlesPrefab;

    [Header("Hint System")]
    [SerializeField] private string puzzleID = "breaker_panel";
    [SerializeField] private int hintThreshold = 2;

    [Header("Warning Settings")]
    [SerializeField] private float warningDisplayTime = 3f;

    private AudioSource audioSource;
    private Collider hazardCollider;
    private PuddleAudioHandler puddleAudioHandler;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D audio
        }

        hazardCollider = GetComponent<Collider>();
        puddleAudioHandler = GetComponent<PuddleAudioHandler>();
    }

    /// <summary>
    /// Check if lever can be safely activated
    /// </summary>
    public bool CanActivateLever()
    {
        return !isPuddlePresent;
    }

    /// <summary>
    /// Called when player tries to flip lever while puddle exists
    /// </summary>
    public void OnDangerousActivationAttempt()
    {
        // Apply electric shock damage
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.TakeDamage(electricShockDamage);
            GameLog.Log($"[PuddleHazard] ⚡ Electric shock! Applied {electricShockDamage} damage");
        }

        // Play electric shock effects
        PlayElectricShockEffects();

        // Register failed attempt for hint system
        ClueManager.Instance?.RegisterFailedAttempt(puzzleID, hintThreshold);
        GameLog.Log($"[PuddleHazard] Failed attempt registered for hint system");
    }

    /// <summary>
    /// Called by MopCleaner when puddle is cleaned
    /// </summary>
    public void RemovePuddle()
    {
        isPuddlePresent = false;
        ApplyRemovedVisuals();

        GameLog.Log("[PuddleHazard] Puddle removed - lever can now be safely used");
        PuzzleManager.Instance?.RegisterPuzzleCompletion(puzzleID);
    }

    /// <summary>
    /// ARCHITECTURAL FIX: Does NOT use SetActive(false) to avoid save bug (same
    /// pattern as PuzzleBase.ApplyCompletedStateVisuals) - disables the visual/
    /// interactive components instead, keeping this GameObject active and
    /// discoverable by FindObjectsOfType for the save system.
    /// </summary>
    private void ApplyRemovedVisuals()
    {
        if (puddleVisual != null)
        {
            puddleVisual.SetActive(false);
        }

        if (waterDripParticles != null)
        {
            waterDripParticles.Stop();
        }

        if (hazardCollider != null)
        {
            hazardCollider.enabled = false;
        }

        if (puddleAudioHandler != null)
        {
            puddleAudioHandler.enabled = false; // fires its OnDisable, stopping any in-progress cleaning-loop audio
        }

        if (audioSource != null)
        {
            audioSource.enabled = false;
        }
    }

    private void PlayElectricShockEffects()
    {
        // Play shock sound
        if (audioSource != null && electricShockSound != null)
        {
            audioSource.PlayOneShot(electricShockSound);
        }

        // Instantiate spark effect at spawn point
        if (sparkParticlesPrefab != null)
        {
            Vector3 spawnPosition = sparkSpawnPoint != null ? sparkSpawnPoint.position : leverToggle.transform.position;
            GameObject sparkEffect = Instantiate(sparkParticlesPrefab, spawnPosition, Quaternion.identity);

            // Auto-destroy after 2 seconds
            Destroy(sparkEffect, 2f);
        }
    }

    /// <summary>
    /// Check if puddle is currently present
    /// </summary>
    public bool IsPuddlePresent()
    {
        return isPuddlePresent;
    }

    #region ISaveable Implementation

    public string SaveID => puzzleID;

    public void SaveState(SaveData saveData)
    {
        if (!isPuddlePresent && !saveData.completedPuzzleIDs.Contains(puzzleID))
        {
            saveData.completedPuzzleIDs.Add(puzzleID);
        }
    }

    public void LoadState(SaveData saveData)
    {
        if (saveData.completedPuzzleIDs.Contains(puzzleID))
        {
            isPuddlePresent = false;
            ApplyRemovedVisuals();
        }
    }

    #endregion
}
