using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip loadingMusic;

    [Header("Volume Settings")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private float musicVolume = 1f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;

    private Coroutine currentFadeCoroutine;
    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    protected override void Awake()
    {
        base.Awake(); // THIS IS CRITICAL - calls MonoSingleton's Awake first
        GameLog.Log("AudioManager Awake called");
    }

    public override void Init()
    {
        if (isInitialized) return; // Prevent double initialization

        DontDestroyOnLoad(gameObject);
        InitializeAudioSource();
        isInitialized = true;
        GameLog.Log("AudioManager initialized");
    }

    private void InitializeAudioSource()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
            {
                GameLog.LogWarning("No AudioSource found on AudioManager, creating one...");
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }
        }

        // Apply initial volume
        UpdateMusicSourceVolume();
    }

    #region Volume Control Methods (for SettingsManager)

    /// <summary>
    /// Set master volume (affects all audio)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateMusicSourceVolume();
        GameLog.Log($"[AudioManager] Master volume set to: {masterVolume}");
    }

    /// <summary>
    /// Set SFX volume (for sound effects - implement when SFX system is added)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        // TODO: Apply to SFX AudioSource when implemented
        GameLog.Log($"[AudioManager] SFX volume set to: {sfxVolume}");
    }

    /// <summary>
    /// Set music volume
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateMusicSourceVolume();
        GameLog.Log($"[AudioManager] Music volume set to: {musicVolume}");
    }

    /// <summary>
    /// Update the music source volume based on master and music volume settings
    /// </summary>
    private void UpdateMusicSourceVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = masterVolume * musicVolume;
        }
    }

    public float GetMasterVolume() => masterVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetMusicVolume() => musicVolume;

    #endregion

    /// <summary>
    /// Plays menu music with optional fade-in
    /// </summary>
    public void PlayMenuMusic(bool fadeIn = false)
    {
        GameLog.Log($"PlayMenuMusic called - fadeIn: {fadeIn}, isInitialized: {isInitialized}, musicSource exists: {musicSource != null}, menuMusic exists: {menuMusic != null}");

        if (fadeIn)
            PlayMusicWithFadeIn(menuMusic, fadeDuration);
        else
            PlayMusic(menuMusic);
    }

    /// <summary>
    /// Plays gameplay music with optional fade-in
    /// </summary>
    public void PlayGameplayMusic(bool fadeIn = true)
    {
        if (fadeIn)
            PlayMusicWithFadeIn(gameplayMusic, fadeDuration);
        else
            PlayMusic(gameplayMusic);
    }

    /// <summary>
    /// Plays loading music with optional fade-in
    /// </summary>
    public void PlayLoadingMusic(bool fadeIn = true)
    {
        GameLog.Log($"PlayLoadingMusic called - fadeIn: {fadeIn}, loadingMusic exists: {loadingMusic != null}");

        if (fadeIn)
            PlayMusicWithFadeIn(loadingMusic, fadeDuration);
        else
            PlayMusic(loadingMusic);
    }

    /// <summary>
    /// Fades out current music over specified duration
    /// </summary>
    public IEnumerator FadeOutMusic(float duration = -1)
    {
        if (duration < 0) duration = fadeDuration;

        GameLog.Log($"FadeOutMusic called - duration: {duration}");

        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeOutCoroutine(duration));
        yield return currentFadeCoroutine;
    }

    /// <summary>
    /// Stops music immediately without fade
    /// </summary>
    public void StopMusic()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
            UpdateMusicSourceVolume();
        }
    }

    /// <summary>
    /// Sets the fade duration for future fades
    /// </summary>
    public void SetFadeDuration(float duration)
    {
        fadeDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// Gets the current music volume
    /// </summary>
    public float GetVolume()
    {
        return musicSource != null ? musicSource.volume : 0f;
    }

    /// <summary>
    /// Sets the music volume (legacy method - use SetMusicVolume instead)
    /// </summary>
    public void SetVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// Checks if music is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return musicSource != null && musicSource.isPlaying;
    }

    // Private helper methods
    private void PlayMusic(AudioClip clip)
    {
        GameLog.Log($"PlayMusic called - clip: {clip?.name}, musicSource: {musicSource != null}");

        if (musicSource == null || clip == null)
        {
            GameLog.LogWarning($"Cannot play music - AudioSource={musicSource != null}, clip={clip != null}");
            return;
        }

        // Stop any ongoing fade
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = null;
        }

        if (musicSource.clip != clip)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            UpdateMusicSourceVolume();
            musicSource.Play();
            GameLog.Log($"Playing music: {clip.name}, isPlaying: {musicSource.isPlaying}");
        }
        else if (!musicSource.isPlaying)
        {
            UpdateMusicSourceVolume();
            musicSource.Play();
            GameLog.Log($"Resuming music: {clip.name}, isPlaying: {musicSource.isPlaying}");
        }
    }

    private void PlayMusicWithFadeIn(AudioClip clip, float duration)
    {
        if (musicSource == null || clip == null)
        {
            GameLog.LogWarning("Cannot play music - AudioSource or clip is null");
            return;
        }

        GameLog.Log($"PlayMusicWithFadeIn called - clip: {clip.name}, duration: {duration}");

        // Stop any ongoing fade
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        musicSource.clip = clip;
        musicSource.loop = true;

        currentFadeCoroutine = StartCoroutine(FadeInCoroutine(duration));
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        if (musicSource == null || !musicSource.isPlaying)
        {
            yield break;
        }

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        GameLog.Log($"Fading out audio over {duration} seconds");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        GameLog.Log("Audio fade out complete");

        currentFadeCoroutine = null;
    }

    private IEnumerator FadeInCoroutine(float duration)
    {
        if (musicSource == null)
        {
            yield break;
        }

        musicSource.volume = 0f;

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }

        float elapsed = 0f;
        float targetVolume = masterVolume * musicVolume;

        GameLog.Log($"Fading in audio over {duration} seconds to volume {targetVolume}");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        GameLog.Log("Audio fade in complete");

        currentFadeCoroutine = null;
    }
}