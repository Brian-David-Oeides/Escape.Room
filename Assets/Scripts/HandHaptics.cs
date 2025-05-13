/*
 * HandHaptics.cs
 * 
 * Copyright © 2025 Brian David
 * All Rights Reserved
 *
 * Attach this script to each hand controller to enable haptic feedback.
 * Other scripts can find and call this script's methods via events.
 * 
 * Created: May 13, 2025
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HandHaptics : MonoBehaviour
{
    [Header("Haptic Settings")]
    [Range(0, 1)]
    public float defaultIntensity = 0.7f;

    [Range(0.01f, 2f)]
    public float defaultDuration = 0.2f;

    public bool isLeftHand = false;

    [Header("Audio Settings")]
    [Tooltip("Audio source for haptic sounds. If not set, one will be created.")]
    public AudioSource audioSource;

    [Tooltip("Default sound to play with haptic feedback")]
    public AudioClip defaultSound;

    [Tooltip("Success pattern sound")]
    public AudioClip successSound;

    [Tooltip("Error pattern sound")]
    public AudioClip errorSound;

    [Range(0, 1)]
    [Tooltip("Volume for haptic audio")]
    public float volume = 0.5f;

    [Tooltip("Pitch variation to add randomness (-0.1 to +0.1)")]
    [Range(0, 0.1f)]
    public float pitchVariation = 0.05f;

    [Header("Advanced")]
    [Tooltip("Spatial blend for audio (0=2D, 1=3D)")]
    [Range(0, 1)]
    public float spatialBlend = 0.8f;


    private ActionBasedController controller;

    private void Awake()
    {
        controller = GetComponent<ActionBasedController>();

        // Auto-detect if this is a left hand based on name if not set
        if (gameObject.name.ToLower().Contains("left"))
        {
            isLeftHand = true;
        }

        // Setup audio source if needed
        SetupAudioSource();
    }

    private void SetupAudioSource()
    {
        // Create audio source if not provided
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Configure audio source
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.volume = volume;
        audioSource.priority = 128; // Medium priority
        audioSource.minDistance = 0.1f;
        audioSource.maxDistance = 5f;
    }

    // Simple haptic pulse with default settings
    public void TriggerHaptic()
    {
        if (controller != null)
        {
            controller.SendHapticImpulse(defaultIntensity, defaultDuration);
        }

        if (audioSource != null && defaultSound != null)
        {
            PlaySound(defaultSound);
        }
    }

    // Haptic pulse with custom intensity
    public void TriggerHaptic(float intensity)
    {
        if (controller != null)
        {
            controller.SendHapticImpulse(intensity, defaultDuration);
        }

        if (audioSource != null && defaultSound != null)
        {
            PlaySound(defaultSound, Mathf.Clamp(intensity, 0.3f, 1f));
        }
    }

    // Haptic pulse with custom intensity and duration
    public void TriggerHaptic(float intensity, float duration)
    {
        if (controller != null)
        {
            controller.SendHapticImpulse(intensity, duration);
        }

        if (audioSource != null && defaultSound != null)
        {
            PlaySound(defaultSound, Mathf.Clamp(intensity, 0.3f, 1f));
        }
    }

    // Haptic pulse with custom sound
    public void TriggerHapticWithSound(AudioClip sound, float intensity = -1)
    {
        if (intensity < 0) intensity = defaultIntensity;

        if (controller != null)
        {
            controller.SendHapticImpulse(intensity, defaultDuration);
        }

        if (audioSource != null && sound != null)
        {
            PlaySound(sound);
        }
    }

    // Success pattern (crescendo)
    public void TriggerSuccessHaptic()
    {
        StartCoroutine(SuccessPattern());
    }

    // Error pattern (staccato)
    public void TriggerErrorHaptic()
    {
        StartCoroutine(ErrorPattern());
    }

    private System.Collections.IEnumerator SuccessPattern()
    {
        if (controller == null) yield break;

        // Play success sound if available
        if (audioSource != null && successSound != null)
        {
            audioSource.clip = successSound;
            audioSource.volume = volume;
            audioSource.Play();
        }
        else
        {

            controller.SendHapticImpulse(0.3f, 0.1f);
            if (audioSource != null && defaultSound != null) PlaySound(defaultSound, 0.3f);

            yield return new WaitForSeconds(0.1f);

            controller.SendHapticImpulse(0.5f, 0.1f);
            if (audioSource != null && defaultSound != null) PlaySound(defaultSound, 0.5f);

            yield return new WaitForSeconds(0.1f);

            controller.SendHapticImpulse(0.7f, 0.2f);
            if (audioSource != null && defaultSound != null) PlaySound(defaultSound, 0.7f);
        }
    }

    private System.Collections.IEnumerator ErrorPattern()
    {
        if (controller == null) yield break;

        // Play error sound if available
        if (audioSource != null && errorSound != null)
        {
            audioSource.clip = errorSound;
            audioSource.volume = volume;
            audioSource.Play();
        }
        else
        {
            // Otherwise use separate sounds for each pulse
            controller.SendHapticImpulse(0.7f, 0.1f);
            if (audioSource != null && defaultSound != null) PlaySound(defaultSound, 0.7f, 1.2f);

            yield return new WaitForSeconds(0.1f);

            controller.SendHapticImpulse(0.0f, 0.1f);
            yield return new WaitForSeconds(0.1f);

            controller.SendHapticImpulse(0.7f, 0.1f);
            if (audioSource != null && defaultSound != null) PlaySound(defaultSound, 0.7f, 1.2f);
        }
    }

    // Helper method to play a sound with variation
    private void PlaySound(AudioClip clip, float volumeMultiplier = 1.0f, float pitchMultiplier = 1.0f)
    {
        if (audioSource == null || clip == null) return;

        // Add slight variations to make repeated sounds more natural
        audioSource.pitch = pitchMultiplier * Random.Range(1.0f - pitchVariation, 1.0f + pitchVariation);
        audioSource.PlayOneShot(clip, volume * volumeMultiplier);
    }
}