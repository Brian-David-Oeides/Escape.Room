using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerController : MonoSingleton<PlayerController>
{
    [Header("XR References")]
    [SerializeField] private GameObject xrOrigin;

    private bool isInitialized = false;

    public GameObject XROrigin => xrOrigin;
    public bool IsInitialized => isInitialized;

    protected override void Awake()
    {
        base.Awake(); // THIS IS CRITICAL - calls MonoSingleton's Awake first
        Debug.Log("PlayerController Awake called");
    }

    public override void Init()
    {
        if (isInitialized) return; // Prevent double initialization

        DontDestroyOnLoad(gameObject);
        isInitialized = true;
        Debug.Log("PlayerController initialized");
    }

    /// <summary>
    /// Updates the XR Origin reference (call after scene loads)
    /// </summary>
    public void UpdateXROriginReference()
    {
        if (xrOrigin == null)
        {
            XROrigin xrOriginComponent = FindObjectOfType<XROrigin>();
            if (xrOriginComponent != null)
            {
                xrOrigin = xrOriginComponent.gameObject;
                Debug.Log("XR Origin reference updated");
            }
            else
            {
                Debug.LogWarning("Could not find XR Origin in scene");
            }
        }
    }

    /// <summary>
    /// Positions the player at the designated spawn point
    /// </summary>
    public void PositionAtSpawnPoint()
    {
        PlayerSpawnHandler spawnHandler = FindObjectOfType<PlayerSpawnHandler>();
        if (spawnHandler != null && xrOrigin != null)
        {
            if (spawnHandler.xrOrigin != null && spawnHandler.startPosition != null)
            {
                xrOrigin.transform.position = spawnHandler.startPosition.position;
                xrOrigin.transform.rotation = spawnHandler.startPosition.rotation;
                Debug.Log("Player positioned at spawn point");
            }
            else
            {
                Debug.LogWarning("PlayerSpawnHandler found but xrOrigin or startPosition is null");
            }
        }
        else
        {
            Debug.LogWarning("PlayerSpawnHandler not found in scene or xrOrigin is null");
        }
    }

    #region Movement Settings (for SettingsManager)

    /// <summary>
    /// Set the player's movement speed
    /// </summary>
    public void SetMovementSpeed(float speed)
    {
        if (xrOrigin == null)
        {
            Debug.LogWarning("[PlayerController] Cannot set movement speed - XR Origin is null");
            return;
        }

        var continuousMove = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
        if (continuousMove != null)
        {
            continuousMove.moveSpeed = speed;
            Debug.Log($"[PlayerController] Movement speed set to: {speed}");
        }
        else
        {
            Debug.LogWarning("[PlayerController] ActionBasedContinuousMoveProvider not found");
        }
    }

    /// <summary>
    /// Set the snap turn angle
    /// </summary>
    public void SetSnapTurnAngle(float angle)
    {
        if (xrOrigin == null)
        {
            Debug.LogWarning("[PlayerController] Cannot set snap turn angle - XR Origin is null");
            return;
        }

        var snapTurn = xrOrigin.GetComponent<ActionBasedSnapTurnProvider>();
        if (snapTurn != null)
        {
            snapTurn.turnAmount = angle;
            Debug.Log($"[PlayerController] Snap turn angle set to: {angle}");
        }
        else
        {
            Debug.LogWarning("[PlayerController] ActionBasedSnapTurnProvider not found");
        }
    }

    /// <summary>
    /// Get the current movement speed
    /// </summary>
    public float GetMovementSpeed()
    {
        if (xrOrigin == null) return 0f;

        var continuousMove = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
        return continuousMove != null ? continuousMove.moveSpeed : 0f;
    }

    /// <summary>
    /// Get the current snap turn angle
    /// </summary>
    public float GetSnapTurnAngle()
    {
        if (xrOrigin == null) return 0f;

        var snapTurn = xrOrigin.GetComponent<ActionBasedSnapTurnProvider>();
        return snapTurn != null ? snapTurn.turnAmount : 0f;
    }

    #endregion

    /// <summary>
    /// Disables all player movement components
    /// </summary>
    public void DisableMovement()
    {
        if (xrOrigin == null)
        {
            Debug.LogWarning("Cannot disable movement - XR Origin is null");
            return;
        }

        StartCoroutine(DisableMovementSafely());
    }

    /// <summary>
    /// Enables all player movement components
    /// </summary>
    public void EnableMovement()
    {
        if (xrOrigin == null)
        {
            Debug.LogWarning("Cannot enable movement - XR Origin is null");
            return;
        }

        StartCoroutine(EnableMovementCoroutine());
    }

    /// <summary>
    /// Checks if the player can currently move
    /// </summary>
    public bool CanMove()
    {
        if (xrOrigin == null) return false;

        var continuousMove = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
        return continuousMove != null && continuousMove.enabled;
    }

    private IEnumerator DisableMovementSafely()
    {
        // Wait a frame to ensure any current input reads complete
        yield return null;

        // Disable locomotion components
        var teleport = xrOrigin.GetComponent<TeleportationProvider>();
        if (teleport != null) teleport.enabled = false;

        var continuousMove = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
        if (continuousMove != null)
        {
            continuousMove.enabled = false;
            // Force clear any cached input
            continuousMove.leftHandMoveAction.action?.Disable();
            continuousMove.rightHandMoveAction.action?.Disable();
            yield return null;
            continuousMove.leftHandMoveAction.action?.Enable();
            continuousMove.rightHandMoveAction.action?.Enable();
        }

        var snapTurn = xrOrigin.GetComponent<ActionBasedSnapTurnProvider>();
        if (snapTurn != null) snapTurn.enabled = false;

        var continuousTurn = xrOrigin.GetComponent<ActionBasedContinuousTurnProvider>();
        if (continuousTurn != null) continuousTurn.enabled = false;

        Debug.Log("Player movement disabled");
    }

    private IEnumerator EnableMovementCoroutine()
    {
        // Small delay to ensure state changes have propagated
        yield return new WaitForSeconds(0.2f);

        // Enable locomotion components with verification
        var teleport = xrOrigin.GetComponent<TeleportationProvider>();
        if (teleport != null)
        {
            teleport.enabled = true;
            Debug.Log($"Teleport enabled: {teleport.enabled}");
        }

        var continuousMove = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
        if (continuousMove != null)
        {
            // Clear and reset the component
            continuousMove.enabled = false;
            yield return null;
            continuousMove.enabled = true;
            Debug.Log($"Continuous move enabled: {continuousMove.enabled}");
        }

        var snapTurn = xrOrigin.GetComponent<ActionBasedSnapTurnProvider>();
        if (snapTurn != null)
        {
            {
                // Force disable left hand action to prevent null reference
                snapTurn.leftHandSnapTurnAction = new UnityEngine.InputSystem.InputActionProperty();
                snapTurn.enabled = true;
                Debug.Log($"Snap turn enabled: {snapTurn.enabled}");
            }
        }

        var continuousTurn = xrOrigin.GetComponent<ActionBasedContinuousTurnProvider>();
        if (continuousTurn != null)
        {
            continuousTurn.enabled = true;
            Debug.Log($"Continuous turn enabled: {continuousTurn.enabled}");
        }

        Debug.Log("Player movement enabled");
    }
}