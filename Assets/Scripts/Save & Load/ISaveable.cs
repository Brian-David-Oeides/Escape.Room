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

/// <summary>
/// Example implementation for a puzzle object
/// You would attach this to puzzle GameObjects
/// </summary>
public class SaveablePuzzle : MonoBehaviour, ISaveable
{
    [Header("Save System")]
    [Tooltip("Unique ID for this puzzle - must be unique in the scene")]
    public string puzzleID = "puzzle_001";

    private bool isCompleted = false;

    public string SaveID => puzzleID;

    public void SaveState(SaveData saveData)
    {
        // If puzzle is completed, add its ID to the completed list
        if (isCompleted && !saveData.completedPuzzleIDs.Contains(puzzleID))
        {
            saveData.completedPuzzleIDs.Add(puzzleID);
        }
    }

    public void LoadState(SaveData saveData)
    {
        // Check if this puzzle was completed in the save
        isCompleted = saveData.completedPuzzleIDs.Contains(puzzleID);

        // Apply the loaded state
        if (isCompleted)
        {
            // Puzzle was already solved - set it to completed state
            SetPuzzleCompleted();
        }
    }

    // Called when puzzle is solved during gameplay
    public void CompletePuzzle()
    {
        isCompleted = true;
        SetPuzzleCompleted();
    }

    private void SetPuzzleCompleted()
    {
        // Your puzzle completion logic here
        // Example: disable puzzle, unlock something, play animation, etc.
        Debug.Log($"Puzzle {puzzleID} is in completed state");
    }
}

/// <summary>
/// Example implementation for a consumable object
/// You would attach this to edible items (candy, food, etc.)
/// </summary>
public class SaveableConsumable : MonoBehaviour, ISaveable
{
    [Header("Save System")]
    [Tooltip("Unique ID for this consumable - must be unique in the scene")]
    public string consumableID = "candy_001";

    [Header("Consumable Properties")]
    public float energyRestoreAmount = 25f;
    public float healthRestoreAmount = 10f;

    private bool isConsumed = false;

    public string SaveID => consumableID;

    public void SaveState(SaveData saveData)
    {
        // If consumed, add to consumed list
        if (isConsumed && !saveData.consumedObjectIDs.Contains(consumableID))
        {
            saveData.consumedObjectIDs.Add(consumableID);
        }
    }

    public void LoadState(SaveData saveData)
    {
        // Check if this object was consumed in the save
        isConsumed = saveData.consumedObjectIDs.Contains(consumableID);

        if (isConsumed)
        {
            // Object was already consumed - hide/destroy it
            gameObject.SetActive(false);
        }
    }

    // Called when player eats this item
    public void Consume()
    {
        if (isConsumed) return;

        isConsumed = true;

        // TODO: Add to player's health/energy (when HealthEnergyManager exists)
        // HealthEnergyManager.Instance.RestoreEnergy(energyRestoreAmount);
        // HealthEnergyManager.Instance.RestoreHealth(healthRestoreAmount);

        // Hide the object
        gameObject.SetActive(false);

        Debug.Log($"Consumed {consumableID}: +{energyRestoreAmount} energy, +{healthRestoreAmount} health");
    }
}

/// <summary>
/// Example implementation for a moveable object (doors, drawers, etc.)
/// Saves position and rotation
/// </summary>
public class SaveableMoveable : MonoBehaviour, ISaveable
{
    [Header("Save System")]
    [Tooltip("Unique ID for this moveable object - must be unique in the scene")]
    public string objectID = "door_001";

    public string SaveID => objectID;

    public void SaveState(SaveData saveData)
    {
        // Save current position and rotation
        MoveableObjectData objectState = new MoveableObjectData(
            objectID,
            transform.position,
            transform.rotation,
            gameObject.activeSelf,
            "" // customData (optional)
        );

        saveData.moveableObjects.Add(objectState);
    }

    public void LoadState(SaveData saveData)
    {
        // Find this object's saved state
        MoveableObjectData savedState = saveData.moveableObjects.Find(obj => obj.objectID == objectID);

        if (savedState != null)
        {
            // Restore position and rotation
            transform.position = savedState.position;
            transform.rotation = savedState.rotation;
            gameObject.SetActive(savedState.isActive);

            Debug.Log($"Loaded state for {objectID}");
        }
    }
}
