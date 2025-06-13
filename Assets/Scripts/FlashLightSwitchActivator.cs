using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashLightSwitchActivator : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float detectionRange = 10f;
    public LayerMask detectionLayer = -1;

    [Header("References")]
    public Light torchLight;
    public Transform raycastOrigin;

    [Header("Switch Reference")]
    public GameObject targetSwitchObject; // Direct reference to the ToggleSwitchOnOff05 GameObject

    private bool _switchAlreadyActivated = false;

    private void Update()
    {
        if (torchLight != null && torchLight.enabled && !_switchAlreadyActivated)
        {
            PerformSwitchDetection();
        }
    }

    private void PerformSwitchDetection()
    {
        Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
        RaycastHit hit;

        Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * detectionRange, Color.red);

        if (Physics.Raycast(ray, out hit, detectionRange, detectionLayer))
        {
            //Debug.Log($"Raycast hit: {hit.collider.name}");

            if (hit.collider.CompareTag("Switch") || hit.collider.name.Contains("PlugAndSwitch"))
            {
                Debug.Log("Switch object detected!");
                EnableSwitchGameObject();
            }
        }
    }

    private void EnableSwitchGameObject()
    {
        if (targetSwitchObject != null && !targetSwitchObject.activeInHierarchy)
        {
            targetSwitchObject.SetActive(true);
            _switchAlreadyActivated = true;
            Debug.Log("ToggleSwitchOnOff05 GameObject has been enabled!");
        }
        else if (targetSwitchObject == null)
        {
            Debug.Log("Target Switch GameObject reference is null!");
        }
        else
        {
            Debug.Log("Target Switch GameObject is already active!");
        }
    }
}
