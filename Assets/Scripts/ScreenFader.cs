using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;

    void Awake()
    {
        // Set screen fully black before anything renders
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
        }
    }

    public void FadeIn(float duration = 1f)
    {
        StartCoroutine(Fade(0, 1, duration));
    }

    public void FadeOut(float duration = 1f)
    {
        StartCoroutine(Fade(1, 0, duration));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        Debug.Log($"Starting fade from {from} to {to} over {duration} seconds.");

        float elapsed = 0f;
        while (elapsed < duration)
        {
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        fadeCanvasGroup.alpha = to;

        Debug.Log($"Finished fade to {to}");
    }
}