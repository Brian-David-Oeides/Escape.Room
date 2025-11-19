using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// UNIFIED LOCOMOTION CONTROLLER - Facade Pattern
/// Manages Input Actions AND Movement Components as a single atomic unit.
/// Eliminates timing gaps that cause NullReferenceExceptions.
/// </summary>
/// 

public class LocomotionController : MonoBehaviour
{
    #region Singleton Pattern

    private static LocomotionController _instance;
    public static LocomotionController Instance => _instance;

    #endregion

    #region State Machine

    public enum LocomotionState
    {
        Disabled,      // Everything off (menu, game over, scene transitions)
        Initializing,  // Setting up - components MUST stay disabled
        Ready,         // Validated and safe to move
        Failed         // Initialization failed
    }

    private LocomotionState _state = LocomotionState.Disabled;
    public LocomotionState State => _state;

    #endregion

    #region Configuration

    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActionAsset;

    [Header("Validation Settings")]
    [SerializeField] private int maxRetryAttempts = 3;
    [SerializeField] private float retryDelaySeconds = 0.1f;
    [SerializeField] private int bindingResolutionFrames = 2;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    #endregion

    #region Component References

    private GameObject _xrOrigin;
    private ActionBasedContinuousMoveProvider _moveProvider;
    private ActionBasedSnapTurnProvider _snapTurnProvider;
    private ActionBasedContinuousTurnProvider _continuousTurnProvider;
    private TeleportationProvider _teleportProvider;

    #endregion

    #region Action Map References

    private InputActionMap _leftHandLocomotion;
    private InputActionMap _rightHandLocomotion;
    private InputActionMap _leftHandInteraction;
    private InputActionMap _rightHandInteraction;

    #endregion

    #region Events

    public event Action OnLocomotionReady;
    public event Action OnLocomotionFailed;
    public event Action<LocomotionState> OnStateChanged;

    #endregion

    #region Initialization

    private void Awake()
    {
        // Singleton setup
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Cache action maps
        CacheActionMaps();

        Log("LocomotionController initialized");
    }

    private void CacheActionMaps()
    {
        if (inputActionAsset == null)
        {
            LogError("InputActionAsset is not assigned!");
            return;
        }

        _leftHandLocomotion = inputActionAsset.FindActionMap("XRI LeftHand Locomotion");
        _rightHandLocomotion = inputActionAsset.FindActionMap("XRI RightHand Locomotion");
        _leftHandInteraction = inputActionAsset.FindActionMap("XRI LeftHand Interaction");
        _rightHandInteraction = inputActionAsset.FindActionMap("XRI RightHand Interaction");

        if (_leftHandLocomotion == null || _rightHandLocomotion == null)
        {
            LogError("Could not find required locomotion action maps!");
        }

        if (_leftHandInteraction == null || _rightHandInteraction == null)
        {
            LogError("Could not find required interaction action maps!");
        }
    }

    #endregion

    #region XR Origin Management

    /// <summary>
    /// Update XR Origin reference (call after scene loads)
    /// </summary>
    public void UpdateXROriginReference()
    {
        XROrigin xrOriginComponent = FindObjectOfType<XROrigin>();

        if (xrOriginComponent != null)
        {
            _xrOrigin = xrOriginComponent.gameObject;
            CacheLocomotionComponents();

            // CRITICAL: Force disable all components immediately
            ForceDisableAllComponents();

            Log($"XR Origin updated: {_xrOrigin.name}");
        }
        else
        {
            LogError("Could not find XR Origin in scene!");
        }
    }

    private void CacheLocomotionComponents()
    {
        if (_xrOrigin == null) return;

        _moveProvider = _xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
        _snapTurnProvider = _xrOrigin.GetComponent<ActionBasedSnapTurnProvider>();
        _continuousTurnProvider = _xrOrigin.GetComponent<ActionBasedContinuousTurnProvider>();
        _teleportProvider = _xrOrigin.GetComponent<TeleportationProvider>();

        Log("Locomotion components cached");
    }

    #endregion

    #region Atomic State Control

    /// <summary>
    /// ATOMIC OPERATION: Forcibly disable ALL locomotion (components + actions)
    /// This is the ONLY safe state during transitions
    /// </summary>
    private void ForceDisableAllComponents()
    {
        // 1. DISABLE COMPONENTS FIRST - stops Update() immediately
        if (_moveProvider != null) _moveProvider.enabled = false;
        if (_snapTurnProvider != null) _snapTurnProvider.enabled = false;
        if (_continuousTurnProvider != null) _continuousTurnProvider.enabled = false;
        if (_teleportProvider != null) _teleportProvider.enabled = false;

        // 2. Then disable action maps
        DisableActionMap(_leftHandLocomotion);
        DisableActionMap(_rightHandLocomotion);

        ChangeState(LocomotionState.Disabled);
        Log("✓ ALL locomotion forcibly disabled (atomic operation)");
    }

    /// <summary>
    /// ATOMIC OPERATION: Enable components ONLY after actions are validated
    /// </summary>
    private void EnableAllComponents()
    {
        if (_moveProvider != null)
        {
            _moveProvider.enabled = true;
            Log($"✓ ContinuousMove enabled: {_moveProvider.enabled}");
        }

        if (_snapTurnProvider != null)
        {
            _snapTurnProvider.enabled = true;
            Log($"✓ SnapTurn enabled: {_snapTurnProvider.enabled}");
        }

        if (_continuousTurnProvider != null)
        {
            _continuousTurnProvider.enabled = true;
            Log($"✓ ContinuousTurn enabled: {_continuousTurnProvider.enabled}");
        }

        if (_teleportProvider != null)
        {
            _teleportProvider.enabled = true;
            Log($"✓ Teleport enabled: {_teleportProvider.enabled}");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Initialize for gameplay - Handles the entire sequence atomically
    /// </summary>
    public void InitializeForGameplay(Action onSuccess = null, Action onFailure = null)
    {
        StartCoroutine(InitializeGameplayCoroutine(onSuccess, onFailure));
    }

    /// <summary>
    /// Switch to menu mode (disable locomotion, enable interaction)
    /// </summary>
    public void SwitchToMenuMode()
    {
        Log("Switching to MENU MODE");

        // Disable everything
        ForceDisableAllComponents();

        // Enable interaction maps only
        EnableActionMap(_leftHandInteraction);
        EnableActionMap(_rightHandInteraction);

        ChangeState(LocomotionState.Disabled);
    }

    /// <summary>
    /// Emergency shutdown - disable everything immediately
    /// </summary>
    public void EmergencyShutdown()
    {
        LogError("EMERGENCY SHUTDOWN triggered");

        // Find ALL movement providers in scene and disable them
        var allMoveProviders = FindObjectsOfType<ActionBasedContinuousMoveProvider>();
        foreach (var provider in allMoveProviders)
        {
            provider.enabled = false;
            Log($"Emergency disabled: {provider.gameObject.name}");
        }

        // Disable all action maps
        DisableActionMap(_leftHandLocomotion);
        DisableActionMap(_rightHandLocomotion);

        ChangeState(LocomotionState.Failed);
    }

    /// <summary>
    /// Check if locomotion is ready
    /// </summary>
    public bool IsReady()
    {
        return _state == LocomotionState.Ready;
    }

    #endregion

    #region Initialization Coroutine

    private IEnumerator InitializeGameplayCoroutine(Action onSuccess, Action onFailure)
    {
        Log("=== Starting Gameplay Locomotion Initialization ===");
        ChangeState(LocomotionState.Initializing);

        // Ensure XR Origin reference is current
        if (_xrOrigin == null)
        {
            UpdateXROriginReference();
        }

        // Validate we have what we need
        if (_xrOrigin == null || _moveProvider == null)
        {
            LogError("Missing required references!");
            ChangeState(LocomotionState.Failed);
            OnLocomotionFailed?.Invoke();
            onFailure?.Invoke();
            yield break;
        }

        int attemptCount = 0;
        bool success = false;

        while (attemptCount < maxRetryAttempts && !success)
        {
            attemptCount++;

            // Step 1: Ensure components are disabled
            ForceDisableAllComponents();

            // Step 2: KEEP interaction maps enabled
            EnableActionMap(_leftHandInteraction);
            EnableActionMap(_rightHandInteraction);

            // Step 3: Enable locomotion action maps
            EnableActionMap(_leftHandLocomotion);
            EnableActionMap(_rightHandLocomotion);

            // Step 4: CRITICAL FIX - Explicitly enable all actions in the maps
            EnableAllActionsInMap(_leftHandLocomotion);
            EnableAllActionsInMap(_rightHandLocomotion);

            // Step 5: Wait longer for Input System to fully rebind
            for (int i = 0; i < bindingResolutionFrames + 2; i++) // Changed: Wait 2 extra frames
            {
                yield return null;
            }

            // Step 6: Validate bindings AND test if actions can be read
            if (ValidateActionMaps() && ValidateActionsCanBeRead())
            {
                Log("✓ Bindings validated successfully");

                // Step 7: NOW SAFE to enable components
                EnableAllComponents();

                success = true;
                ChangeState(LocomotionState.Ready);
                Log("=== Gameplay Locomotion Initialization SUCCESSFUL ===");

                OnLocomotionReady?.Invoke();
                onSuccess?.Invoke();
            }
            else
            {
                if (attemptCount < maxRetryAttempts)
                {
                    yield return new WaitForSeconds(retryDelaySeconds);
                }
            }
        }

        if (!success)
        {
            LogError("=== Gameplay Locomotion Initialization FAILED ===");
            EmergencyShutdown();
            OnLocomotionFailed?.Invoke();
            onFailure?.Invoke();
        }
    }

    #endregion

    #region Validation

    private bool ValidateActionMaps()
    {
        if (_leftHandLocomotion == null || _rightHandLocomotion == null)
        {
            LogError("Action maps are null!");
            return false;
        }

        if (!_leftHandLocomotion.enabled || !_rightHandLocomotion.enabled)
        {
            LogError("Action maps are not enabled!");
            return false;
        }

        // Check if bindings are resolved
        foreach (var action in _leftHandLocomotion.actions)
        {
            if (action.controls.Count == 0)
            {
                LogWarning($"Action '{action.name}' has no bound controls!");
                return false;
            }
        }

        Log("✓ Action maps validated successfully");
        return true;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Explicitly enable all actions within an action map
    /// </summary>
    private void EnableAllActionsInMap(InputActionMap map)
    {
        if (map == null) return;

        foreach (var action in map.actions)
        {
            if (!action.enabled)
            {
                action.Enable();
            }
        }

        Log($"✓ Enabled all actions in map: {map.name}");
    }

    private void EnableActionMap(InputActionMap map)
    {
        if (map == null) return;

        if (!map.enabled)
        {
            map.Enable();
            Log($"✓ Enabled action map: {map.name}");
        }
    }

    private void DisableActionMap(InputActionMap map)
    {
        if (map == null) return;

        if (map.enabled)
        {
            map.Disable();
            Log($"✓ Disabled action map: {map.name}");
        }
    }

    private void ChangeState(LocomotionState newState)
    {
        if (_state != newState)
        {
            Log($"State changed: {_state} → {newState}");
            _state = newState;
            OnStateChanged?.Invoke(newState);
        }
    }

    /// <summary>
    /// Test if actions can actually be read without throwing exceptions
    /// </summary>
    private bool ValidateActionsCanBeRead()
    {
        try
        {
            // Test reading from move provider's actions
            if (_moveProvider != null)
            {
                // Test left hand move action
                if (_moveProvider.leftHandMoveAction != null && _moveProvider.leftHandMoveAction.action != null)
                {
                    var testValue = _moveProvider.leftHandMoveAction.action.ReadValue<Vector2>();
                }

                // Test right hand move action
                if (_moveProvider.rightHandMoveAction != null && _moveProvider.rightHandMoveAction.action != null)
                {
                    var testValue = _moveProvider.rightHandMoveAction.action.ReadValue<Vector2>();
                }
            }

            // Test reading from snap turn provider's actions
            if (_snapTurnProvider != null)
            {
                // Test left hand snap turn action
                if (_snapTurnProvider.leftHandSnapTurnAction != null && _snapTurnProvider.leftHandSnapTurnAction.action != null)
                {
                    var testValue = _snapTurnProvider.leftHandSnapTurnAction.action.ReadValue<Vector2>();
                }

                // Test right hand snap turn action
                if (_snapTurnProvider.rightHandSnapTurnAction != null && _snapTurnProvider.rightHandSnapTurnAction.action != null)
                {
                    var testValue = _snapTurnProvider.rightHandSnapTurnAction.action.ReadValue<Vector2>();
                }
            }

            Log("✓ Actions can be read successfully");
            return true;
        }
        catch (Exception ex)
        {
            LogWarning($"Action read test failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Logging

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[LocomotionController] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning($"[LocomotionController] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[LocomotionController] {message}");
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        OnLocomotionReady = null;
        OnLocomotionFailed = null;
        OnStateChanged = null;
    }

    #endregion
}
