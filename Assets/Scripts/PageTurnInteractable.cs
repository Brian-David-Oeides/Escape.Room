using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class PageTurnInteractable : MonoBehaviour
{
    [Header("Page References")]
    public Transform rightPage; // R_Page child object
    public Transform leftPage;  // L_Page child object

    [Header("Page Turn Settings")]
    public float pageRotationAngle = 180f;
    public float pageFlipSpeed = 2.0f;
    public AnimationCurve pageFlipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio Settings")]
    public AudioClip pageFlipSound;
    [Range(0f, 1f)]
    public float pageAudioVolume = 0.7f;

    [Header("Page Interaction Control")]
    public bool enablePageTurning = false; // Only allow page turning when journal is positioned

    private AudioSource audioSource;

    // Page state tracking
    private bool rightPageFlipped = false;
    private bool leftPageFlipped = false;
    private bool isFlippingPage = false;

    private void Awake()
    {
        // Set up audio source
        SetupAudioSource();

        // Set up page interactions
        SetupPageInteractions();

        // Initialize collider states (all disabled until page turning is enabled)
        DisableAllPageColliders();
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.spatialBlend = 1.0f; // Full 3D
        audioSource.volume = pageAudioVolume;
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 5f;
    }

    private void SetupPageInteractions()
    {
        // Set up right page interaction
        if (rightPage != null)
        {
            SetupPageInteractable(rightPage.gameObject, true);
        }
        else
        {
            Debug.LogWarning("Right page (R_Page) not assigned!");
        }

        // Set up left page interaction
        if (leftPage != null)
        {
            SetupPageInteractable(leftPage.gameObject, false);
        }
        else
        {
            Debug.LogWarning("Left page (L_Page) not assigned!");
        }
    }

    private void SetupPageInteractable(GameObject pageObject, bool isRightPage)
    {
        Debug.Log($"Setting up page interactable for: {pageObject.name}");

        // Add XR Simple Interactable if not present
        XRSimpleInteractable interactable = pageObject.GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = pageObject.AddComponent<XRSimpleInteractable>();
            // Debug.Log($"Added XR Simple Interactable to {pageObject.name}");
        }

        // Add collider if not present
        Collider pageCollider = pageObject.GetComponent<Collider>();
        if (pageCollider == null)
        {
            BoxCollider boxCollider = pageObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = false; // Make it solid for ray interaction
            // Debug.Log($"Added Box Collider to {pageObject.name}");
        }

        // Check layer
        // Debug.Log($"Page {pageObject.name} is on layer: {LayerMask.LayerToName(pageObject.layer)}");

        // Subscribe to interaction events
        interactable.selectEntered.AddListener((args) => OnPageSelected(isRightPage));

        // Debug.Log($"Page interaction setup complete for {(isRightPage ? "right" : "left")} page");
    }

    public void OnPageSelected(bool isRightPage)
    {
        Debug.Log($"PAGE SELECTED CALLED! Right page: {isRightPage}, Enable page turning: {enablePageTurning}");

        // Only allow page turning when journal is positioned in front of user
        if (!enablePageTurning || isFlippingPage)
        {
            Debug.Log("Page turning disabled or already flipping");
            return;
        }

        // No need for blocking logic anymore - colliders handle this
        if (isRightPage)
        {
            FlipRightPage();
        }
        else
        {
            FlipLeftPage();
        }
    }

    private void FlipRightPage()
    {
        if (rightPage == null) return;

        Debug.Log($"FlipRightPage called - Current state: rightPageFlipped = {rightPageFlipped}");

        StartCoroutine(FlipPageCoroutine(rightPage, !rightPageFlipped));
        rightPageFlipped = !rightPageFlipped;

        // Update collider states after changing page state
        UpdatePageColliderStates();

        Debug.Log($"Flipping right page - now {(rightPageFlipped ? "flipped" : "unflipped")}");
    }

    private void FlipLeftPage()
    {
        if (leftPage == null) return;

        Debug.Log($"FlipLeftPage called - Current state: leftPageFlipped = {leftPageFlipped}");

        StartCoroutine(FlipPageCoroutine(leftPage, !leftPageFlipped));
        leftPageFlipped = !leftPageFlipped;

        // Update collider states after changing page state
        UpdatePageColliderStates();

        Debug.Log($"Flipping left page - now {(leftPageFlipped ? "flipped" : "unflipped")}");
    }

    private IEnumerator FlipPageCoroutine(Transform page, bool flipForward)
    {
        isFlippingPage = true;

        Debug.Log($"FlipPageCoroutine started - Page: {page.name}, flipForward: {flipForward}");

        // Play page flip sound
        PlayPageFlipSound();

        // Calculate rotation
        Vector3 startRotation = page.localEulerAngles;
        Vector3 targetRotation = startRotation;

        Debug.Log($"Start rotation: {startRotation}");

        // Determine correct rotation direction based on which page and current state
        float rotationAmount;

        if (page == rightPage)
        {
            // Right page should only turn towards the left (negative Z rotation)
            rotationAmount = flipForward ? -pageRotationAngle : pageRotationAngle;
            Debug.Log($"Right page - flipForward: {flipForward}, rotationAmount: {rotationAmount}");
        }
        else // left page
        {
            // Left page should only turn towards the right (positive Z rotation)  
            rotationAmount = flipForward ? pageRotationAngle : -pageRotationAngle;
            Debug.Log($"Left page - flipForward: {flipForward}, rotationAmount: {rotationAmount}");
        }

        targetRotation.z += rotationAmount;
        Debug.Log($"Target rotation: {targetRotation}");

        // Perform smooth rotation
        float elapsedTime = 0f;
        float duration = 1f / pageFlipSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Apply animation curve for smooth easing
            float curveProgress = pageFlipCurve.Evaluate(progress);

            // Interpolate rotation
            Vector3 currentRotation = Vector3.Lerp(startRotation, targetRotation, curveProgress);
            page.localEulerAngles = currentRotation;

            yield return null;
        }

        // Ensure final rotation is exact
        page.localEulerAngles = targetRotation;
        isFlippingPage = false;

        Debug.Log($"Page flip completed: {page.name} - isFlippingPage now: {isFlippingPage}");
    }

    private void PlayPageFlipSound()
    {
        if (pageFlipSound != null && audioSource != null)
        {
            audioSource.clip = pageFlipSound;
            audioSource.volume = pageAudioVolume;
            audioSource.Play();
        }
    }

    // Add these methods to manage collider states
    private void UpdatePageColliderStates()
    {
        // Enable/disable colliders based on page states
        if (rightPage != null)
        {
            Collider rightCollider = rightPage.GetComponent<Collider>();
            if (rightCollider != null)
            {
                // Right page collider is enabled if: no pages are flipped, OR right page is already flipped (can turn back)
                bool enableRightCollider = (!leftPageFlipped && !rightPageFlipped) || rightPageFlipped;
                rightCollider.enabled = enableRightCollider;
                Debug.Log($"Right page collider enabled: {enableRightCollider}");
            }
        }

        if (leftPage != null)
        {
            Collider leftCollider = leftPage.GetComponent<Collider>();
            if (leftCollider != null)
            {
                // Left page collider is enabled if: no pages are flipped, OR left page is already flipped (can turn back)
                bool enableLeftCollider = (!rightPageFlipped && !leftPageFlipped) || leftPageFlipped;
                leftCollider.enabled = enableLeftCollider;
                Debug.Log($"Left page collider enabled: {enableLeftCollider}");
            }
        }
    }

    private void DisableAllPageColliders()
    {
        if (rightPage != null)
        {
            Collider rightCollider = rightPage.GetComponent<Collider>();
            if (rightCollider != null) rightCollider.enabled = false;
        }

        if (leftPage != null)
        {
            Collider leftCollider = leftPage.GetComponent<Collider>();
            if (leftCollider != null) leftCollider.enabled = false;
        }
        Debug.Log("All page colliders disabled");
    }


    // Public methods for JournalPositioner to communicate state
    public void EnablePageTurning()
    {
        enablePageTurning = true;
        UpdatePageColliderStates(); // Enable appropriate colliders
        Debug.Log("Page turning enabled - journal is positioned for reading");
    }

    public void DisablePageTurning()
    {
        enablePageTurning = false;
        DisableAllPageColliders(); // Disable all page colliders
        Debug.Log("Page turning disabled - journal moved away");
    }

    // Reset pages to original state
    public void ResetPages()
    {
        if (rightPage != null)
        {
            rightPage.localEulerAngles = Vector3.zero;
            rightPageFlipped = false;
        }

        if (leftPage != null)
        {
            leftPage.localEulerAngles = Vector3.zero;
            leftPageFlipped = false;
        }

        Debug.Log("Pages reset to original positions");
    }
}
