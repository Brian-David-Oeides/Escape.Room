using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockAudioManager : MonoBehaviour
{
    [Header("Audio Components")]
    public AudioSource audioSource;
    public AudioClip tickSound;

    [Header("Tick Settings")]
    public bool enableTicking = true;
    public float tickVolume = 0.5f;

    [Header("Audio Source Settings")]
    public bool playOnAwake = false;
    public bool loop = false;
    public int priority = 128;
    public float pitch = 1f;
    public float stereoPan = 0f;
    [Range(0f, 1f)]
    public float spatialBlend = 1f; // 0 = 2D, 1 = 3D
    public float dopplerLevel = 1f;
    public float spread = 0f;
    public AudioRolloffMode volumeRolloff = AudioRolloffMode.Logarithmic;
    public float minDistance = 1f;
    public float maxDistance = 500f;

    [Header("References")]
    public TimeManager timeManager;

    void Start()
    {
        // Auto-find TimeManager if not assigned
        if (timeManager == null)
            timeManager = FindObjectOfType<TimeManager>();

        if (timeManager == null)
        {
            GameLog.LogError("ClockAudioManager: No TimeManager found! Please assign one or add a TimeManager to the scene.");
            return;
        }

        // Setup AudioSource
        SetupAudioSource();

        // Subscribe to second changes for ticking
        timeManager.OnSecondChanged.AddListener(PlayTickSound);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (timeManager != null)
            timeManager.OnSecondChanged.RemoveListener(PlayTickSound);
    }

    void SetupAudioSource()
    {
        if (audioSource == null)
        {
            GameLog.LogError("ClockAudioManager: No AudioSource assigned!");
            return;
        }

        // Apply all audio settings
        audioSource.clip = tickSound;
        audioSource.volume = tickVolume;
        audioSource.playOnAwake = playOnAwake;
        audioSource.loop = loop;
        audioSource.priority = priority;
        audioSource.pitch = pitch;
        audioSource.panStereo = stereoPan;
        audioSource.spatialBlend = spatialBlend;
        audioSource.dopplerLevel = dopplerLevel;
        audioSource.spread = spread;
        audioSource.rolloffMode = volumeRolloff;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
    }

    void PlayTickSound()
    {
        if (!enableTicking || audioSource == null || tickSound == null)
            return;

        audioSource.PlayOneShot(tickSound, tickVolume);
    }

    // Public methods
    public void SetTickingEnabled(bool enabled)
    {
        enableTicking = enabled;
    }

    public void SetTickVolume(float volume)
    {
        tickVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
            audioSource.volume = tickVolume;
    }

    public void SetTickSound(AudioClip newTickSound)
    {
        tickSound = newTickSound;
        if (audioSource != null)
            audioSource.clip = tickSound;
    }

    public void ToggleTicking()
    {
        enableTicking = !enableTicking;
    }

    public void PlayCustomSound(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}