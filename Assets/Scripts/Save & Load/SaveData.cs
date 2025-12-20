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

    // Timer State (complete fields for proper save/load)
    public float timerElapsedTime;
    public float timerRemainingTime;
    public bool timerIsRunning;
    public bool timerExpired;
    public int timerMode;                // 0=Disabled, 1=CountUp, 2=CountDown
    public float timerCountdownDuration;

    [Header("Clue System Data")]
    public List<string> discoveredClueIDs = new List<string>();
    public List<string> puzzleFailedAttemptsData = new List<string>(); // Format: "puzzleID:attemptCount"
    public int totalCluesDiscovered = 0;
    public int totalHintsUsed = 0;

    // Game state
    public string currentSceneName;
    public GameState gameState;

    // Puzzle tracking (binary - completed or not)
    public List<string> completedPuzzleIDs = new List<string>();

    // Consumable objects (which ones have been eaten)
    public List<string> consumedObjectIDs = new List<string>();

    // Moveable objects (position/rotation/state tracking)
    public List<MoveableObjectData> moveableObjects = new List<MoveableObjectData>();

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
/// Stores position, rotation, and custom state data for moveable and toggle objects
/// ARCHITECTURAL FIX: Unified class for both physical objects and puzzle states
/// </summary>
[System.Serializable]
public class MoveableObjectData
{
    public string objectID; // Unique identifier for the object
    public Vector3 position;
    public Quaternion rotation;
    public bool isActive; // Whether object is active in scene
    public string customData; // Custom state data (e.g., "ON", "OFF", angles, etc.)

    public MoveableObjectData()
    {
        objectID = "";
        position = Vector3.zero;
        rotation = Quaternion.identity;
        isActive = true;
        customData = "";
    }

    public MoveableObjectData(string id, Vector3 pos, Quaternion rot, bool active, string data = "")
    {
        objectID = id;
        position = pos;
        rotation = rot;
        isActive = active;
        customData = data;
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