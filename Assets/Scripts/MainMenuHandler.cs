using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class MainMenuHandler : MonoBehaviour
{
    public GameObject xrOrigin;
    public GameObject mainMenuUI;
    public ScreenFader screenFader;
    public Transform mainMenuSpawnPoint; // refreence for the MainMenuPosition 


    void Start()
    {
        // run menu logic if in the actual Main Menu scene
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            if (xrOrigin != null)
            {
                xrOrigin.transform.position = mainMenuSpawnPoint.position;
                xrOrigin.transform.rotation = mainMenuSpawnPoint.rotation;
                RotateOriginToFace(mainMenuUI.transform.position);
                LockPlayerMovement();
            }

            if (mainMenuUI != null)
            {
                mainMenuUI.SetActive(true);
            }

            //fade in from black
            if (screenFader != null)
            {
                screenFader.FadeOut(1f);
            }
        }
        else
        {
            // in the gameplay scene (i.e., "TheBoilerDemo")
            // hide the menu UI 
            if (mainMenuUI != null)
            {
                mainMenuUI.SetActive(false);
            }

            //fade in from black on restart
            if (screenFader != null && GameMode.startFromMenu == false)
            {
                screenFader.FadeOut(1f);
            }

            // disable this script entirely (no Update() or coroutine logic)
            this.enabled = false;
        }
    }

    public void StartGame()
    {
        var routine = LoadGameScene();
        if (routine != null)
        {
            StartCoroutine(routine);
        }
        else
        {
            Debug.LogWarning("LoadGameScene coroutine is null. Scene not loaded.");
        }
    }

    private IEnumerator LoadGameScene()
    {
        if (screenFader != null)
        {
            screenFader.FadeIn(1f);
            yield return new WaitForSeconds(1f);
        }
        else
        {
            Debug.LogWarning("ScreenFader is not assigned.");
        }

        GameMode.startFromMenu = false;
        SceneManager.LoadScene("TheBoilerDemo");
    }

    public void ExitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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

