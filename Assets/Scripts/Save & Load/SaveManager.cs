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

                // Auto-save to current slot (if one is loaded)
                if (currentSaveSlot != -1)
                {
                    DebugLog("Auto-save triggered");
                    SaveGame(currentSaveSlot, isAutoSave: true);
                }
            }
        }
    }

    #region Save Operations

    /// <summary>
    /// Save the current game to a specific slot
    /// </summary>
    public bool SaveGame(int slotNumber, bool isAutoSave = false)
    {
        if (slotNumber < 1 || slotNumber > 3)
        {
            Debug.LogError($"Invalid save slot number: {slotNumber}. Must be 1-3.");
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
            DebugLog($"{saveType} game to slot {slotNumber}");

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

        // Save health/energy (when HealthEnergyManager exists)
        // TODO: Uncomment when HealthEnergyManager is implemented
        // if (HealthEnergyManager.Instance != null)
        // {
        //     data.currentHealth = HealthEnergyManager.Instance.CurrentHealth;
        //     data.currentEnergy = HealthEnergyManager.Instance.CurrentEnergy;
        // }

        // Save timer state (when TimerManager exists)
        // TODO: Uncomment when TimerManager is implemented
        // if (TimerManager.Instance != null)
        // {
        //     data.timeRemaining = TimerManager.Instance.TimeRemaining;
        //     data.timerEnabled = TimerManager.Instance.IsTimerEnabled;
        //     data.timerDuration = TimerManager.Instance.TimerDuration;
        // }

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
        if (slotNumber < 1 || slotNumber > 3)
        {
            Debug.LogError($"Invalid save slot number: {slotNumber}. Must be 1-3.");
            return false;
        }

        string filePath = GetSaveFilePath(slotNumber);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No save file found in slot {slotNumber}");
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

            DebugLog($"Loaded save from slot {slotNumber}");

            // Start coroutine to load the saved scene
            StartCoroutine(LoadGameCoroutine(loadedData));

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game from slot {slotNumber}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Coroutine to load the saved scene and restore game state
    /// </summary>
    private IEnumerator LoadGameCoroutine(SaveData data)
    {
        // If we're not in the correct scene, load it first
        if (SceneManager.GetActiveScene().name != data.currentSceneName)
        {
            DebugLog($"Loading scene: {data.currentSceneName}");

            // Use GameManager's scene loading system
            if (GameManager.Instance != null)
            {
                // Trigger loading state
                GameManager.Instance.SetGameState(GameState.Loading);

                // Wait for scene to load
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(data.currentSceneName);
                while (!asyncLoad.isDone)
                {
                    yield return null;
                }
            }
            else
            {
                // Fallback if GameManager doesn't exist
                SceneManager.LoadScene(data.currentSceneName);
                yield return null;
            }
        }

        // Wait a frame for scene to initialize
        yield return new WaitForEndOfFrame();

        // Restore game state
        RestoreGameState(data);

        // Notify listeners
        OnGameLoaded?.Invoke(currentSaveSlot);

        DebugLog("Game state restored");
    }

    /// <summary>
    /// Restore all game state from loaded data
    /// </summary>
    private void RestoreGameState(SaveData data)
    {
        // Restore player position
        if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            PlayerController.Instance.XROrigin.transform.position = data.playerPosition;
            PlayerController.Instance.XROrigin.transform.rotation = data.playerRotation;
            DebugLog($"Player position restored to {data.playerPosition}");
        }

        // Restore health/energy (when implemented)
        // TODO: Uncomment when HealthEnergyManager exists
        // if (HealthEnergyManager.Instance != null)
        // {
        //     HealthEnergyManager.Instance.SetHealth(data.currentHealth);
        //     HealthEnergyManager.Instance.SetEnergy(data.currentEnergy);
        // }

        // Restore timer (when implemented)
        // TODO: Uncomment when TimerManager exists
        // if (TimerManager.Instance != null)
        // {
        //     TimerManager.Instance.SetTimeRemaining(data.timeRemaining);
        //     TimerManager.Instance.SetTimerEnabled(data.timerEnabled);
        // }

        // Restore all ISaveable objects in the scene
        ISaveable[] saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
        foreach (ISaveable saveable in saveableObjects)
        {
            saveable.LoadState(data);
        }

        // Set game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(data.gameState);
        }
    }

    #endregion

    #region Save Slot Management

    /// <summary>
    /// Delete a save slot
    /// </summary>
    public bool DeleteSave(int slotNumber)
    {
        if (slotNumber < 1 || slotNumber > 3)
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
                DebugLog($"Deleted save slot {slotNumber}");

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