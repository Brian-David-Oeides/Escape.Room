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
    [SerializeField] private string drawerID = "drawer_main";

    private Rigidbody rb;
    private Vector3 initialLocalPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialLocalPosition = transform.localPosition;
    }

    void FixedUpdate()
    {
        Vector3 localPos = transform.localPosition;
        float localZ = localPos.z - initialLocalPosition.z;
        float clampedZ = Mathf.Clamp(localZ, minLocalZ, maxLocalZ);
        transform.localPosition = new Vector3(localPos.x, localPos.y, initialLocalPosition.z + clampedZ);
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

        Debug.Log($"[DrawerClamp] Saved state for {drawerID}: localZ={transform.localPosition.z:F3}");
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

            Debug.Log($"[DrawerClamp] Loaded state for {drawerID}: localZ={transform.localPosition.z:F3}");
        }
        else
        {
            Debug.Log($"[DrawerClamp] No saved state found for {drawerID} - using defaults");
        }
    }

    #endregion
}

