using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class CutSceneManager : MonoBehaviour
{
    [Header("Cutscene References")]
    public Transform exitTarget;
    public float moveDuration = 2f;

    [Header("UI References")]
    public GameObject EscapedUI;
    public TMP_Text timeText;

    private ScreenFader _screenFader;
    private CharacterController characterController;

    private void Start()
    {
        _screenFader = FindObjectOfType<ScreenFader>();

        // Get XR Origin from GameManager
        if (GameManager.Instance?.xrOrigin != null)
        {
            characterController = GameManager.Instance.xrOrigin.GetComponent<CharacterController>();
        }

        // Hide escaped UI initially
        if (EscapedUI != null)
        {
            EscapedUI.SetActive(false);
        }
    }

    public void TriggerCutscene()
    {
        StartCoroutine(PlayCutscene());
    }

    private IEnumerator PlayCutscene()
    {
        // Get XR Origin from GameManager
        GameObject xrOrigin = GameManager.Instance?.xrOrigin;
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin not found in GameManager!");
            yield break;
        }

        // Use GameManager's screen fader if available, otherwise use local one
        ScreenFader screenFader = GameManager.Instance?.screenFader ?? _screenFader;

        // Fade to black
        if (screenFader != null)
        {
            screenFader.FadeIn(1f);
            yield return new WaitForSeconds(1.2f);
        }

        // Move the player to the exit position
        Vector3 startPos = xrOrigin.transform.position;
        Vector3 endPos = exitTarget.position;

        float elapsed = 0;
        while (elapsed < moveDuration)
        {
            xrOrigin.transform.position = Vector3.Lerp(startPos, endPos, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        xrOrigin.transform.position = endPos;

        // Rotate player 90° to the left
        xrOrigin.transform.Rotate(0, -90f, 0);

        // Fade back in
        if (screenFader != null)
        {
            screenFader.FadeOut(1f);
            yield return new WaitForSeconds(1f);
        }

        // Stop timer
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StopTimer();

            // Update UI text
            if (timeText != null)
            {
                timeText.text = "Time: " + GameTimer.Instance.GetFormattedTime();
            }
        }

        Debug.Log("Cutscene completed - Now triggering escaped state");

        // NOW tell GameManager we've escaped (after the cutscene completes)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerEscaped();
        }

        // The EscapedUI will be shown automatically by the EscapeUIButtonHandler
        // when it receives the GameState.Escaped event from GameManager
    }
}