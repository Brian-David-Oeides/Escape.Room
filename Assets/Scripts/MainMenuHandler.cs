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
    public Transform mainMenuSpawnPoint; // reference for the MainMenuPosition 


    void Start()
    {
        // run menu logic if in the actual Main Menu scene
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            if (xrOrigin != null)
            {
                xrOrigin.transform.position = mainMenuSpawnPoint.position; // spawn XR Origin at spawn point
                xrOrigin.transform.rotation = mainMenuSpawnPoint.rotation;
                RotateOriginToFace(mainMenuUI.transform.position); // face the main menu
                LockPlayerMovement(); // disable snapturn and locomotion
            }

            if (mainMenuUI != null)
            {
                mainMenuUI.SetActive(true); // set UI active
            }

            //fade in from black
            if (screenFader != null)
            {
                screenFader.FadeOut(1f); // fade out 
            }
        }
        else
        {
            // in the gameplay scene (i.e., "TheBoilerDemo")
            if (mainMenuUI != null)
            {
                mainMenuUI.SetActive(false); // hide the menu UI 
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
        var routine = LoadGameScene(); // assign coroutine to var
        if (routine != null) // check if var exists
        {
            StartCoroutine(routine); // run coroutine stored in var
        }
        else // or 
        {
            Debug.LogWarning("LoadGameScene coroutine is null. Scene not loaded."); // log warning
        }
    }

    private IEnumerator LoadGameScene()
    {
        if (screenFader != null) // check if screenfader exists
        {
            screenFader.FadeIn(1f); // fade in
            yield return new WaitForSeconds(1f); // delay 1 sec
        }
        else
        {
            Debug.LogWarning("ScreenFader is not assigned."); // if null log warning
        }

        GameMode.startFromMenu = false; // disable start from menu
        SceneManager.LoadScene("TheBoilerDemo"); //load the scene via string name
    }

    public void ExitGame()
    {
        Debug.Log("Quitting game..."); // log the event
        Application.Quit(); // close the application in build

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // close the application in Unity Editor
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

