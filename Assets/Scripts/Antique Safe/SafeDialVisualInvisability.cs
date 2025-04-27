using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDialVisualInvisability : MonoBehaviour
{
    #region Variables

    [Header("References")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private XRGrabInteractable grabInteractable;

    [Header("Invisibility Settings")]
    [SerializeField] private Color fullyTransparentColor = new Color(1, 1, 1, 0f); // Fully invisible white
    private MaterialPropertyBlock propertyBlock;

    #endregion

    #region Unity Events

    private void Start()
    {
        if (targetRenderer == null || grabInteractable == null)
        {
            Debug.LogError("[SafeDialVisual_Invisibility] Missing Target Renderer or Grab Interactable!");
            return;
        }

        propertyBlock = new MaterialPropertyBlock();
        ApplyInvisibility(); // Set invisible immediately

        // Listen to hover events
        grabInteractable.hoverEntered.AddListener(_ => ReleaseMaterial());
        grabInteractable.hoverExited.AddListener(_ => ApplyInvisibility());

        // Cache the interaction manager safely
        var manager = grabInteractable.interactionManager;
        if (manager != null)
        {
            grabInteractable.selectExited.AddListener(_ =>
            {
                ApplyInvisibility();
                targetRenderer.SetPropertyBlock(null);
                // Use the IXRSelectInteractable overload 
                manager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);
            });
        }
        else
        {
            grabInteractable.selectExited.AddListener(_ =>
            {
                ApplyInvisibility();
                targetRenderer.SetPropertyBlock(null);
            });
        }
    }

    #endregion

    #region Custom Methods

    private void ApplyInvisibility()
    {
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", fullyTransparentColor);
        propertyBlock.SetColor("_EmissionColor", Color.black);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ReleaseMaterial()
    {
        targetRenderer.SetPropertyBlock(null); // Allow SafeDialGlowVisual to animate colors normally
    }

    #endregion
}