using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class DrawerClamp : MonoBehaviour, ISaveable
{
    [Tooltip("Minimum Z distance (fully closed) relative to start")]
    public float minLocalZ = 0f;

    [Tooltip("Maximum Z distance (fully open) relative to start")]
    public float maxLocalZ = 0.25f;

    [Header("Save System")]
    [SerializeField] private string drawerID = "";

    private Rigidbody rb;
    private Vector3 initialLocalPosition;
    private bool hasReachedFullyOpen = false;

    void Start()
    {
        // Auto-generate unique ID if not set
        if (string.IsNullOrEmpty(drawerID))
        {
            drawerID = GenerateUniqueID();
            GameLog.Log($"[DrawerClamp] Auto-generated ID: {drawerID}");
        }

        rb = GetComponent<Rigidbody>();
        initialLocalPosition = transform.localPosition;
    }

    void FixedUpdate()
    {
        Vector3 localPos = transform.localPosition;
        float localZ = localPos.z - initialLocalPosition.z;
        float clampedZ = Mathf.Clamp(localZ, minLocalZ, maxLocalZ);
        transform.localPosition = new Vector3(localPos.x, localPos.y, initialLocalPosition.z + clampedZ);

        if (!hasReachedFullyOpen && clampedZ >= maxLocalZ)
        {
            hasReachedFullyOpen = true;
            PuzzleManager.Instance?.RegisterPuzzleCompletion(drawerID);
        }
    }

    /// <summary>
    /// Generate unique ID based on GameObject hierarchy path
    /// </summary>
    private string GenerateUniqueID()
    {
        string path = GetHierarchyPath(transform);
        return $"drawer_{path}".Replace("/", "_").Replace(" ", "_");
    }

    /// <summary>
    /// Get the full hierarchy path of this GameObject
    /// </summary>
    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    #region ISaveable Implementation

    public string SaveID => drawerID;

    public void SaveState(SaveData saveData)
    {
        // Save drawer position using MoveableObjectData
        MoveableObjectData objectState = new MoveableObjectData(
            drawerID,
            transform.position,
            transform.rotation,
            gameObject.activeSelf,
            "" // No custom data needed - position is sufficient
        );

        saveData.moveableObjects.Add(objectState);

        // Save puzzle completion state so PuzzleManager's restored count includes it
        if (hasReachedFullyOpen && !saveData.completedPuzzleIDs.Contains(drawerID))
        {
            saveData.completedPuzzleIDs.Add(drawerID);
        }

        GameLog.Log($"[DrawerClamp] Saved state for {drawerID}: localZ={transform.localPosition.z:F3}");
    }

    public void LoadState(SaveData saveData)
    {
        // Find this drawer's saved state
        MoveableObjectData savedState = saveData.moveableObjects.Find(obj => obj.objectID == drawerID);

        if (savedState != null)
        {
            // Restore position
            transform.position = savedState.position;
            transform.rotation = savedState.rotation;

            GameLog.Log($"[DrawerClamp] Loaded state for {drawerID}: localZ={transform.localPosition.z:F3}");
        }
        else
        {
            GameLog.Log($"[DrawerClamp] No saved state found for {drawerID} - using defaults");
        }
    }

    #endregion
}

