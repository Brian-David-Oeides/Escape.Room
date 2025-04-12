using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStateController : MonoBehaviour
{
    public GameObject mainMenuUI;
    public GameObject gameplayLogic;

    void Start()
    {
        if (GameMode.startFromMenu)
        {
            mainMenuUI.SetActive(true);
            gameplayLogic.SetActive(false);
        }
        else
        {
            mainMenuUI.SetActive(false);
            gameplayLogic.SetActive(true);

        }
    }
}
