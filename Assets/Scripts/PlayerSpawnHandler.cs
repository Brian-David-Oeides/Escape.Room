using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    public GameObject xrOrigin;
    public Transform startPosition;

    void Start()
    {
        // Player should only be moved if entering from the main menu or restarting
        if (!GameMode.startFromMenu)
        {
            if (xrOrigin != null && startPosition != null)
            {
                xrOrigin.transform.position = startPosition.position;
                xrOrigin.transform.rotation = startPosition.rotation;
            }
        }
    }
}

