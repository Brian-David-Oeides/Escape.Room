using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the set of ConsumableObject instances active in the scene based on difficulty.
/// Core consumables are always active; optional consumables are randomly trimmed down to
/// match the difficulty's target count. Implements ISaveable so the chosen set survives
/// save/load instead of re-randomizing.
/// </summary>

public class ConsumableManager : MonoSingleton<ConsumableManager>, ISaveable
{
    [Header("Save System")]
    [Tooltip("Unique ID for this manager - MUST be unique in scene")]
    [SerializeField] private string saveID = "consumableManager_001";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool hasAppliedDifficulty = false;
    private List<string> inactiveOptionalConsumableIDs = new List<string>();

    public string SaveID => saveID;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Init()
    {
        DontDestroyOnLoad(gameObject);

        DebugLog("ConsumableManager initialized");
    }

    #region Difficulty Application

    /// <summary>
    /// Apply a difficulty-based target count to the consumables currently in the scene.
    /// Core consumables always stay active; optional consumables are randomly trimmed
    /// down to fill the remaining budget. Only randomizes once per new game - call
    /// ResetForNewGame() first if a fresh randomization is required.
    /// </summary>
    public void ApplyDifficultyCount(int targetCount)
    {
        if (hasAppliedDifficulty)
        {
            DebugLog("Difficulty already applied this game - skipping re-randomization");
            return;
        }

        ConsumableObject[] allConsumables = FindObjectsOfType<ConsumableObject>();

        List<ConsumableObject> coreList = allConsumables.Where(c => c.IsCore).ToList();
        List<ConsumableObject> optionalList = allConsumables.Where(c => !c.IsCore).ToList();

        DebugLog($"Found {allConsumables.Length} consumables total - {coreList.Count} core, {optionalList.Count} optional");

        int optionalToKeep = Mathf.Clamp(targetCount - coreList.Count, 0, optionalList.Count);

        // Shuffle a copy of the optional list and keep the first N
        List<ConsumableObject> shuffledOptional = new List<ConsumableObject>(optionalList);
        ShuffleList(shuffledOptional);

        List<ConsumableObject> optionalToActivate = shuffledOptional.Take(optionalToKeep).ToList();
        List<ConsumableObject> optionalToDeactivate = shuffledOptional.Skip(optionalToKeep).ToList();

        inactiveOptionalConsumableIDs.Clear();

        foreach (ConsumableObject consumable in optionalToDeactivate)
        {
            consumable.gameObject.SetActive(false);
            inactiveOptionalConsumableIDs.Add(consumable.SaveID);
        }

        hasAppliedDifficulty = true;

        DebugLog($"Target count: {targetCount} - kept {coreList.Count} core + {optionalToActivate.Count} optional active, deactivated {optionalToDeactivate.Count} optional");
    }

    /// <summary>
    /// Fisher-Yates shuffle
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Clear the applied-difficulty flag so ApplyDifficultyCount will re-randomize on
    /// its next call. Call this when starting a genuinely new game.
    /// </summary>
    public void ResetForNewGame()
    {
        hasAppliedDifficulty = false;
        inactiveOptionalConsumableIDs.Clear();
        DebugLog("Reset for new game - difficulty will be re-applied on next ApplyDifficultyCount call");
    }

    #endregion

    #region ISaveable Implementation

    public void SaveState(SaveData saveData)
    {
        saveData.inactiveOptionalConsumableIDs = new List<string>(inactiveOptionalConsumableIDs);
        DebugLog($"Saved {inactiveOptionalConsumableIDs.Count} inactive optional consumable IDs");
    }

    public void LoadState(SaveData saveData)
    {
        inactiveOptionalConsumableIDs = new List<string>(saveData.inactiveOptionalConsumableIDs);

        ConsumableObject[] allConsumables = FindObjectsOfType<ConsumableObject>();
        int deactivatedCount = 0;

        foreach (string id in inactiveOptionalConsumableIDs)
        {
            ConsumableObject match = allConsumables.FirstOrDefault(c => c.SaveID == id);
            if (match != null)
            {
                match.gameObject.SetActive(false);
                deactivatedCount++;
            }
            else
            {
                DebugLog($"Could not find consumable with ID '{id}' to restore inactive state");
            }
        }

        // The set is now established from the save - don't randomize again this game
        hasAppliedDifficulty = true;

        DebugLog($"Loaded state: deactivated {deactivatedCount}/{inactiveOptionalConsumableIDs.Count} saved-inactive consumables");
    }

    #endregion

    #region Debug

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            GameLog.Log($"[ConsumableManager] {message}");
        }
    }

    #endregion
}
