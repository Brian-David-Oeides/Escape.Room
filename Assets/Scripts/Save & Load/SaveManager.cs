using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Manages all save and load operations
/// Handles 3 save slots, auto-save, and manual save functionality
/// </summary>

public class SaveManager : MonoSingleton<SaveManager>
{
    [Header("Auto-Save Settings")]
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes in seconds

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Events
    public System.Action<int> OnGameSaved; // int = slot number
    public System.Action<int> OnGameLoaded; // int = slot number
    public System.Action<int> OnSaveDeleted; // int = slot number

    // Current save data
    private SaveData currentSaveData;
    private int currentSaveSlot = -1; // -1 means no slot loaded
    private float autoSaveTimer = 0f;

    // Save file paths
    private string SaveFolderPath => Path.Combine(Application.persistentDataPath, "Saves");

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Init()
    {
        DontDestroyOnLoad(gameObject);

        // Create saves folder if it doesn't exist
        if (!Directory.Exists(SaveFolderPath))
        {
            Directory.CreateDirectory(SaveFolderPath);
            DebugLog("Created saves folder at: " + SaveFolderPath);
        }

        DebugLog("SaveManager initialized");
    }

    private void Update()
    {
        // Auto-save timer (only during gameplay)
        if (autoSaveEnabled && GameManager.Instance != null &&
            GameManager.Instance.currentState == GameState.Playing)
        {
            autoSaveTimer += Time.deltaTime;

            if (autoSaveTimer >= autoSaveInterval)
            {
                autoSaveTimer = 0f;

                // CRITICAL: Auto-save always goes to Continue Slot (Slot 0)
                DebugLog("Auto-save triggered");
                SaveGame(0, isAutoSave: true);
            }
        }
    }

    #region Save Operations

    /// <summary>
    /// Save the current game to a specific slot
    /// </summary>
    public bool SaveGame(int slotNumber, bool isAutoSave = false)
    {
        // Slot 0 = Continue slot (hidden from user)
        // Slots 1-3 = Manual save slots
        if (slotNumber < 0 || slotNumber > 3)
        {
            Debug.LogError($"Invalid save slot number: {slotNumber}. Must be 0-3.");
            return false;
        }

        // Don't allow saving when player is dead
        if (HealthEnergyManager.Instance != null && HealthEnergyManager.Instance.GetCurrentHealth() <= 0)
        {
            DebugLog("Cannot save - player health is 0");
            return false;
        }

        try
        {
            // Create new save data or use existing
            if (currentSaveData == null || currentSaveSlot != slotNumber)
            {
                currentSaveData = new SaveData(slotNumber);
            }

            // Update metadata
            currentSaveData.saveTimestamp = DateTime.Now;
            currentSaveData.timesSaved++;

            // Gather data from all game systems
            GatherGameData(currentSaveData);

            // Serialize to JSON
            string json = JsonUtility.ToJson(currentSaveData, true);

            // Write to file
            string filePath = GetSaveFilePath(slotNumber);
            File.WriteAllText(filePath, json);

            currentSaveSlot = slotNumber;

            string saveType = isAutoSave ? "Auto-saved" : "Saved";
            string slotName = slotNumber == 0 ? "Continue Slot" : $"Slot {slotNumber}";
            DebugLog($"{saveType} game to {slotName}");

            // Notify listeners
            OnGameSaved?.Invoke(slotNumber);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game to slot {slotNumber}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gather all data from game systems to save
    /// </summary>
    private void GatherGameData(SaveData data)
    {
        // Save scene info
        data.currentSceneName = SceneManager.GetActiveScene().name;
        data.gameState = GameManager.Instance != null ? GameManager.Instance.currentState : GameState.MainMenu;

        // Save player state
        if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            data.playerPosition = PlayerController.Instance.XROrigin.transform.position;
            data.playerRotation = PlayerController.Instance.XROrigin.transform.rotation;
        }

        // Save health/energy
        if (HealthEnergyManager.Instance != null)
        {
            // Don't save if health is 0 (game over state)
            if (HealthEnergyManager.Instance.GetCurrentHealth() > 0)
            {
                data.currentHealth = HealthEnergyManager.Instance.GetCurrentHealth();
                data.currentEnergy = HealthEnergyManager.Instance.GetCurrentEnergy();
            }
            else
            {
                // If health is 0, save with starting values instead
                data.currentHealth = HealthEnergyManager.Instance.GetMaxHealth();
                data.currentEnergy = HealthEnergyManager.Instance.GetMaxEnergy();
                DebugLog("Health was 0, saving with starting values instead");
            }
        }

        // Save timer state (when TimerManager exists)
        // TODO: Uncomment when TimerManager is implemented
        // Save timer state
        if (GameTimer.Instance != null)
        {
            data.timeRemaining = GameTimer.Instance.RemainingTime;
            data.timerEnabled = GameTimer.Instance.IsRunning;
            data.timerDuration = GameTimer.Instance.Mode == GameTimer.TimerMode.CountDown ?
                                 GameTimer.Instance.RemainingTime + GameTimer.Instance.ElapsedTime : 0f;

            DebugLog($"Timer state saved - Remaining: {data.timeRemaining}s, Running: {data.timerEnabled}");
        }

        // Save statistics
        if (GameManager.Instance != null)
        {
            data.totalPlaytime = GameManager.Instance.GetGameTime();
        }

        // Clear lists before gathering (prevent duplicates)
        data.completedPuzzleIDs.Clear();
        data.consumedObjectIDs.Clear();
        data.moveableObjects.Clear();

        // Gather data from all ISaveable objects in the scene
        ISaveable[] saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
        foreach (ISaveable saveable in saveableObjects)
        {
            saveable.SaveState(data);
        }

        DebugLog($"Gathered data: {data.completedPuzzleIDs.Count} puzzles, {data.consumedObjectIDs.Count} consumables, {data.moveableObjects.Count} moveable objects");
    }

    #endregion

    #region Load Operations

    /// <summary>
    /// Load a game from a specific slot
    /// </summary>
    public bool LoadGame(int slotNumber)
    {
        if (slotNumber < 0 || slotNumber > 3)
        {
            Debug.LogError($"Invalid save slot number: {slotNumber}. Must be 0-3.");
            return false;
        }

        string filePath = GetSaveFilePath(slotNumber);

        if (!File.Exists(filePath))
        {
            string slotName = slotNumber == 0 ? "Continue Slot" : $"Slot {slotNumber}";
            Debug.LogWarning($"No save file found in {slotName}");
            return false;
        }

        try
        {
            // Read from file
            string json = File.ReadAllText(filePath);

            // Deserialize
            SaveData loadedData = JsonUtility.FromJson<SaveData>(json);

            if (loadedData == null)
            {
                Debug.LogError("Failed to deserialize save data");
                return false;
            }

            currentSaveData = loadedData;
            currentSaveSlot = slotNumber;
            currentSaveData.timesLoaded++;

            string slotName = slotNumber == 0 ? "Continue Slot" : $"Slot {slotNumber}";
            DebugLog($"Loaded save from {slotName}");

            // Let GameManager handle the scene loading properly
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadSavedGame(loadedData);
            }
            else
            {
                Debug.LogError("GameManager not found!");
                return false;
            }

            // Notify listeners
            OnGameLoaded?.Invoke(currentSaveSlot);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game from slot {slotNumber}: {e.Message}");
            return false;
        }
    }

    #endregion

    #region Save Slot Management

    /// <summary>
    /// Delete a save slot
    /// </summary>
    public bool DeleteSave(int slotNumber)
    {
        if (slotNumber < 0 || slotNumber > 3)
        {
            Debug.LogError($"Invalid save slot number: {slotNumber}");
            return false;
        }

        string filePath = GetSaveFilePath(slotNumber);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                string slotName = slotNumber == 0 ? "Continue Slot" : $"Slot {slotNumber}";
                DebugLog($"Deleted {slotName}");

                // Clear current save if it was this slot
                if (currentSaveSlot == slotNumber)
                {
                    currentSaveData = null;
                    currentSaveSlot = -1;
                }

                OnSaveDeleted?.Invoke(slotNumber);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save slot {slotNumber}: {e.Message}");
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a save slot has data
    /// </summary>
    public bool SaveSlotExists(int slotNumber)
    {
        return File.Exists(GetSaveFilePath(slotNumber));
    }

    /// <summary>
    /// Get info about a save slot for UI display
    /// </summary>
    public SaveSlotInfo GetSaveSlotInfo(int slotNumber)
    {
        SaveSlotInfo info = new SaveSlotInfo(slotNumber);

        string filePath = GetSaveFilePath(slotNumber);

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                info.hasData = true;
                info.saveName = data.saveName;
                info.saveTimestamp = data.saveTimestamp;
                info.playtime = data.totalPlaytime;
                info.puzzlesSolved = data.puzzlesSolvedCount;
                info.sceneName = data.currentSceneName;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read save slot {slotNumber} info: {e.Message}");
            }
        }

        return info;
    }

    /// <summary>
    /// Get all save slot info (for displaying all 3 slots in UI)
    /// </summary>
    public SaveSlotInfo[] GetAllSaveSlots()
    {
        SaveSlotInfo[] slots = new SaveSlotInfo[3];
        for (int i = 1; i <= 3; i++)
        {
            slots[i - 1] = GetSaveSlotInfo(i);
        }
        return slots;
    }

    #endregion

    #region Helper Methods

    private string GetSaveFilePath(int slotNumber)
    {
        return Path.Combine(SaveFolderPath, $"SaveSlot{slotNumber}.json");
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SaveManager] {message}");
        }
    }

    /// <summary>
    /// Clear current save data (for starting new game)
    /// </summary>
    public void ClearCurrentSaveData()
    {
        currentSaveData = null;
        currentSaveSlot = -1;
        DebugLog("Cleared current save data for new game");
    }

    /// <summary>
    /// Set auto-save interval (can be called from settings)
    /// </summary>
    public void SetAutoSaveInterval(float intervalInSeconds)
    {
        autoSaveInterval = intervalInSeconds;
        DebugLog($"Auto-save interval set to {intervalInSeconds} seconds");
    }

    /// <summary>
    /// Enable or disable auto-save
    /// </summary>
    public void SetAutoSaveEnabled(bool enabled)
    {
        autoSaveEnabled = enabled;
        DebugLog($"Auto-save {(enabled ? "enabled" : "disabled")}");
    }

    public int GetCurrentSaveSlot() => currentSaveSlot;
    public SaveData GetCurrentSaveData() => currentSaveData;

    #endregion
}

// Extension method to use LINQ with FindObjectsOfType
public static class Extensions
{
    public static IEnumerable<T> OfType<T>(this UnityEngine.Object[] array)
    {
        foreach (var item in array)
        {
            if (item is T typedItem)
            {
                yield return typedItem;
            }
        }
    }
}