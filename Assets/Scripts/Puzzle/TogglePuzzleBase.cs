using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for toggle-state puzzles (levers, switches, doors).
/// Saves and loads the current on/off state across game sessions.
/// 
/// USAGE:
/// 1. Inherit from TogglePuzzleBase
/// 2. Set unique puzzleID in inspector
/// 3. Override OnToggleOn() and OnToggleOff() for custom behavior
/// 4. Call SetToggleState(true/false) to change state
/// 5. Wire OnToggledOn and OnToggledOff UnityEvents in inspector
/// 
/// IMPORTANT: Toggle puzzles use MoveableObjectData to save state, NOT completedPuzzleIDs.
/// This allows them to remember their on/off position.
/// </summary>
public abstract class TogglePuzzleBase : MonoBehaviour, ISaveable
{
    [Header("Save System")]
    [Tooltip("Unique ID for this toggle puzzle - MUST be unique across all puzzles")]
    [SerializeField] protected string puzzleID = "toggle_base_001";

    [Header("Toggle State")]
    [Tooltip("Is this toggle currently ON?")]
    [SerializeField] protected bool isOn = false;

    [Header("Toggle Events")]
    [Tooltip("Fired when toggle is switched ON")]
    [SerializeField] protected UnityEvent OnToggledOn;

    [Tooltip("Fired when toggle is switched OFF")]
    [SerializeField] protected UnityEvent OnToggledOff;

    [Tooltip("Fired when state is loaded (use for setting initial scene state on load)")]
    [SerializeField] protected UnityEvent OnStateLoaded;

    [Header("Debug")]
    [SerializeField] protected bool showDebugLogs = true;

    public string SaveID => puzzleID;

    protected virtual void Start()
    {
        DebugLog($"Toggle puzzle {puzzleID} initialized. State: {(isOn ? "ON" : "OFF")}");
    }

    /// <summary>
    /// Set the toggle state (on/off)
    /// Call this from your toggle interaction logic
    /// </summary>
    public virtual void SetToggleState(bool newState)
    {
        if (isOn == newState)
        {
            DebugLog($"Toggle already in {(newState ? "ON" : "OFF")} state - ignoring");
            return;
        }

        isOn = newState;
        DebugLog($"Toggle state changed to: {(isOn ? "ON" : "OFF")}");

        if (isOn)
        {
            OnToggleOn();
            OnToggledOn?.Invoke();
        }
        else
        {
            OnToggleOff();
            OnToggledOff?.Invoke();
        }
    }

    /// <summary>
    /// Toggle between on and off states
    /// </summary>
    public void Toggle()
    {
        SetToggleState(!isOn);
    }

    /// <summary>
    /// Override this for custom ON behavior (animations, sounds, etc.)
    /// </summary>
    protected virtual void OnToggleOn()
    {
        DebugLog($"{puzzleID} toggled ON");
    }

    /// <summary>
    /// Override this for custom OFF behavior (animations, sounds, etc.)
    /// </summary>
    protected virtual void OnToggleOff()
    {
        DebugLog($"{puzzleID} toggled OFF");
    }

    /// <summary>
    /// Apply the current state visually (called on load)
    /// Override to add custom visual state restoration
    /// </summary>
    protected virtual void ApplyCurrentState(bool loadedState)
    {
        isOn = loadedState;
        DebugLog($"Applying loaded state: {(isOn ? "ON" : "OFF")}");

        // Fire appropriate events without triggering toggle logic
        if (isOn)
        {
            OnToggleOn();
            OnToggledOn?.Invoke();
        }
        else
        {
            OnToggleOff();
            OnToggledOff?.Invoke();
        }

        OnStateLoaded?.Invoke();
    }

    #region ISaveable Implementation

    public void SaveState(SaveData saveData)
    {
        // Find existing entry or create new one
        MoveableObjectData existingData = saveData.moveableObjects.Find(obj => obj.objectID == puzzleID);

        if (existingData != null)
        {
            // Update existing entry
            existingData.customData = isOn ? "ON" : "OFF";
            existingData.position = transform.position;
            existingData.rotation = transform.rotation;
        }
        else
        {
            // Create new entry
            MoveableObjectData toggleData = new MoveableObjectData
            {
                objectID = puzzleID,
                position = transform.position,
                rotation = transform.rotation,
                customData = isOn ? "ON" : "OFF"
            };
            saveData.moveableObjects.Add(toggleData);
        }

        DebugLog($"Saved toggle state: {(isOn ? "ON" : "OFF")}");
    }

    public void LoadState(SaveData saveData)
    {
        // Find this toggle's data
        MoveableObjectData toggleData = saveData.moveableObjects.Find(obj => obj.objectID == puzzleID);

        if (toggleData != null)
        {
            bool loadedState = toggleData.customData == "ON";
            DebugLog($"Loading toggle state: {(loadedState ? "ON" : "OFF")}");
            ApplyCurrentState(loadedState);
        }
        else
        {
            DebugLog($"No saved state found for {puzzleID} - using default state: {(isOn ? "ON" : "OFF")}");
        }
    }

    #endregion

    #region Debug

    protected void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[TogglePuzzleBase:{puzzleID}] {message}");
        }
    }

    #endregion

    #region Editor Helpers

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(puzzleID) || puzzleID == "toggle_base_001")
        {
            Debug.LogWarning($"[{gameObject.name}] Toggle Puzzle ID is not set or using default! Set a unique puzzleID in inspector.", this);
        }
    }
#endif

    #endregion
}
