using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameDevHQ.FileBase.Plugins.FPS_Character_Controller;

public class FootstepSFX : MonoBehaviour
{
    [Header("Place your footstep WAV here")]
    public AudioClip Footstep_SFX;
    [Header("Place your Jump WAV here")]
    public AudioClip Jump_SFX;
    [Header("Drag audio source component from above into here")]
    public AudioSource Audio_Source;
    public float walkVolume = 1.0f;


    // Start is called before the first frame update
    void Start()
    {
        Audio_Source = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Playfootsteps()
    {
        Audio_Source.pitch = Random.Range(0.8f, 1.2f);
        Audio_Source.volume = walkVolume;
        Audio_Source.PlayOneShot(Footstep_SFX);
    }

    public void PlayJumpSound()
    {
        Audio_Source.pitch = Random.Range(0.8f, 1.2f);
        Audio_Source.PlayOneShot(Jump_SFX);
    }
}
