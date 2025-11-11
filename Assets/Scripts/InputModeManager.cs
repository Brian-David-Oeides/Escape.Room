using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMode
{
    Menu,      // UI interaction only, no locomotion
    Gameplay   // Full locomotion + interaction
}

public class InputModeManager : MonoSingleton<InputModeManager>
{
    [Header("Input Action Asset Reference")]
    public InputActionAsset inputActionAsset;

    [Header("Action Map Names")]
    [SerializeField] private string leftHandLocomotionMap = "XRI LeftHand Locomotion";
    [SerializeField] private string rightHandLocomotionMap = "XRI RightHand Locomotion";
    [SerializeField] private string leftHandInteractionMap = "XRI LeftHand Interaction";
    [SerializeField] private string rightHandInteractionMap = "XRI RightHand Interaction";
    [SerializeField] private string uIMap = "XRI UI";

    private InputMode currentMode = InputMode.Menu;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Init()
    {
        DontDestroyOnLoad(gameObject);

        // Validate input action asset
        if (inputActionAsset == null)
        {
            Debug.LogError("InputActionAsset is not assigned in InputModeManager!");
            return;
        }

        Debug.Log("InputModeManager initialized");
    }

    /// <summary>
    /// Switches to Menu Mode - UI only, no locomotion
    /// </summary>
    public void SwitchToMenuMode()
    {
        if (inputActionAsset == null)
        {
            Debug.LogError("Cannot switch to Menu Mode - InputActionAsset is null");
            return;
        }

        currentMode = InputMode.Menu;
        Debug.Log("Switching to MENU MODE");

        // Step 1: Disable locomotion action maps FIRST
        DisableActionMap(leftHandLocomotionMap);
        DisableActionMap(rightHandLocomotionMap);

        // Step 2: Disable locomotion providers (via PlayerController)
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }

        // Step 3: Enable UI and interaction maps
        EnableActionMap(uIMap);
        EnableActionMap(leftHandInteractionMap);
        EnableActionMap(rightHandInteractionMap);

        Debug.Log("Menu Mode activated");
    }

    /// <summary>
    /// Switches to Gameplay Mode - locomotion + interaction enabled
    /// Use this for scene transitions and restarts (has built-in delay for binding resolution)
    /// </summary>
    public void SwitchToGameplayMode()
    {
        StartCoroutine(SwitchToGameplayModeCoroutine());
    }

    /// <summary>
    /// Immediately switches to gameplay mode without waiting (use with caution)
    /// Only use this if you're certain bindings are already resolved
    /// </summary>
    public void SwitchToGameplayModeImmediate()
    {
        SwitchToGameplayModeInternal();
    }

    private IEnumerator SwitchToGameplayModeCoroutine()
    {
        if (inputActionAsset == null)
        {
            Debug.LogError("Cannot switch to Gameplay Mode - InputActionAsset is null");
            yield break;
        }

        currentMode = InputMode.Gameplay;
        Debug.Log("Switching to GAMEPLAY MODE (with binding resolution wait)");

        // Step 1: Ensure Time.timeScale is normal
        Time.timeScale = 1f;

        // Step 2: FIRST disable locomotion providers
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }

        // Step 3: Enable locomotion action maps
        EnableActionMap(leftHandLocomotionMap);
        EnableActionMap(rightHandLocomotionMap);

        // Step 4: Keep interaction maps enabled
        EnableActionMap(leftHandInteractionMap);
        EnableActionMap(rightHandInteractionMap);

        // CRITICAL: Wait for binding resolution to complete
        // The Input System needs time to resolve bindings and populate processor arrays
        // Without this wait, ContinuousMoveProvider will try to read from null processor arrays
        yield return null; // Wait 1 frame
        yield return null; // Wait another frame to be safe

        // Step 5: NOW it's safe to enable locomotion providers
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EnableMovement();
        }

        Debug.Log("Gameplay Mode activated (with safe binding resolution)");
    }

    private void SwitchToGameplayModeInternal()
    {
        if (inputActionAsset == null)
        {
            Debug.LogError("Cannot switch to Gameplay Mode - InputActionAsset is null");
            return;
        }

        currentMode = InputMode.Gameplay;
        Debug.Log("Switching to GAMEPLAY MODE (immediate)");

        // Step 1: Ensure Time.timeScale is normal
        Time.timeScale = 1f;

        // Step 2: FIRST disable locomotion providers
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }

        // Step 3: Enable locomotion action maps
        EnableActionMap(leftHandLocomotionMap);
        EnableActionMap(rightHandLocomotionMap);

        // Step 4: Keep interaction maps enabled
        EnableActionMap(leftHandInteractionMap);
        EnableActionMap(rightHandInteractionMap);

        // Step 5: Enable locomotion providers
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EnableMovement();
        }

        Debug.Log("Gameplay Mode activated (immediate)");
    }

    /// <summary>
    /// Forces a full reset of all action maps - use before scene loads
    /// </summary>
    public void ResetAllActionMaps()
    {
        if (inputActionAsset == null) return;

        StartCoroutine(ResetAllActionMapsCoroutine());
    }

    private IEnumerator ResetAllActionMapsCoroutine()
    {
        Debug.Log("Resetting all action maps");

        // Step 1: Disable locomotion providers immediately
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }

        // Step 2: Disable all action maps
        inputActionAsset.Disable();

        // Step 3: Wait for multiple frames to ensure clean slate
        yield return null;
        yield return null;

        // Step 4: Re-enable based on target mode
        if (currentMode == InputMode.Menu)
        {
            SwitchToMenuMode();
        }
        else
        {
            // Use the coroutine version for proper timing
            SwitchToGameplayMode();
        }

        Debug.Log("Action maps reset complete");
    }

    private void EnableActionMap(string mapName)
    {
        var actionMap = inputActionAsset.FindActionMap(mapName);
        if (actionMap != null)
        {
            actionMap.Enable();
            Debug.Log($"✓ Enabled action map: {mapName}");
        }
        else
        {
            Debug.LogWarning($"Action map not found: {mapName}");
        }
    }

    private void DisableActionMap(string mapName)
    {
        var actionMap = inputActionAsset.FindActionMap(mapName);
        if (actionMap != null)
        {
            actionMap.Disable();
            Debug.Log($"✗ Disabled action map: {mapName}");
        }
        else
        {
            Debug.LogWarning($"Action map not found: {mapName}");
        }
    }

    public InputMode GetCurrentMode()
    {
        return currentMode;
    }
}