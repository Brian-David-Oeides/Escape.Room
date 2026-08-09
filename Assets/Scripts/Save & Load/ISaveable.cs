using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for any object that needs to save/load its state
/// Implement this on puzzle scripts, consumable objects, moveable objects, etc.
/// </summary>

public interface ISaveable 
{
    /// <summary>
    /// Unique identifier for this saveable object
    /// Should be set in the inspector or generated at runtime
    /// MUST be unique within the scene
    /// </summary>
    string SaveID { get; }

    /// <summary>
    /// Save this object's current state to the SaveData
    /// Called when the game is saved
    /// </summary>
    void SaveState(SaveData saveData);

    /// <summary>
    /// Restore this object's state from the SaveData
    /// Called when the game is loaded
    /// </summary>
    void LoadState(SaveData saveData);
}
