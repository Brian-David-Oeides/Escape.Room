using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeDoorAudioEvents : MonoBehaviour
{
    [SerializeField] private AudioSource handleAudioSource;
    [SerializeField] private AudioClip handleTurnSound;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;

    public void PlayHandleSound()
    {
        handleAudioSource.PlayOneShot(handleTurnSound);
    }

    public void PlayOpenSound()
    {
        handleAudioSource.PlayOneShot(doorOpenSound);
    }

    public void PlayCloseSound()
    {
        handleAudioSource.PlayOneShot(doorCloseSound);
    }
}

