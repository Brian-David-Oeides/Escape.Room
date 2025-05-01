using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeDoorAudioEvents : MonoBehaviour
{
    [SerializeField] 
    private AudioSource _handleAudioSource;
    [SerializeField] 
    private AudioClip _handleTurnSound;
    [SerializeField] 
    private AudioClip _doorOpenSound;
    [SerializeField] 
    private AudioClip _doorCloseSound;

    public void PlayHandleSound()
    {
        _handleAudioSource.PlayOneShot(_handleTurnSound);
    }

    public void PlayOpenSound()
    {
        _handleAudioSource.PlayOneShot(_doorOpenSound);
    }

    public void PlayCloseSound()
    {
        _handleAudioSource.PlayOneShot(_doorCloseSound);
    }
}

