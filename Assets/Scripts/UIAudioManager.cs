using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class UISound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

public class UIAudioManager : MonoBehaviour
{
    private static UIAudioManager _instance;
    public static UIAudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIAudioManager>();
            }
            return _instance;
        }
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("UI Sounds")]
    public UISound menuOpenSound;
    public UISound menuCloseSound;
    public UISound buttonHoverSound;
    public UISound buttonClickSound;
    public UISound confirmSound;
    public UISound cancelSound;
    public UISound errorSound;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Create audio source if not assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    // Public methods for playing specific sounds
    public void PlayMenuOpen()
    {
        PlaySound(menuOpenSound);
    }

    public void PlayMenuClose()
    {
        PlaySound(menuCloseSound);
    }

    public void PlayButtonHover()
    {
        PlaySound(buttonHoverSound);
    }

    public void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
    }

    public void PlayConfirm()
    {
        PlaySound(confirmSound);
    }

    public void PlayCancel()
    {
        PlaySound(cancelSound);
    }

    public void PlayError()
    {
        PlaySound(errorSound);
    }

    // Generic play sound method
    public void PlaySound(UISound sound)
    {
        if (sound?.clip != null && audioSource != null)
        {
            audioSource.pitch = sound.pitch;
            audioSource.PlayOneShot(sound.clip, sound.volume);
        }
    }

    // Play sound by name (useful for UnityEvents)
    public void PlaySoundByName(string soundName)
    {
        UISound sound = null;

        switch (soundName.ToLower())
        {
            case "menuopen": sound = menuOpenSound; break;
            case "menuclose": sound = menuCloseSound; break;
            case "hover": sound = buttonHoverSound; break;
            case "click": sound = buttonClickSound; break;
            case "confirm": sound = confirmSound; break;
            case "cancel": sound = cancelSound; break;
            case "error": sound = errorSound; break;
        }

        if (sound != null)
            PlaySound(sound);
    }
}