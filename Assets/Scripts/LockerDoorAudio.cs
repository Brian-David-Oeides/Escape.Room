using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockerDoorAudio : MonoBehaviour
{
    [SerializeField] private AudioClip _openDoorClip;
    [SerializeField] private AudioClip _closeDoorClip;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Called via animation event
    public void PlayOpenSound()
    {
        if (_openDoorClip != null)
        {
            audioSource.clip = _openDoorClip;
            audioSource.Play();
        }
    }

    // Called via animation event
    public void PlayCloseSound()
    {
        if (_closeDoorClip != null)
        {
            audioSource.clip = _closeDoorClip;
            audioSource.Play();
        }
    }
}
