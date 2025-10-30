using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure that holds all saveable game data
/// This gets serialized to JSON and saved to disk
/// </summary>
[System.Serializable]

public class SaveData
{
    // Save metadata
    public string saveName;

    // Store timestamp as string for JSON serialization
    public string saveTimestampString;

    public DateTime saveTimestamp
    {
        get
        {
            if (DateTime.TryParse(saveTimestampString, out DateTime result))
                return result;
            return DateTime.MinValue;
        }
        set
        {
            saveTimestampString = value.ToString("o"); // ISO 8601 format
        }
    }

    public int saveSlotNumber; // 1, 2, or 3

    // Player state
    public float currentHealth = 100f;
    public float currentEnergy = 100f;
    public Vector3 playerPosition;
    public Quaternion playerRotation;

    // Timer state
    public float timeRemaining; // Time left on countdown timer
    public bool timerEnabled;
    public float timerDuration; // Total timer duration setting

    // Game state
    public string currentSceneName;
    public GameState gameState;

    // Puzzle tracking (binary - completed or not)
    public List<string> completedPuzzleIDs = new List<string>();

    // Consumable objects (which ones have been eaten)
    public List<string> consumedObjectIDs = new List<string>();

    // Moveable objects (position/rotation tracking)
    public List<ObjectStateData> moveableObjects = new List<ObjectStateData>();

    // Statistics
    public float totalPlaytime; // Total seconds played
    public int puzzlesSolvedCount;
    public int deathCount;
    public int timesSaved;
    public int timesLoaded;

    // Constructor
    public SaveData(int slotNumber)
    {
        saveSlotNumber = slotNumber;
        saveName = $"Save Slot {slotNumber}";
        saveTimestamp = DateTime.Now;
    }
}

/// <summary>
/// Stores position and rotation data for moveable objects
/// </summary>
[System.Serializable]
public class ObjectStateData
{
    public string objectID; // Unique identifier for the object
    public Vector3 position;
    public Quaternion rotation;
    public bool isActive; // Whether object is active in scene

    public ObjectStateData(string id, Vector3 pos, Quaternion rot, bool active)
    {
        objectID = id;
        position = pos;
        rotation = rot;
        isActive = active;
    }
}

/// <summary>
/// Lightweight data for displaying save slot info in UI
/// </summary>
[System.Serializable]
public class SaveSlotInfo
{
    public int slotNumber;
    public bool hasData;
    public string saveName;

    // Store timestamp as string for JSON serialization
    public string saveTimestampString;

    public DateTime saveTimestamp
    {
        get
        {
            if (DateTime.TryParse(saveTimestampString, out DateTime result))
                return result;
            return DateTime.MinValue;
        }
        set
        {
            saveTimestampString = value.ToString("o"); // ISO 8601 format
        }
    }

    public float playtime;
    public int puzzlesSolved;
    public string sceneName;

    public SaveSlotInfo(int slot)
    {
        slotNumber = slot;
        hasData = false;
    }
}
