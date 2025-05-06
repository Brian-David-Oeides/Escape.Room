using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDialUI : MonoBehaviour
{
    #region Variables

    [Header("UI References")]
    public TextMeshProUGUI dialNumberText;
    public Color defaultColor = Color.white;
    public Color highlightColor = Color.green;
    public float highlightDuration = 0.5f;
    
    [Header("Canvas Reference")]
    public Canvas dialCanvas;

    [Header("Safe Dial Reference")]
    public SafeDial safeDial;

    private float highlightTimer = 0f;
    private bool isHighlighted = false;

    [Header("Pop Effect Settings")]
    public float popScaleMultiplier = 1.2f;
    public float popDuration = 0.2f;

    private Vector3 originalScale;
    private bool isPopping = false;

    #endregion

    #region Unity Events

    private void Start()
    {
        if (dialCanvas != null)
        {
            dialCanvas.enabled = false; // hide at start
        }

        if (safeDial != null)
        {
            safeDial.OnDialNumberChanged += UpdateDialDisplay;
        }
        else
        {
            Debug.LogWarning("SafeDial reference is missing on SafeDialUI.");
        }

        if (dialNumberText != null)
        {
            dialNumberText.color = defaultColor;
            originalScale = dialNumberText.transform.localScale;
        }
    }

    private void Update()
    {
        if (isHighlighted)
        {
            highlightTimer -= Time.deltaTime;
            if (highlightTimer <= 0f)
            {
                ResetDialColor();
            }
        }
    }

    private void OnDestroy()
    {
        if (safeDial != null)
        {
            safeDial.OnDialNumberChanged -= UpdateDialDisplay;
        }
    }

    #endregion

    #region Custom Methods

    private void UpdateDialDisplay(int number)
    {
        if (dialNumberText != null)
        {
            dialNumberText.text = number.ToString("D2"); // 2 digits

            // check if player is close to correct number
            if (!safeDial.IsUnlocked && safeDial.CurrentCombinationIndex < safeDial.CorrectCombination.Length)
            {
                int targetNumber = safeDial.CorrectCombination[safeDial.CurrentCombinationIndex];
                if (Mathf.Abs(number - targetNumber) <= safeDial.NumberTolerance)
                {
                    TriggerHighlight();
                }
            }
        }
    }

    private void TriggerHighlight()
    {
        if (dialNumberText != null)
        {
            dialNumberText.color = highlightColor;
            highlightTimer = highlightDuration;
            isHighlighted = true;

            if (!isPopping)
                StartCoroutine(PopEffect());
        }
    }

    private IEnumerator PopEffect()
    {
        isPopping = true;

        // scale up immediately
        dialNumberText.transform.localScale = originalScale * popScaleMultiplier;

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            dialNumberText.transform.localScale = Vector3.Lerp(dialNumberText.transform.localScale, originalScale, elapsed / popDuration);
            yield return null;
        }

        // snap back exactly
        dialNumberText.transform.localScale = originalScale;

        isPopping = false;
    }


    private void ResetDialColor()
    {
        if (dialNumberText != null)
        {
            dialNumberText.color = defaultColor;
            isHighlighted = false;
        }
    }

    public void OnDialGrabbed()
    {
        if (dialCanvas != null)
        {
            dialCanvas.enabled = true; // show canvas
        }
    }

    public void OnDialReleased()
    {
        if (dialCanvas != null)
        {
            dialCanvas.enabled = false; // hide canvas
        }
    }

    #endregion
}