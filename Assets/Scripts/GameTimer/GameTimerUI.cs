using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays game timer in VR HUD
/// Shows countdown time with color-coded urgency levels
/// Optional circular progress bar
/// </summary>
/// 

public class GameTimerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private Image timerCircle; // Optional circular progress indicator

    [Header("Colors - Time Remaining")]
    [SerializeField] private Color textColor = Color.black; // Black text for contrast
    [SerializeField] private float outlineWidth = 0.2f; // Outline thickness (0-1)
    [SerializeField] private Color safeColor = new Color(0.2f, 1f, 0.2f); // Green > 5 min
    [SerializeField] private Color cautionColor = new Color(1f, 1f, 0.2f); // Yellow 2-5 min
    [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0f); // Orange 1-2 min
    [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.2f); // Red < 1 min

    [Header("Time Thresholds (seconds)")]
    [SerializeField] private float cautionThreshold = 300f; // 5 minutes
    [SerializeField] private float warningThreshold = 120f; // 2 minutes
    [SerializeField] private float criticalThreshold = 60f; // 1 minute

    [Header("Warning Effects")]
    [SerializeField] private bool enableCriticalPulse = true;
    [SerializeField] private float pulseDuration = 0.5f;

    [Header("Display Options")]
    [SerializeField] private bool showLabel = true;
    [SerializeField] private bool showCircularProgress = true;
    [SerializeField] private bool hideWhenDisabled = true; // Hide UI when timer disabled

    [Header("Position Settings")]
    [SerializeField] private bool followPlayer = false;
    [Tooltip("X=Left/Right, Y=Up/Down, Z=Distance. Position relative to camera")]
    [SerializeField] private Vector3 hudOffset = new Vector3(-0.3f, 0.2f, 0.5f); // Left side, opposite of health/energy

    private bool isCriticalWarning = false;
    private Coroutine pulseCoroutine;

    private void Start()
    {
        // Subscribe to GameTimer if it exists
        if (GameTimer.Instance != null)
        {
            // Check if timer is enabled/active
            UpdateVisibility();

            GameLog.Log("GameTimerUI initialized");
        }
        else
        {
            GameLog.LogWarning("GameTimer not found! UI will not function until gameplay scene loads.");
        }

        // Position HUD
        if (!followPlayer)
        {
            PositionHUD();
        }

        // Show/hide elements based on settings
        UpdateElementVisibility();

        // Initialize text outline settings
        InitializeTextOutline();
    }

    private void OnValidate()
    {
        // Update element visibility when inspector values change
        if (Application.isPlaying)
        {
            UpdateElementVisibility();
        }
    }

    private void UpdateElementVisibility()
    {
        if (timerLabel != null)
        {
            timerLabel.gameObject.SetActive(showLabel);
        }

        if (timerCircle != null)
        {
            timerCircle.gameObject.SetActive(showCircularProgress);
        }
    }

    private void InitializeTextOutline()
    {
        if (timerText != null)
        {
            // Set text color to black
            timerText.color = textColor;

            // Enable outline
            timerText.outlineWidth = outlineWidth;
            timerText.outlineColor = safeColor; // Start with safe color

            GameLog.Log($"Timer text outline initialized - Width: {outlineWidth}, Color: {safeColor}");
        }
    }

    private void OnDestroy()
    {
        // Stop any running coroutines
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
    }

    private void Update()
    {
        // Update timer display every frame
        if (GameTimer.Instance != null)
        {
            UpdateTimerDisplay();
        }
    }

    private void LateUpdate()
    {
        // If follow player enabled, update position each frame
        if (followPlayer && PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            UpdateHUDPosition();
        }
    }

    #region Display Updates

    private void UpdateTimerDisplay()
    {
        if (GameTimer.Instance == null) return;

        // Check visibility based on timer mode
        UpdateVisibility();

        // Don't update display if timer is disabled
        if (GameTimer.Instance.Mode == GameTimer.TimerMode.Disabled)
            return;

        // Get formatted time
        string timeString = GameTimer.Instance.GetFormattedTime();

        // Update text
        if (timerText != null)
        {
            timerText.text = timeString;
        }

        // Update color and circular progress based on mode
        if (GameTimer.Instance.Mode == GameTimer.TimerMode.CountDown)
        {
            UpdateCountdownDisplay();
        }
        else // CountUp mode
        {
            UpdateCountUpDisplay();
        }
    }

    private void UpdateCountdownDisplay()
    {
        float remainingTime = GameTimer.Instance.RemainingTime;
        Color urgencyColor = GetColorForRemainingTime(remainingTime);

        // Keep text black, update outline color to show urgency
        if (timerText != null && !isCriticalWarning)
        {
            timerText.color = textColor; // Black text
            timerText.outlineColor = urgencyColor; // Colored outline shows urgency
        }

        // Update circle progress and color
        if (timerCircle != null && showCircularProgress)
        {
            float progress = GameTimer.Instance.GetTimePercentage();
            timerCircle.fillAmount = progress;
            timerCircle.color = urgencyColor; // Circle shows urgency color
        }

        // Handle critical warning pulse
        if (remainingTime <= criticalThreshold && remainingTime > 0f)
        {
            if (!isCriticalWarning && enableCriticalPulse)
            {
                isCriticalWarning = true;
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                }
                pulseCoroutine = StartCoroutine(CriticalPulse());
            }
        }
        else
        {
            isCriticalWarning = false;
        }
    }

    private void UpdateCountUpDisplay()
    {
        // CountUp mode - black text with green outline
        if (timerText != null)
        {
            timerText.color = textColor; // Black text
            timerText.outlineColor = safeColor; // Green outline
        }

        if (timerCircle != null && showCircularProgress)
        {
            // For count up, show as full circle with safe color
            timerCircle.fillAmount = 1f;
            timerCircle.color = safeColor;
        }
    }

    private Color GetColorForRemainingTime(float remainingTime)
    {
        if (remainingTime > cautionThreshold)
        {
            return safeColor; // Green
        }
        else if (remainingTime > warningThreshold)
        {
            return cautionColor; // Yellow
        }
        else if (remainingTime > criticalThreshold)
        {
            return warningColor; // Orange
        }
        else
        {
            return criticalColor; // Red
        }
    }

    private void UpdateVisibility()
    {
        if (!hideWhenDisabled)
            return;

        if (GameTimer.Instance == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Show UI only when timer is enabled
        bool shouldShow = GameTimer.Instance.Mode != GameTimer.TimerMode.Disabled;
        gameObject.SetActive(shouldShow);
    }

    #endregion

    #region Warning Effects

    private IEnumerator CriticalPulse()
    {
        if (timerText == null) yield break;

        while (isCriticalWarning && GameTimer.Instance != null && GameTimer.Instance.RemainingTime > 0f)
        {
            // Pulse outline color from critical red to white
            float elapsed = 0f;
            while (elapsed < pulseDuration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration / 2);

                // Keep text black, pulse outline color
                timerText.color = textColor; // Stay black
                timerText.outlineColor = Color.Lerp(criticalColor, Color.white, t);

                yield return null;
            }

            // Pulse back from white to critical red
            elapsed = 0f;
            while (elapsed < pulseDuration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration / 2);

                // Keep text black, pulse outline color
                timerText.color = textColor; // Stay black
                timerText.outlineColor = Color.Lerp(Color.white, criticalColor, t);

                yield return null;
            }

            yield return new WaitForSeconds(0.3f); // Pause between pulses
        }

        // Reset to appropriate outline color
        if (GameTimer.Instance != null && timerText != null)
        {
            float remainingTime = GameTimer.Instance.RemainingTime;
            Color urgencyColor = GetColorForRemainingTime(remainingTime);

            timerText.color = textColor; // Black text
            timerText.outlineColor = urgencyColor; // Restore urgency outline color
        }
    }

    #endregion

    #region Positioning

    private void PositionHUD()
    {
        if (PlayerController.Instance == null || PlayerController.Instance.XROrigin == null)
        {
            GameLog.LogWarning("Cannot position Timer HUD - PlayerController or XROrigin not found");
            return;
        }

        Camera mainCamera = PlayerController.Instance.XROrigin.GetComponentInChildren<Camera>();
        if (mainCamera == null)
        {
            GameLog.LogWarning("Cannot position Timer HUD - Main Camera not found in XR Origin");
            return;
        }

        // Position HUD relative to camera
        transform.position = mainCamera.transform.position +
                           mainCamera.transform.right * hudOffset.x +
                           mainCamera.transform.up * hudOffset.y +
                           mainCamera.transform.forward * hudOffset.z;

        // Face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    }

    private void UpdateHUDPosition()
    {
        Camera mainCamera = PlayerController.Instance.XROrigin.GetComponentInChildren<Camera>();
        if (mainCamera == null) return;

        // Instantly follow player head position (no smoothing)
        transform.position = mainCamera.transform.position +
                            mainCamera.transform.right * hudOffset.x +
                            mainCamera.transform.up * hudOffset.y +
                            mainCamera.transform.forward * hudOffset.z;

        // Always face camera
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Show or hide the timer HUD
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// Set custom HUD offset position
    /// </summary>
    public void SetHUDOffset(Vector3 offset)
    {
        hudOffset = offset;
        if (!followPlayer)
        {
            PositionHUD();
        }
    }

    /// <summary>
    /// Enable or disable follow player mode
    /// </summary>
    public void SetFollowPlayer(bool follow)
    {
        followPlayer = follow;
        if (!follow)
        {
            PositionHUD();
        }
    }

    #endregion
}