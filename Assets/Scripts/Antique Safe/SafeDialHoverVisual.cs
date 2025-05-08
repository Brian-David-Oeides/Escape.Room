using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]

public class SafeDialHoverVisual : MonoBehaviour
{
    #region Variables

    [Header("Visual Settings")]
    [SerializeField] private Color _hoverColor = new Color(0f, 0.8f, 1f, 1f);
    [SerializeField] private Color _selectColor = new Color(0f, 0.9f, 1f, 1f);
    [SerializeField] private Color _baseColor = new Color(0f, 0f, 0f, 0f);

    [Header("Transition Settings")]
    [SerializeField] private float _fadeSpeed = 5f;

    [Header("References")]
    [SerializeField] private List<Renderer> _tintRenderers;
    [SerializeField] private XRBaseInteractable _interactable;

    private MaterialPropertyBlock _propertyBlock;
    private static readonly int _EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int _BaseColor = Shader.PropertyToID("_BaseColor");

    private Color _currentColor;
    private Color _targetColor;

    #endregion

    #region Unity Events

    private void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
        _propertyBlock = new MaterialPropertyBlock();
        _currentColor = _baseColor;
        _targetColor = _baseColor;

        if (_interactable != null)
        {
            _interactable.firstHoverEntered.AddListener(OnHoverEntered);
            _interactable.lastHoverExited.AddListener(OnHoverExited);
            _interactable.firstSelectEntered.AddListener(OnSelectEntered);
            _interactable.lastSelectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDestroy()
    {
        if (_interactable != null)
        {
            _interactable.firstHoverEntered.RemoveListener(OnHoverEntered);
            _interactable.lastHoverExited.RemoveListener(OnHoverExited);
            _interactable.firstSelectEntered.RemoveListener(OnSelectEntered);
            _interactable.lastSelectExited.RemoveListener(OnSelectExited);
        }
    }

    private void Update()
    {
        _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * _fadeSpeed);
        ApplyColorToRenderers(_currentColor);
    }

    #endregion

    #region Hover & Selection Callbacks

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        _targetColor = _hoverColor;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        _targetColor = _baseColor;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        _targetColor = _selectColor;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        _targetColor = _baseColor;
    }

    #endregion

    #region Custom Methods

    private void ApplyColorToRenderers(Color color)
    {
        foreach (var rend in _tintRenderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_BaseColor, color);
            _propertyBlock.SetColor(_EmissionColor, color);
            rend.SetPropertyBlock(_propertyBlock);
        }
    }

    #endregion
}