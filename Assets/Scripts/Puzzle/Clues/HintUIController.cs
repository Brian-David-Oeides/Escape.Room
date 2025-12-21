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
    [Header("Positioning")]
    [Tooltip("Distance in front of camera to display hint")]
    public float distanceFromCamera = 1.5f;

    [Tooltip("Vertical offset from camera forward (positive = up)")]
    public float verticalOffset = -0.2f;

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
    private Transform cameraTransform;
    private Coroutine displayCoroutine;
    private bool isShowing = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        audioSource = GetComponent<AudioSource>();

        // Start invisible (but keep GameObject enabled)
        canvasGroup.alpha = 0f;
        isShowing = false;
        // Don't disable GameObject - just keep it invisible with alpha = 0
    }

    private void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        // Follow camera position when showing
        if (isShowing && cameraTransform != null)
        {
            UpdatePosition();
        }

        // Check for B button to dismiss
        if (isShowing && Input.GetKeyDown(KeyCode.B))
        {
            DismissHint();
        }

        // VR B button (secondary button on right controller)
        if (isShowing && UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand)
            .TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bButtonPressed))
        {
            if (bButtonPressed)
            {
                DismissHint();
            }
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
        // Position in front of camera
        UpdatePosition();

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

    /// <summary>
    /// Update position to stay in front of camera
    /// </summary>
    private void UpdatePosition()
    {
        if (cameraTransform == null) return;

        Vector3 forward = cameraTransform.forward;
        Vector3 up = cameraTransform.up;

        // Position in front and slightly down
        Vector3 targetPosition = cameraTransform.position
            + forward * distanceFromCamera
            + up * verticalOffset;

        transform.position = targetPosition;

        // Face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
    }
}
