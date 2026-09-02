using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Dedicated hint display that follows player and auto-dismisses
/// </summary>
/// 

[RequireComponent(typeof(CanvasGroup))]

public class HintUIController : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("How long hint stays visible before auto-hiding (seconds)")]
    public float displayDuration = 5f;

    [Tooltip("Fade in/out duration")]
    public float fadeDuration = 0.5f;

    [Header("UI References")]
    public TextMeshProUGUI hintText;

    [Header("Audio")]
    public AudioClip hintAppearSound;
    public AudioClip hintDismissSound;

    private CanvasGroup canvasGroup;
    private AudioSource audioSource;
    private Coroutine displayCoroutine;
    private bool isShowing = false;

    private bool bButtonWasPressed = false; // Track previous frame state

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        audioSource = GetComponent<AudioSource>();

        // Start invisible (but keep GameObject enabled)
        canvasGroup.alpha = 0f;
        isShowing = false;
        // Don't disable GameObject - just keep it invisible with alpha = 0
    }

    private void Update()
    {
        // Check for B button to dismiss (keyboard)
        if (isShowing && Input.GetKeyDown(KeyCode.B))
        {
            DismissHint();
        }

        // VR B button (secondary button on right controller) - FIXED VERSION
        if (isShowing && UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand)
            .TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bButtonPressed))
        {
            // Only trigger on button DOWN (not held)
            if (bButtonPressed && !bButtonWasPressed)
            {
                DismissHint();
            }
            bButtonWasPressed = bButtonPressed;
        }
        else
        {
            bButtonWasPressed = false;
        }
    }

    /// <summary>
    /// Show hint message
    /// </summary>
    public void ShowHint(string message)
    {
        // Stop any existing display
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        // Set message
        if (hintText != null)
        {
            hintText.text = message;
        }

        // CRITICAL FIX: Enable GameObject BEFORE starting coroutine
        gameObject.SetActive(true);
        isShowing = true;

        // Start display sequence
        displayCoroutine = StartCoroutine(DisplayHintSequence());
    }

    private IEnumerator DisplayHintSequence()
    {
        // Play appear sound
        if (hintAppearSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hintAppearSound);
        }

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // Hide
        isShowing = false;
        gameObject.SetActive(false);

        displayCoroutine = null;
    }

    /// <summary>
    /// Manually dismiss hint (called by B button)
    /// </summary>
    public void DismissHint()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        StartCoroutine(FadeOutAndHide());
    }

    private IEnumerator FadeOutAndHide()
    {
        // Play dismiss sound
        if (hintDismissSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hintDismissSound);
        }

        // Fade out
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = startAlpha * (1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        isShowing = false;
        gameObject.SetActive(false);
    }
}
