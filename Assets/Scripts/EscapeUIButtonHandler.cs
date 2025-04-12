using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeUIButtonHandler : MonoBehaviour
{
    public GameObject xrOrigin;
    public Transform mainMenuPosition;
    public GameObject escapedUI;
    public GameObject mainMenuUI;
    public ScreenFader screenFader;

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(BackToMenu());
    }

    private IEnumerator BackToMenu()
    {
        screenFader.FadeIn(1f);
        yield return new WaitForSeconds(1f);

        xrOrigin.transform.position = mainMenuPosition.position;
        xrOrigin.transform.rotation = mainMenuPosition.rotation;

        // Align player to face the UI
        RotateOriginToFace(mainMenuUI.transform.position);

        escapedUI.SetActive(false);
        mainMenuUI.SetActive(true);

        // short wait before fade in again
        yield return new WaitForSeconds(0.1f);


        screenFader.FadeOut(1f);
        yield return new WaitForSeconds(1f);

        // Now reload the scene to reset everything to default
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private void RotateOriginToFace(Vector3 targetPosition)
    {
        Camera camera = xrOrigin.GetComponentInChildren<Camera>();
        if (camera == null) return;

        Vector3 headsetForward = camera.transform.forward;
        headsetForward.y = 0;

        Vector3 directionToUI = (targetPosition - camera.transform.position);
        directionToUI.y = 0;

        float angle = Vector3.SignedAngle(headsetForward, directionToUI, Vector3.up);
        xrOrigin.transform.Rotate(0, angle, 0);
    }

}
