using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Renderer))]

public class SafeDialGlowVisual : MonoBehaviour
{
    #region Variables

    [Header("Glow Settings")]
    public Color hoverColor = Color.cyan;
    public Color selectColor = Color.green;
    public float glowIntensity = 2f;
    public float lerpSpeed = 5f;

    [Header("References")]
    [SerializeField] private XRGrabInteractable grabInteractable;

    private Renderer objectRenderer;
    private MaterialPropertyBlock propBlock;
    private Color currentTargetColor = Color.black;
    private Color initialEmissionColor;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    #endregion

    #region Unity Events

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        if (grabInteractable == null)
        {
            Debug.LogError("[SafeDialGlowVisual] Missing XRGrabInteractable! Glow feedback won't work.");
            enabled = false;
            return;
        }

        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);

        // Force Emission setup properly
        if (objectRenderer.material != null)
        {
            Material mat = objectRenderer.material;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor(EmissionColorID, Color.black);
            mat.color = Color.black; // << IMPORTANT: Force base color invisible
        }

        initialEmissionColor = Color.black;
        SetEmissionColor(initialEmissionColor); // Hide at start
    }

    private void Update()
    {
        if (objectRenderer == null)
            return;

        objectRenderer.GetPropertyBlock(propBlock);
        Color currentColor = propBlock.GetColor(EmissionColorID);
        Color targetColor = currentTargetColor * Mathf.LinearToGammaSpace(glowIntensity);

        Color lerpedColor = Color.Lerp(currentColor, targetColor, lerpSpeed * Time.deltaTime);
        propBlock.SetColor(EmissionColorID, lerpedColor);
        objectRenderer.SetPropertyBlock(propBlock);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    #endregion

    #region Interaction Callbacks

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        currentTargetColor = hoverColor;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        currentTargetColor = Color.black;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        currentTargetColor = selectColor;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        currentTargetColor = hoverColor; // Go back to hover color after release
    }

    #endregion

    #region Helper Methods

    private void SetEmissionColor(Color color)
    {
        if (objectRenderer == null)
            return;

        objectRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(EmissionColorID, color * Mathf.LinearToGammaSpace(glowIntensity));
        objectRenderer.SetPropertyBlock(propBlock);
    }

    #endregion
}