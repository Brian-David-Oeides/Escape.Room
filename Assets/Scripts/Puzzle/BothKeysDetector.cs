using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Detects when both bronze keys are inserted into Lock 1 and Lock 2 sockets
/// Fires UnityEvent when both keys are in place
/// Implements ISaveable to persist socket states
/// </summary>
/// 

public class BothKeysDetector : MonoBehaviour, ISaveable
{
    [Header("Socket References")]
    [Tooltip("XRSocketInteractor for Lock 1")]
    [SerializeField] private XRSocketInteractor lock1Socket;

    [Tooltip("XRSocketInteractor for Lock 2")]
    [SerializeField] private XRSocketInteractor lock2Socket;

    [Header("Key GameObjects")]
    [Tooltip("Bronze Key (Lock 1) GameObject - will be disabled when in socket")]
    [SerializeField] private GameObject bronzeKey1;

    [Tooltip("Bronze Key (Lock 2) GameObject - will be disabled when in socket")]
    [SerializeField] private GameObject bronzeKey2;

    [Header("Lock Visual GameObjects")]
    [Tooltip("Lock 1 GameObject - disabled when key is inserted")]
    [SerializeField] private GameObject lock1Visual;

    [Tooltip("Lock 2 GameObject - disabled when key is inserted")]
    [SerializeField] private GameObject lock2Visual;

    [Tooltip("LockSocket 1 GameObject - disabled when key is inserted")]
    [SerializeField] private GameObject lockSocket1Visual;

    [Tooltip("LockSocket 2 GameObject - disabled when key is inserted")]
    [SerializeField] private GameObject lockSocket2Visual;

    [Header("Save System")]
    [SerializeField] private string puzzleID = "bothkeys_safe_001";

    [Header("Events")]
    [Tooltip("Fired when both keys are inserted into their sockets")]
    public UnityEvent OnBothKeysInserted;

    [Tooltip("Fired when one or both keys are removed (optional - for re-locking)")]
    public UnityEvent OnKeyRemoved;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // State tracking
    private bool _lock1HasKey = false;
    private bool _lock2HasKey = false;
    private bool _dialUnlocked = false;  // Once true, stays true forever

    #region Unity Lifecycle

    private void Start()
    {
        // Validate references
        if (lock1Socket == null || lock2Socket == null)
        {
            GameLog.LogError("[BothKeysDetector] Missing socket references! Please assign both Lock 1 and Lock 2 sockets.");
            enabled = false;
            return;
        }

        if (bronzeKey1 == null || bronzeKey2 == null)
        {
            GameLog.LogError("[BothKeysDetector] Missing Bronze Key GameObject references!");
            enabled = false;
            return;
        }

        // Subscribe to socket events
        lock1Socket.selectEntered.AddListener(OnLock1KeyInserted);
        lock1Socket.selectExited.AddListener(OnLock1KeyRemoved);

        lock2Socket.selectEntered.AddListener(OnLock2KeyInserted);
        lock2Socket.selectExited.AddListener(OnLock2KeyRemoved);

        DebugLog("BothKeysDetector initialized - waiting for keys...");
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        if (lock1Socket != null)
        {
            lock1Socket.selectEntered.RemoveListener(OnLock1KeyInserted);
            lock1Socket.selectExited.RemoveListener(OnLock1KeyRemoved);
        }

        if (lock2Socket != null)
        {
            lock2Socket.selectEntered.RemoveListener(OnLock2KeyInserted);
            lock2Socket.selectExited.RemoveListener(OnLock2KeyRemoved);
        }
    }

    #endregion

    #region Socket Event Handlers

    private void OnLock1KeyInserted(SelectEnterEventArgs args)
    {
        _lock1HasKey = true;
        DebugLog($"Lock 1: Key inserted ({args.interactableObject.transform.name})");
        PuzzleManager.Instance?.RegisterPuzzleCompletion("bronzeKey_lock1_001");
        CheckBothKeys();
    }

    private void OnLock1KeyRemoved(SelectExitEventArgs args)
    {
        _lock1HasKey = false;
        DebugLog("Lock 1: Key removed");

        // Only fire OnKeyRemoved if dial hasn't been permanently unlocked
        if (!_dialUnlocked)
        {
            OnKeyRemoved?.Invoke();
            DebugLog("Key removed - OnKeyRemoved fired");
        }
    }

    private void OnLock2KeyInserted(SelectEnterEventArgs args)
    {
        _lock2HasKey = true;
        DebugLog($"Lock 2: Key inserted ({args.interactableObject.transform.name})");
        PuzzleManager.Instance?.RegisterPuzzleCompletion("bronzeKey_lock2_001");
        CheckBothKeys();
    }

    private void OnLock2KeyRemoved(SelectExitEventArgs args)
    {
        _lock2HasKey = false;
        DebugLog("Lock 2: Key removed");

        // Only fire OnKeyRemoved if dial hasn't been permanently unlocked
        if (!_dialUnlocked)
        {
            OnKeyRemoved?.Invoke();
            DebugLog("Key removed - OnKeyRemoved fired");
        }
    }

    #endregion

    #region Key Detection Logic

    private void CheckBothKeys()
    {
        // If both keys are inserted AND dial hasn't been unlocked yet
        if (_lock1HasKey && _lock2HasKey && !_dialUnlocked)
        {
            _dialUnlocked = true;  // Permanent unlock
            DebugLog("✓ BOTH KEYS INSERTED - Firing OnBothKeysInserted event! Dial permanently unlocked.");
            PuzzleManager.Instance?.RegisterPuzzleCompletion(puzzleID);
            OnBothKeysInserted?.Invoke();
        }
    }

    #endregion

    #region ISaveable Implementation

    public string SaveID => puzzleID;

    public void SaveState(SaveData saveData)
    {
        // Create a MoveableObjectData entry to store our state
        // We'll use customData to store our three booleans as a simple string
        string customData = $"{_lock1HasKey}|{_lock2HasKey}|{_dialUnlocked}";

        MoveableObjectData objectState = new MoveableObjectData(
            puzzleID,
            transform.position,
            transform.rotation,
            gameObject.activeSelf,
            customData
        );

        saveData.moveableObjects.Add(objectState);

        DebugLog($"State saved: Lock1={_lock1HasKey}, Lock2={_lock2HasKey}, DialUnlocked={_dialUnlocked}");
    }

    public void LoadState(SaveData saveData)
    {
        // Find this object's saved state
        MoveableObjectData savedState = saveData.moveableObjects.Find(obj => obj.objectID == puzzleID);

        if (savedState != null && !string.IsNullOrEmpty(savedState.customData))
        {
            // Parse the customData string
            string[] parts = savedState.customData.Split('|');

            if (parts.Length == 3)
            {
                bool.TryParse(parts[0], out _lock1HasKey);
                bool.TryParse(parts[1], out _lock2HasKey);
                bool.TryParse(parts[2], out _dialUnlocked);

                DebugLog($"State loaded: Lock1={_lock1HasKey}, Lock2={_lock2HasKey}, DialUnlocked={_dialUnlocked}");

                // Restore visual state based on loaded data
                RestoreSocketStates();

                // If dial was already unlocked, fire the event again
                if (_dialUnlocked)
                {
                    DebugLog("Dial was already unlocked - firing OnBothKeysInserted event");
                    OnBothKeysInserted?.Invoke();
                }
            }
        }
        else
        {
            DebugLog("No saved state found - using defaults");
        }
    }

    private void RestoreSocketStates()
    {
        // If Lock 1 had a key when saved, disable the visuals
        if (_lock1HasKey)
        {
            if (bronzeKey1 != null)
            {
                bronzeKey1.SetActive(false);
                DebugLog("Lock 1 had key - disabled Bronze Key 1 GameObject");
            }
            if (lock1Visual != null)
            {
                lock1Visual.SetActive(false);
                DebugLog("Lock 1 had key - disabled Lock 1 visual");
            }
            if (lockSocket1Visual != null)
            {
                lockSocket1Visual.SetActive(false);
                DebugLog("Lock 1 had key - disabled LockSocket 1 visual");
            }
        }

        // If Lock 2 had a key when saved, disable the visuals
        if (_lock2HasKey)
        {
            if (bronzeKey2 != null)
            {
                bronzeKey2.SetActive(false);
                DebugLog("Lock 2 had key - disabled Bronze Key 2 GameObject");
            }
            if (lock2Visual != null)
            {
                lock2Visual.SetActive(false);
                DebugLog("Lock 2 had key - disabled Lock 2 visual");
            }
            if (lockSocket2Visual != null)
            {
                lockSocket2Visual.SetActive(false);
                DebugLog("Lock 2 had key - disabled LockSocket 2 visual");
            }
        }
    }

    #endregion

    #region Debug Helpers

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            GameLog.Log($"[BothKeysDetector] {message}");
        }
    }

    // Public getters for debugging/UI
    public bool Lock1HasKey => _lock1HasKey;
    public bool Lock2HasKey => _lock2HasKey;
    public bool DialUnlocked => _dialUnlocked;

    #endregion
}
