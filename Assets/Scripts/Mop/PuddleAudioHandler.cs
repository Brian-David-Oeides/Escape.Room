using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuddleAudioHandler : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip cleaningSound;
    [SerializeField] private float volume = 1f;
    [SerializeField] private float maxDistance = 10f;

    private AudioSource audioSource;
    private bool isMopInPuddle = false;

    private void Start()
    {
        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D audio
        audioSource.volume = volume;
        audioSource.maxDistance = maxDistance;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (cleaningSound != null)
        {
            audioSource.clip = cleaningSound;
        }
        else
        {
            GameLog.LogWarning("[PuddleAudioHandler] No cleaning sound assigned!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if a Mop entered the puddle
        MopCleaner mop = other.GetComponentInParent<MopCleaner>();
        if (mop != null && !isMopInPuddle)
        {
            isMopInPuddle = true;

            if (audioSource != null && cleaningSound != null && !audioSource.isPlaying)
            {
                audioSource.Play();
                GameLog.Log($"[PuddleAudioHandler] ✓ Started cleaning sound (volume: {audioSource.volume}, isPlaying: {audioSource.isPlaying})");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the Mop left the puddle
        MopCleaner mop = other.GetComponentInParent<MopCleaner>();
        if (mop != null && isMopInPuddle)
        {
            isMopInPuddle = false;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                GameLog.Log("[PuddleAudioHandler] Stopped cleaning sound");
            }
        }
    }

    private void OnDisable()
    {
        // Stop audio when puddle is disabled/destroyed
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            GameLog.Log("[PuddleAudioHandler] Puddle disabled - stopped audio");
        }
    }
}