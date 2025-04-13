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
        StartCoroutine(RestartGameRoutine());
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(BackToMenu());
    }

    private IEnumerator RestartGameRoutine()
    {
        if (screenFader != null)
        {
            screenFader.FadeIn(1f); // fade to black
            yield return new WaitForSeconds(1f);
        }

        GameMode.startFromMenu = false; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    private IEnumerator BackToMenu()
    {
        if (screenFader != null)
        {
            screenFader.FadeIn(1f);
            yield return new WaitForSeconds(1f);
        }

        GameMode.startFromMenu = true; // show menu UI

        SceneManager.LoadScene("MainMenuScene");
    }

}
