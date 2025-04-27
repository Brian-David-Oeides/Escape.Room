using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDialHoverFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer dialRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.cyan;
    [SerializeField] private Color selectedColor = Color.green;

    private XRGrabInteractable grabInteractable;
    private Material dialMaterial;
    private Color originalEmissionColor;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("[SafeDialHoverFeedback] Missing XRGrabInteractable component!");
        }

        if (dialRenderer != null)
        {
            dialMaterial = dialRenderer.material;
            originalEmissionColor = dialMaterial.GetColor("_EmissionColor");
        }
        else
        {
            Debug.LogError("[SafeDialHoverFeedback] Missing MeshRenderer reference!");
        }

        // Subscribe to events
        grabInteractable.hoverEntered.AddListener(OnHoverEnter);
        grabInteractable.hoverExited.AddListener(OnHoverExit);
        grabInteractable.selectEntered.AddListener(OnSelectEnter);
        grabInteractable.selectExited.AddListener(OnSelectExit);
    }

    private void OnDestroy()
    {
        // Clean up events
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEnter);
            grabInteractable.hoverExited.RemoveListener(OnHoverExit);
            grabInteractable.selectEntered.RemoveListener(OnSelectEnter);
            grabInteractable.selectExited.RemoveListener(OnSelectExit);
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (dialMaterial != null)
        {
            dialMaterial.SetColor("_EmissionColor", hoverColor);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (dialMaterial != null)
        {
            dialMaterial.SetColor("_EmissionColor", originalEmissionColor);
        }
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        if (dialMaterial != null)
        {
            dialMaterial.SetColor("_EmissionColor", selectedColor);
        }
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        if (dialMaterial != null)
        {
            dialMaterial.SetColor("_EmissionColor", originalEmissionColor);
        }
    }
}

