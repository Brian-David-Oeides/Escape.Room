using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DynamicAttachPoint : MonoBehaviour
{
    private XRDirectInteractor directInteractor;
    private XRRayInteractor rayInteractor;
    private Transform pinchAttachPoint;
    private Transform defaultAttachPoint;
    private Transform thumbTip;
    private Transform indexTip;

    private void Start()
    {
        directInteractor = GetComponentInChildren<XRDirectInteractor>();
        rayInteractor = GetComponentInChildren<XRRayInteractor>();

        // Store the original default attach points
        if (directInteractor != null)
        {
            defaultAttachPoint = directInteractor.attachTransform;
        }

        StartCoroutine(SetupPinchAttachPoint());

        // Subscribe to selection events
        if (directInteractor != null)
        {
            directInteractor.selectEntered.AddListener(OnObjectGrabbed);
            directInteractor.selectExited.AddListener(OnObjectReleased);
        }

        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.AddListener(OnObjectGrabbed);
            rayInteractor.selectExited.AddListener(OnObjectReleased);
        }
    }

    private IEnumerator SetupPinchAttachPoint()
    {
        yield return new WaitForSeconds(0.5f);

        // Determine if this is left or right hand
        bool isLeftHand = gameObject.name.ToLower().Contains("left");
        string thumbBoneName = isLeftHand ? "hands:b_l_thumb_ignore" : "hands:b_r_thumb_ignore";
        string indexBoneName = isLeftHand ? "hands:b_l_index_ignore" : "hands:b_r_index_ignore";

        // Find the fingertip bones
        thumbTip = FindBoneByName(thumbBoneName);
        indexTip = FindBoneByName(indexBoneName);

        if (thumbTip != null && indexTip != null)
        {
            // Create the PinchAttachPoint
            GameObject attachObj = new GameObject("PinchAttachPoint_Dynamic");
            pinchAttachPoint = attachObj.transform;
            pinchAttachPoint.SetParent(transform);

            // Initial positioning
            UpdateAttachPointPosition();

            Debug.Log($"Pinch attach point created for {gameObject.name}. Will be used only for pinchable objects.");
        }
        else
        {
            Debug.LogError($"Could not find fingertip bones on {gameObject.name}");
        }
    }

    private void Update()
    {
        // Continuously update the pinch attach point position
        if (thumbTip != null && indexTip != null && pinchAttachPoint != null)
        {
            UpdateAttachPointPosition();
        }
    }

    private void UpdateAttachPointPosition()
    {
        pinchAttachPoint.position = (thumbTip.position + indexTip.position) / 2f;
        pinchAttachPoint.rotation = indexTip.rotation;
    }

    private void OnObjectGrabbed(SelectEnterEventArgs args)
    {
        // Check if the grabbed object is pinchable
        XRBaseInteractable interactable = args.interactableObject as XRBaseInteractable;
        bool isPinchable = interactable != null && interactable.GetComponent<PinchableObject>() != null;

        XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;

        if (isPinchable && pinchAttachPoint != null)
        {
            // Use pinch attach point for pinchable objects
            if (interactor != null)
            {
                interactor.attachTransform = pinchAttachPoint;
                Debug.Log($"Using pinch attach for {interactable.name}");
            }
        }
        else
        {
            // Use default attach point for regular objects
            if (interactor != null)
            {
                interactor.attachTransform = defaultAttachPoint;
                Debug.Log($"Using default attach for {interactable.name}");
            }
        }
    }

    private void OnObjectReleased(SelectExitEventArgs args)
    {
        // Reset to default when object is released
        XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;
        if (interactor != null)
        {
            interactor.attachTransform = defaultAttachPoint;
        }
    }

    private Transform FindBoneByName(string boneName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == boneName)
            {
                return child;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (directInteractor != null)
        {
            directInteractor.selectEntered.RemoveListener(OnObjectGrabbed);
            directInteractor.selectExited.RemoveListener(OnObjectReleased);
        }

        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.RemoveListener(OnObjectGrabbed);
            rayInteractor.selectExited.RemoveListener(OnObjectReleased);
        }
    }
}