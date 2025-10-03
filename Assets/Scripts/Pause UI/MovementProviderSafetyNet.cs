using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MovementProviderSafetyNet : MonoBehaviour
{
    private ActionBasedContinuousMoveProvider moveProvider;

    void Start()
    {
        moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
    }

    void Update()
    {
        // If paused, force disable the component
        if (GameManager.Instance != null &&
            GameManager.Instance.currentState == GameState.Paused &&
            moveProvider != null &&
            moveProvider.enabled)
        {
            moveProvider.enabled = false;
        }
    }
}
