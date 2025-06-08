using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DocumentCarousel : MonoBehaviour
{
    [System.Serializable]
    public class DocumentPage
    {
        public Sprite pageImage;
        [TextArea(3, 10)]
        public string pageText;
        public string pageTitle;
    }

    [Header("Document Pages")]
    public List<DocumentPage> pages = new List<DocumentPage>();

    [Header("UI References")]
    public Image pageImageComponent;
    public TextMeshProUGUI pageTextComponent;
    public TextMeshProUGUI pageTitleComponent;
    public TextMeshProUGUI pageCounterComponent;
    public GameObject navigationHelpUI;
    public TextMeshProUGUI leftArrowText;
    public TextMeshProUGUI rightArrowText;

    [Header("Animation Settings")]
    public float slideDistance = 200f;
    public float slideSpeed = 8f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    public AudioClip pageFlipSound;

    [Header("Events")]
    public UnityEvent<int> onPageChanged;

    #region Private Variables

    private int _currentPageIndex = 0;
    private bool _isSliding = false;
    private float _slideTimer = 0f;
    private Vector3 _slideStartPos;
    private Vector3 _slideTargetPos;
    private bool _slidingRight = true;
    private AudioSource _audioSource;
    private bool _isActive = false;

    #endregion

    #region Unity Events

    private void Awake()
    {
        SetupAudioSource();

        if (navigationHelpUI != null)
            navigationHelpUI.SetActive(false);

        if (pages.Count == 0)
        {
            Debug.LogWarning("No pages assigned to DocumentCarousel on " + gameObject.name);
        }
    }

    private void Update()
    {
        HandlePageSliding();

        if (_isActive)
        {
            CheckNavigationInputs();
        }
    }

    #endregion

    #region Setup

    private void SetupAudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.spatialBlend = 1.0f;
        _audioSource.playOnAwake = false;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.maxDistance = 10f;
    }

    #endregion

    #region Carousel Control

    public void ActivateCarousel()
    {
        _isActive = true;
        _currentPageIndex = 0;
        UpdatePageContent();
        ShowNavigationHelp();

        Debug.Log("Carousel activated");
    }

    public void DeactivateCarousel()
    {
        _isActive = false;
        HideNavigationHelp();

        Debug.Log("Carousel deactivated");
    }

    #endregion

    #region Navigation

    private bool _rightTriggerWasPressed = false;
    private bool _leftTriggerWasPressed = false;

    private void CheckNavigationInputs()
    {
        if (_isSliding || pages.Count <= 1) return;

        // Right trigger for next page
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand)
            .TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float rightTriggerValue))
        {
            bool rightTriggerPressed = rightTriggerValue > 0.8f; // Trigger threshold

            // Only trigger on press (not hold)
            if (rightTriggerPressed && !_rightTriggerWasPressed)
            {
                // Navigate to next page (cycles through pages)
                NavigateToPage(_currentPageIndex + 1, true);
                FlashArrow(rightArrowText);

                Debug.Log("Right trigger pressed - navigating to next page");
            }

            _rightTriggerWasPressed = rightTriggerPressed;
        }

        // Left trigger for previous page
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand)
            .TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float leftTriggerValue))
        {
            bool leftTriggerPressed = leftTriggerValue > 0.8f; // Trigger threshold

            // Only trigger on press (not hold)
            if (leftTriggerPressed && !_leftTriggerWasPressed)
            {
                // Navigate to previous page (cycles backwards)
                NavigateToPage(_currentPageIndex - 1, false);
                FlashArrow(leftArrowText);

                Debug.Log("Left trigger pressed - navigating to previous page");
            }

            _leftTriggerWasPressed = leftTriggerPressed;
        }
    }

    private void NavigateToPage(int targetPageIndex, bool slideRight)
    {
        if (_isSliding || pages.Count <= 1) return;

        // Wrap around pages
        if (targetPageIndex < 0)
            targetPageIndex = pages.Count - 1;
        else if (targetPageIndex >= pages.Count)
            targetPageIndex = 0;

        if (targetPageIndex == _currentPageIndex) return;

        _currentPageIndex = targetPageIndex;
        StartPageSlideAnimation(slideRight);

        PlaySound(pageFlipSound);
        onPageChanged?.Invoke(_currentPageIndex);

        Debug.Log($"Navigated to page {_currentPageIndex + 1}");
    }

    #endregion

    #region Page Content

    private void UpdatePageContent()
    {
        if (pages.Count == 0 || _currentPageIndex >= pages.Count) return;

        DocumentPage currentPage = pages[_currentPageIndex];

        // Update image
        if (pageImageComponent != null && currentPage.pageImage != null)
            pageImageComponent.sprite = currentPage.pageImage;

        // Update text
        if (pageTextComponent != null)
            pageTextComponent.text = currentPage.pageText;

        // Update title
        if (pageTitleComponent != null)
            pageTitleComponent.text = currentPage.pageTitle;

        // Update page counter
        if (pageCounterComponent != null)
            pageCounterComponent.text = $"Page {_currentPageIndex + 1} of {pages.Count}";

        // Update arrow visibility
        UpdateArrowVisibility();
    }

    #endregion

    #region UI Management

    private void ShowNavigationHelp()
    {
        if (navigationHelpUI != null)
        {
            navigationHelpUI.SetActive(true);
            UpdateArrowVisibility();
        }
    }

    private void HideNavigationHelp()
    {
        if (navigationHelpUI != null)
            navigationHelpUI.SetActive(false);
    }

    private void UpdateArrowVisibility()
    {
        if (pages.Count <= 1)
        {
            // Hide both arrows if only one page or no pages
            if (leftArrowText != null)
                leftArrowText.gameObject.SetActive(false);
            if (rightArrowText != null)
                rightArrowText.gameObject.SetActive(false);
            return;
        }

        // Show both arrows for wrap-around navigation
        if (leftArrowText != null)
            leftArrowText.gameObject.SetActive(true);
        if (rightArrowText != null)
            rightArrowText.gameObject.SetActive(true);
    }

    #endregion

    #region Animation

    private void StartPageSlideAnimation(bool slideRight)
    {
        if (pageImageComponent == null) return;

        _isSliding = true;
        _slideTimer = 0f;
        _slidingRight = slideRight;

        _slideStartPos = pageImageComponent.rectTransform.anchoredPosition;
        _slideTargetPos = _slideStartPos + Vector3.right * (slideRight ? slideDistance : -slideDistance);
    }

    private void HandlePageSliding()
    {
        if (!_isSliding || pageImageComponent == null) return;

        _slideTimer += Time.deltaTime * slideSpeed;
        float normalizedTime = Mathf.Clamp01(_slideTimer);
        float curveValue = slideCurve.Evaluate(normalizedTime);

        if (normalizedTime < 0.5f)
        {
            // Slide out
            Vector3 currentPos = Vector3.Lerp(_slideStartPos, _slideTargetPos, curveValue * 2f);
            pageImageComponent.rectTransform.anchoredPosition = currentPos;
        }
        else
        {
            // Update content at halfway point
            if (normalizedTime >= 0.5f && normalizedTime < 0.6f)
            {
                UpdatePageContent();

                // Set position to opposite side for slide in
                Vector3 oppositePos = _slideStartPos + Vector3.right * (_slidingRight ? -slideDistance : slideDistance);
                pageImageComponent.rectTransform.anchoredPosition = oppositePos;
            }

            // Slide in
            Vector3 oppositeStart = _slideStartPos + Vector3.right * (_slidingRight ? -slideDistance : slideDistance);
            Vector3 currentPos = Vector3.Lerp(oppositeStart, _slideStartPos, (curveValue - 0.5f) * 2f);
            pageImageComponent.rectTransform.anchoredPosition = currentPos;
        }

        if (normalizedTime >= 1f)
        {
            _isSliding = false;
            pageImageComponent.rectTransform.anchoredPosition = _slideStartPos;
        }
    }

    #endregion

    #region Visual Feedback

    private void FlashArrow(TextMeshProUGUI arrowText)
    {
        if (arrowText != null)
        {
            StartCoroutine(FlashArrowCoroutine(arrowText));
        }
    }

    private IEnumerator FlashArrowCoroutine(TextMeshProUGUI arrowText)
    {
        Color originalColor = arrowText.color;

        // Flash brighter/different color
        arrowText.color = Color.yellow;
        yield return new WaitForSeconds(0.15f);

        // Return to original
        arrowText.color = originalColor;
    }

    #endregion

    #region Audio

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
            _audioSource.PlayOneShot(clip);
    }

    #endregion

    #region Public Methods

    public void GoToPage(int pageIndex)
    {
        if (pageIndex >= 0 && pageIndex < pages.Count && pageIndex != _currentPageIndex)
        {
            bool slideRight = pageIndex > _currentPageIndex;
            NavigateToPage(pageIndex, slideRight);
        }
    }

    public int GetCurrentPageIndex()
    {
        return _currentPageIndex;
    }

    public int GetTotalPages()
    {
        return pages.Count;
    }

    public bool IsActive()
    {
        return _isActive;
    }

    #endregion
}