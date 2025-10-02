using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Button))]

public class UIButtonAudioHandler : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Sound Settings")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private bool overrideClickSound = false;
    [SerializeField] private string customClickSoundName; // For special buttons like "confirm" or "cancel"

    [Header("Hover Cooldown")]
    [SerializeField] private float hoverCooldown = 0.5f;

    private Button _button;
    private XRSimpleInteractable _xrInteractable;
    private float _lastHoverTime;
    private bool _isHovering = false;
    private bool _hasPlayedClickThisFrame = false;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _xrInteractable = GetComponent<XRSimpleInteractable>();

        // Add click sound to button's onClick event
        if (_button != null && playClickSound)
        {
            _button.onClick.AddListener(PlayClickSound);
        }

        // Add XR interaction audio
        if (_xrInteractable != null)
        {
            if (playClickSound)
            {
                _xrInteractable.selectEntered.AddListener((args) => PlayClickSoundXR());
            }
            if (playHoverSound)
            {
                _xrInteractable.hoverEntered.AddListener((args) => PlayHoverSoundXR());
                _xrInteractable.hoverExited.AddListener((args) => OnHoverExit());
            }
        }
    }

    private void OnDestroy()
    {
        if (_button != null && playClickSound)
        {
            _button.onClick.RemoveListener(PlayClickSound);
        }

        if (_xrInteractable != null)
        {
            _xrInteractable.selectEntered.RemoveAllListeners();
            _xrInteractable.hoverEntered.RemoveAllListeners();
            _xrInteractable.hoverExited.RemoveAllListeners();
        }
    }

    private void LateUpdate()
    {
        // Reset click flag at end of frame
        _hasPlayedClickThisFrame = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isHovering && playHoverSound && _button != null && _button.interactable)
        {
            if (Time.time - _lastHoverTime > hoverCooldown)
            {
                PlayHoverSound();
                _lastHoverTime = Time.time;
                _isHovering = true;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Button onClick will handle this
    }

    private void OnHoverExit()
    {
        _isHovering = false;
    }

    private void PlayHoverSound()
    {
        UIAudioManager.Instance?.PlayButtonHover();
    }

    private void PlayHoverSoundXR()
    {
        if (!_isHovering && Time.time - _lastHoverTime > hoverCooldown)
        {
            PlayHoverSound();
            _lastHoverTime = Time.time;
            _isHovering = true;
        }
    }

    private void PlayClickSound()
    {
        // Prevent multiple plays in same frame
        if (_hasPlayedClickThisFrame) return;
        _hasPlayedClickThisFrame = true;

        if (UIAudioManager.Instance == null) return;

        if (overrideClickSound && !string.IsNullOrEmpty(customClickSoundName))
        {
            UIAudioManager.Instance.PlaySoundByName(customClickSoundName);
        }
        else
        {
            UIAudioManager.Instance.PlayButtonClick();
        }
    }

    private void PlayClickSoundXR()
    {
        // Prevent duplicate if button.onClick also fires
        if (_hasPlayedClickThisFrame) return;
        PlayClickSound();
    }
}