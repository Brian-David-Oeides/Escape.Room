using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Test script for Save System - Use keyboard shortcuts to test save/load
/// Attach this to any GameObject in the scene during testing
/// </summary>

#if UNITY_EDITOR
public class SaveSystemTester : MonoBehaviour
{
    [Header("Test Hotkeys")]
    [Tooltip("Press this key to save to slot 1")]
    public KeyCode saveKey = KeyCode.F1;

    [Tooltip("Press this key to load from slot 1")]
    public KeyCode loadKey = KeyCode.F2;

    [Tooltip("Press this key to check slot 1 info")]
    public KeyCode checkSlotKey = KeyCode.F3;

    [Tooltip("Press this key to delete slot 1")]
    public KeyCode deleteSlotKey = KeyCode.F4;

    [Header("Test Settings")]
    [Tooltip("Which save slot to use for testing (1-3)")]
    [Range(1, 3)]
    public int testSlot = 1;

    void Start()
    {
        // Verify SaveManager exists
        if (SaveManager.Instance != null)
        {
            GameLog.Log("✅ SaveManager is ready!");
            GameLog.Log($"📋 Test Controls:");
            GameLog.Log($"   {saveKey} = Save to Slot {testSlot}");
            GameLog.Log($"   {loadKey} = Load from Slot {testSlot}");
            GameLog.Log($"   {checkSlotKey} = Check Slot {testSlot} Info");
            GameLog.Log($"   {deleteSlotKey} = Delete Slot {testSlot}");

            // Show exact save location
            GameLog.Log($"📁 Save files location: {Application.persistentDataPath}\\Saves\\");
            GameLog.Log($"📁 Save folder path: {Application.persistentDataPath}");
            GameLog.Log($"💡 TIP: Copy the path above, press Win+R, paste it, and press Enter to open the folder!");
        }
        else
        {
            GameLog.LogError("❌ SaveManager not found! Make sure SaveManager GameObject exists in the scene.");
        }
    }

    void Update()
    {
        if (SaveManager.Instance == null) return;

        // SAVE - F1 (or custom key)
        if (Input.GetKeyDown(saveKey))
        {
            GameLog.Log($"💾 Saving game to slot {testSlot}...");
            bool success = SaveManager.Instance.SaveGame(testSlot);

            if (success)
            {
                GameLog.Log($"✅ Game saved successfully to slot {testSlot}!");
            }
            else
            {
                GameLog.LogError($"❌ Failed to save to slot {testSlot}");
            }
        }

        // LOAD - F2 (or custom key)
        if (Input.GetKeyDown(loadKey))
        {
            GameLog.Log($"📂 Loading game from slot {testSlot}...");
            bool success = SaveManager.Instance.LoadGame(testSlot);

            if (success)
            {
                GameLog.Log($"✅ Game loaded successfully from slot {testSlot}!");
            }
            else
            {
                GameLog.LogWarning($"⚠️ No save found in slot {testSlot} or load failed");
            }
        }

        // CHECK SLOT INFO - F3 (or custom key)
        if (Input.GetKeyDown(checkSlotKey))
        {
            GameLog.Log($"ℹ️ Checking slot {testSlot} info...");
            SaveSlotInfo info = SaveManager.Instance.GetSaveSlotInfo(testSlot);

            if (info.hasData)
            {
                GameLog.Log($"📊 Slot {testSlot} Info:");
                GameLog.Log($"   Save Name: {info.saveName}");
                GameLog.Log($"   Timestamp: {info.saveTimestamp}");
                GameLog.Log($"   Playtime: {info.playtime:F1}s ({info.playtime / 60:F1} minutes)");
                GameLog.Log($"   Puzzles Solved: {info.puzzlesSolved}");
                GameLog.Log($"   Scene: {info.sceneName}");
            }
            else
            {
                GameLog.Log($"📭 Slot {testSlot} is empty");
            }
        }

        // DELETE SLOT - F4 (or custom key)
        if (Input.GetKeyDown(deleteSlotKey))
        {
            if (SaveManager.Instance.SaveSlotExists(testSlot))
            {
                GameLog.Log($"🗑️ Deleting save slot {testSlot}...");
                bool success = SaveManager.Instance.DeleteSave(testSlot);

                if (success)
                {
                    GameLog.Log($"✅ Slot {testSlot} deleted successfully");
                }
                else
                {
                    GameLog.LogError($"❌ Failed to delete slot {testSlot}");
                }
            }
            else
            {
                GameLog.Log($"⚠️ Slot {testSlot} is already empty");
            }
        }
    }

    void OnGUI()
    {
        // Optional: Display controls on screen
        if (SaveManager.Instance == null) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        string controlsText = $"SAVE SYSTEM TEST CONTROLS:\n" +
                             $"{saveKey} = Save | {loadKey} = Load | {checkSlotKey} = Info | {deleteSlotKey} = Delete\n" +
                             $"Testing with Slot {testSlot}";

        GUI.Label(new Rect(10, 10, 500, 80), controlsText, style);
    }
}
#endif