using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRLocomotionValidator : MonoBehaviour
{
    private ActionBasedContinuousMoveProvider _continuousMove;
    private ActionBasedSnapTurnProvider _snapTurn;
    private ActionBasedContinuousTurnProvider _continuousTurn;

    private bool _lastKnownMoveState;
    private bool _lastKnownSnapState;
    private bool _lastKnownTurnState;

    private void Awake()
    {
        _continuousMove = GetComponent<ActionBasedContinuousMoveProvider>();
        _snapTurn = GetComponent<ActionBasedSnapTurnProvider>();
        _continuousTurn = GetComponent<ActionBasedContinuousTurnProvider>();
    }

    private void LateUpdate()
    {
        // Only validate when game is playing (not paused)
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Playing)
        {
            // Check if components have been incorrectly disabled
            ValidateComponent(_continuousMove, ref _lastKnownMoveState, "ContinuousMove");
            ValidateComponent(_snapTurn, ref _lastKnownSnapState, "SnapTurn");
            ValidateComponent(_continuousTurn, ref _lastKnownTurnState, "ContinuousTurn");
        }
    }

    private void ValidateComponent<T>(T component, ref bool lastKnownState, string componentName) where T : MonoBehaviour
    {
        if (component != null)
        {
            bool currentState = component.enabled;

            // If state changed unexpectedly during gameplay
            if (currentState != lastKnownState && !lastKnownState)
            {
                Debug.LogWarning($"{componentName} was disabled during gameplay - re-enabling");
                component.enabled = true;
            }

            lastKnownState = component.enabled;
        }
    }

    public void ResetValidation()
    {
        // Call this when pausing to reset the validation state
        _lastKnownMoveState = false;
        _lastKnownSnapState = false;
        _lastKnownTurnState = false;
    }
}
