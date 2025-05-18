using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioEventPlayer : MonoBehaviour
{
    public AudioSource source;

    public void PlayClip(AudioClip clip)
    {
        if (source != null && clip != null)
        {
            source.clip = clip;
            source.Play();
        }
    }
}
