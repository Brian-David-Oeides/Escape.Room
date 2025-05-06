using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]

public class SafeDialHoverVisual : MonoBehaviour
{
    #region Variables

    [Header("Visual Settings")]
    [SerializeField] private Color hoverColor = new Color(0f, 0.8f, 1f, 1f);
    [SerializeField] private Color selectColor = new Color(0f, 0.9f, 1f, 1f);
    [SerializeField] private Color baseColor = new Color(0f, 0f, 0f, 0f);

    [Header("Transition Settings")]
    [SerializeField] private float fadeSpeed = 5f;

    [Header("References")]
    [SerializeField] private List<Renderer> tintRenderers;
    [SerializeField] private XRBaseInteractable interactable;

    private MaterialPropertyBlock propertyBlock;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private Color currentColor;
    private Color targetColor;

    #endregion

    #region Unity Events

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        propertyBlock = new MaterialPropertyBlock();
        currentColor = baseColor;
        targetColor = baseColor;

        if (interactable != null)
        {
            interactable.firstHoverEntered.AddListener(OnHoverEntered);
            interactable.lastHoverExited.AddListener(OnHoverExited);
            interactable.firstSelectEntered.AddListener(OnSelectEntered);
            interactable.lastSelectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.firstHoverEntered.RemoveListener(OnHoverEntered);
            interactable.lastHoverExited.RemoveListener(OnHoverExited);
            interactable.firstSelectEntered.RemoveListener(OnSelectEntered);
            interactable.lastSelectExited.RemoveListener(OnSelectExited);
        }
    }

    private void Update()
    {
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * fadeSpeed);
        ApplyColorToRenderers(currentColor);
    }

    #endregion

    #region Hover & Selection Callbacks

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        targetColor = hoverColor;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        targetColor = baseColor;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        targetColor = selectColor;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        targetColor = baseColor;
    }

    #endregion

    #region Custom Methods

    private void ApplyColorToRenderers(Color color)
    {
        foreach (var rend in tintRenderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColor, color);
            propertyBlock.SetColor(EmissionColor, color);
            rend.SetPropertyBlock(propertyBlock);
        }
    }

    #endregion
}

