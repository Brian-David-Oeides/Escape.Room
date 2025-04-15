using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    public GameObject xrOrigin;
    public Transform startPosition;

    void Start()
    {
        // only move player to this position if entering from the main menu or restarting
        if (!GameMode.startFromMenu) // if GameMode.startFromMenu not true
        {
            if (xrOrigin != null && startPosition != null) // if XR Origin exists and start positiion exists in scene
            {   // set XR origin position and rotation to startPosition
                xrOrigin.transform.position = startPosition.position;
                xrOrigin.transform.rotation = startPosition.rotation;
            }
        }
    }
}

