using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;

public class UIHoverButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Components (Auto-detected if empty)")]
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button button;
    [SerializeField] private XRSimpleInteractable xrInteractable;

    [Header("Text Color Settings")]
    [SerializeField] private bool changeTextColor = true;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color hoverTextColor = Color.yellow;

    [Header("Scale Settings")]
    [SerializeField] private bool scaleOnHover = true;
    [SerializeField] private float normalScale = 1.0f;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleSpeed = 10f;

    [Header("Advanced Options")]
    [SerializeField] private bool affectWholeButton = false; // Scale entire button or just text
    [SerializeField] private bool useUnscaledTime = true; // For paused menus
    [SerializeField] private AnimationCurve hoverCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Transform targetTransform;
    private Vector3 originalScale;
    private Color originalColor;
    private Coroutine hoverCoroutine;
    private bool isHovering = false;

    private void Awake()
    {
        // Auto-detect components if not assigned
        if (button == null)
            button = GetComponent<Button>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (xrInteractable == null)
            xrInteractable = GetComponent<XRSimpleInteractable>();

        // Determine what to scale
        if (affectWholeButton)
            targetTransform = transform;
        else if (buttonText != null)
            targetTransform = buttonText.transform;
        else
            targetTransform = transform;

        // Store original values
        if (targetTransform != null)
            originalScale = targetTransform.localScale;

        if (buttonText != null && changeTextColor)
        {
            originalColor = buttonText.color;
            normalTextColor = originalColor; // Use current color as normal if not set
        }

        // Set up XR hover events
        if (xrInteractable != null)
        {
            xrInteractable.hoverEntered.AddListener(OnXRHoverEnter);
            xrInteractable.hoverExited.AddListener(OnXRHoverExit);
        }
    }

    private void OnDestroy()
    {
        if (xrInteractable != null)
        {
            xrInteractable.hoverEntered.RemoveListener(OnXRHoverEnter);
            xrInteractable.hoverExited.RemoveListener(OnXRHoverExit);
        }

        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
    }

    private void OnEnable()
    {
        // Reset to normal state when enabled
        ResetToNormal();
    }

    private void OnDisable()
    {
        // Reset when disabled
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }
        ResetToNormal();
        isHovering = false;
    }

    // Mouse/Touch hover events
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        StartHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EndHover();
    }

    // XR hover events
    private void OnXRHoverEnter(HoverEnterEventArgs args)
    {
        if (button != null && !button.interactable) return;
        StartHover();
    }

    private void OnXRHoverExit(HoverExitEventArgs args)
    {
        EndHover();
    }

    private void StartHover()
    {
        if (isHovering) return;
        isHovering = true;

        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);

        hoverCoroutine = StartCoroutine(AnimateHover(true));
    }

    private void EndHover()
    {
        if (!isHovering) return;
        isHovering = false;

        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);

        hoverCoroutine = StartCoroutine(AnimateHover(false));
    }

    private IEnumerator AnimateHover(bool hovering)
    {
        float elapsedTime = 0f;
        float duration = 1f / scaleSpeed;

        Vector3 startScale = targetTransform.localScale;
        Vector3 targetScale = originalScale * (hovering ? hoverScale : normalScale);

        Color startColor = buttonText != null ? buttonText.color : normalTextColor;
        Color targetColor = hovering ? hoverTextColor : normalTextColor;

        while (elapsedTime < duration)
        {
            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = hoverCurve.Evaluate(elapsedTime / duration);

            // Animate scale
            if (scaleOnHover && targetTransform != null)
            {
                targetTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            }

            // Animate color
            if (changeTextColor && buttonText != null)
            {
                buttonText.color = Color.Lerp(startColor, targetColor, t);
            }

            yield return null;
        }

        // Ensure final values are set
        if (scaleOnHover && targetTransform != null)
            targetTransform.localScale = targetScale;

        if (changeTextColor && buttonText != null)
            buttonText.color = targetColor;

        hoverCoroutine = null;
    }

    private void ResetToNormal()
    {
        if (targetTransform != null)
            targetTransform.localScale = originalScale * normalScale;

        if (buttonText != null && changeTextColor)
            buttonText.color = normalTextColor;
    }

    // Public method to force reset
    public void ForceReset()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }
        isHovering = false;
        ResetToNormal();
    }

    // Public method to update colors at runtime
    public void SetColors(Color normal, Color hover)
    {
        normalTextColor = normal;
        hoverTextColor = hover;
        if (!isHovering && buttonText != null)
            buttonText.color = normal;
    }

    // Public method to update scale at runtime
    public void SetScale(float normal, float hover)
    {
        normalScale = normal;
        hoverScale = hover;
        if (!isHovering)
            ResetToNormal();
    }
}