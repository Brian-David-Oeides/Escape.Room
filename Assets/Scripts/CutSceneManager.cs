using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class CutSceneManager : MonoBehaviour
{
    public XRBaseInteractor doorInteractor;
    public Transform exitTarget;
    public GameObject xrOrigin;
    public float moveDuration = 2f;

    private ScreenFader _screenFader;
    public GameObject EscapedUI;
    public TMP_Text timeText; 

    private CharacterController characterController;

    private void Start()
    {
        _screenFader = FindObjectOfType<ScreenFader>();
        characterController = xrOrigin.GetComponent<CharacterController>();
    }

    public void TriggerCutscene()
    {
        StartCoroutine(PlayCutscene());
    }

    private IEnumerator PlayCutscene()
    {
        // Fade to black
        _screenFader.FadeIn(1f);
        yield return new WaitForSeconds(1.2f);

        // Optionally lock player input here (disable locomotion scripts, etc.)
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

        // Lock Movement
        LockPlayerMovement();

        // Fade back in
        _screenFader.FadeOut(1f);
        yield return new WaitForSeconds(1f);

        Debug.Log("Showing Escaped UI now!");

        // Now trigger Game Over UI or next logic
        // Stop timer
        GameTimer.Instance.StopTimer();

        // Update UI text
        timeText.text = "Time: " + GameTimer.Instance.GetFormattedTime();

        // Show Game Over UI
        EscapedUI.SetActive(true);
    }

    private void LockPlayerMovement()
    {
        var teleport = xrOrigin.GetComponent<TeleportationProvider>();
        if (teleport != null) teleport.enabled = false;

        var continuousMove = xrOrigin.GetComponent<ContinuousMoveProviderBase>();
        if (continuousMove != null) continuousMove.enabled = false;

        var snapTurn = xrOrigin.GetComponent<SnapTurnProviderBase>();
        if (snapTurn != null) snapTurn.enabled = false;

        var continuousTurn = xrOrigin.GetComponent<ContinuousTurnProviderBase>();
        if (continuousTurn != null) continuousTurn.enabled = false;
    }
}
