using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class MainMenuHandler : MonoBehaviour
{
    public GameObject xrOrigin;
    public Transform startPosition;
    public GameObject mainMenuUI;
    public ScreenFader screenFader;
    public Transform mainMenuSpawnPoint; // refreence for the MainMenuPosition 


    void Start()
    {
        xrOrigin.transform.position = mainMenuSpawnPoint.position;
        xrOrigin.transform.rotation = mainMenuSpawnPoint.rotation;
        RotateOriginToFace(mainMenuUI.transform.position);
        LockPlayerMovement();
    }

    public void StartGame()
    {
        StartCoroutine(TransitionToStart());
    }

    public void ExitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator TransitionToStart()
    {
        // Fade to black
        screenFader.FadeIn(1f);
        yield return new WaitForSeconds(1f);

        // Move player
        xrOrigin.transform.position = startPosition.position;
        xrOrigin.transform.rotation = startPosition.rotation;

        // Deactivate main menu UI
        mainMenuUI.SetActive(false);

        // Unlock movement now that gameplay begins
        UnlockPlayerMovement();

        // Fade back in
        screenFader.FadeOut(1f);
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

    private void UnlockPlayerMovement()
    {
        var teleport = xrOrigin.GetComponent<TeleportationProvider>();
        if (teleport != null) teleport.enabled = true;

        var continuousMove = xrOrigin.GetComponent<ContinuousMoveProviderBase>();
        if (continuousMove != null) continuousMove.enabled = true;

        var snapTurn = xrOrigin.GetComponent<SnapTurnProviderBase>();
        if (snapTurn != null) snapTurn.enabled = true;

        var continuousTurn = xrOrigin.GetComponent<ContinuousTurnProviderBase>();
        if (continuousTurn != null) continuousTurn.enabled = true;
    }
    private void RotateOriginToFace(Vector3 targetPosition)
    {
        Camera camera = xrOrigin.GetComponentInChildren<Camera>();
        if (camera == null) return;

        Vector3 headsetForward = camera.transform.forward;
        headsetForward.y = 0;

        Vector3 directionToUI = targetPosition - camera.transform.position;
        directionToUI.y = 0;

        float angle = Vector3.SignedAngle(headsetForward, directionToUI, Vector3.up);
        xrOrigin.transform.Rotate(0, angle, 0);
    }
}

