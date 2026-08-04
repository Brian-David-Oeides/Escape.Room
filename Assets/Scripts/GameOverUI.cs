using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays Game Over screen in VR
/// Shows fail condition, stats, and navigation buttons
/// </summary>
/// 

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private CanvasGroup canvasGroup; // For fade effects

    [Header("Display Messages")]
    [SerializeField] private string timeUpTitle = "TIME'S UP!";
    [SerializeField] private string timeUpMessage = "You ran out of time";
    [SerializeField] private string healthDepletedTitle = "GAME OVER";
    [SerializeField] private string healthDepletedMessage = "Your health was depleted";
    [SerializeField] private string defaultTitle = "GAME OVER";
    [SerializeField] private string defaultMessage = "Try again?";

    [Header("Colors")]
    [SerializeField] private Color timeUpColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color healthDepletedColor = new Color(1f, 0.2f, 0.2f); // Red
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Animation")]
    [SerializeField] private bool useFadeIn = true;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float delayBeforeShow = 0.5f;

    [Header("Position Settings")]
    [SerializeField] private bool autoPosition = false; // When false, uses Inspector Rect Transform
    [SerializeField] private Vector3 screenOffset = new Vector3(0f, 0.5f, 2f); // Only used if autoPosition is true

    [Header("Audio")]
    [SerializeField] private bool playGameOverSound = true;

    private bool isShowing = false;

    public enum GameOverReason
    {
        TimerExpired,
        HealthDepleted,
        Other
    }

    private void Start()
    {
        // Subscribe to GameManager state changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        // Setup button listeners
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        // Start hidden
        Hide();

        GameLog.Log("GameOverUI initialized");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        // Remove button listeners
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver)
        {
            // Determine the reason for game over
            GameOverReason reason = DetermineGameOverReason();
            Show(reason);
        }
        else
        {
            // Hide game over screen when not in game over state
            if (isShowing)
            {
                Hide();
            }
        }
    }

    private GameOverReason DetermineGameOverReason()
    {
        // Check if timer expired
        if (GameTimer.Instance != null && GameTimer.Instance.TimerExpired)
        {
            return GameOverReason.TimerExpired;
        }

        // Check if health depleted
        if (HealthEnergyManager.Instance != null && HealthEnergyManager.Instance.GetCurrentHealth() <= 0f)
        {
            return GameOverReason.HealthDepleted;
        }

        // Default
        return GameOverReason.Other;
    }

    #region Show/Hide

    public void Show(GameOverReason reason)
    {
        if (isShowing) return;

        StartCoroutine(ShowGameOverScreen(reason));
    }

    private IEnumerator ShowGameOverScreen(GameOverReason reason)
    {
        isShowing = true;

        // Wait before showing
        yield return new WaitForSeconds(delayBeforeShow);

        // Position screen in front of player
        PositionScreen();

        // Update content based on reason
        UpdateContent(reason);

        // Update stats
        UpdateStats();

        // Show panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Play audio
        if (playGameOverSound && AudioManager.Instance != null)
        {
            // TODO: Add game over sound to AudioManager
            // AudioManager.Instance.PlayGameOverSound();
        }

        // Fade in if enabled
        if (useFadeIn && canvasGroup != null)
        {
            yield return StartCoroutine(FadeIn());
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        GameLog.Log($"Game Over screen shown - Reason: {reason}");
    }

    public void Hide()
    {
        isShowing = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    #endregion

    #region Content Updates

    private void UpdateContent(GameOverReason reason)
    {
        string title = defaultTitle;
        string message = defaultMessage;
        Color titleColor = defaultColor;

        switch (reason)
        {
            case GameOverReason.TimerExpired:
                title = timeUpTitle;
                message = timeUpMessage;
                titleColor = timeUpColor;
                break;

            case GameOverReason.HealthDepleted:
                title = healthDepletedTitle;
                message = healthDepletedMessage;
                titleColor = healthDepletedColor;
                break;

            case GameOverReason.Other:
                title = defaultTitle;
                message = defaultMessage;
                titleColor = defaultColor;
                break;
        }

        // Update UI elements
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = titleColor;
        }

        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    private void UpdateStats()
    {
        if (statsText == null) return;

        string stats = "";

        // Time survived/elapsed
        if (GameTimer.Instance != null)
        {
            string timeLabel = GameTimer.Instance.Mode == GameTimer.TimerMode.CountDown ? "Time Survived" : "Time Elapsed";
            string timeValue = GameTimer.Instance.GetFormattedElapsedTime();
            stats += $"{timeLabel}: {timeValue}\n";
        }

        // Health remaining (if relevant)
        if (HealthEnergyManager.Instance != null)
        {
            float health = HealthEnergyManager.Instance.GetCurrentHealth();
            stats += $"Health Remaining: {health:F0}\n";
        }

        // TODO: Add more stats when available
        // - Items collected
        // - Puzzles solved
        // - Distance traveled
        // - etc.

        statsText.text = stats.TrimEnd('\n');
    }

    #endregion

    #region Button Handlers

    private void OnRetryClicked()
    {
        GameLog.Log("Retry button clicked");

        // TODO: Add button click sound when AudioManager has PlayButtonClickSFX method
        // if (AudioManager.Instance != null)
        // {
        //     AudioManager.Instance.PlayButtonClickSFX();
        // }

        // Hide game over screen
        Hide();

        // Restart game through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    private void OnMainMenuClicked()
    {
        GameLog.Log("Main Menu button clicked");

        // TODO: Add button click sound when AudioManager has PlayButtonClickSFX method
        // if (AudioManager.Instance != null)
        // {
        //     AudioManager.Instance.PlayButtonClickSFX();
        // }

        // Hide game over screen
        Hide();

        // Return to main menu through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }

    #endregion

    #region Positioning & Animation

    private void PositionScreen()
    {
        // If auto position is disabled, respect Inspector Rect Transform settings
        if (!autoPosition)
        {
            GameLog.Log("GameOverUI using manual positioning from Inspector Rect Transform");
            return;
        }

        // Auto position mode - calculate position based on camera
        if (PlayerController.Instance == null || PlayerController.Instance.XROrigin == null)
        {
            GameLog.LogWarning("Cannot position Game Over screen - PlayerController or XROrigin not found");
            return;
        }

        Camera mainCamera = PlayerController.Instance.XROrigin.GetComponentInChildren<Camera>();
        if (mainCamera == null)
        {
            GameLog.LogWarning("Cannot position Game Over screen - Main Camera not found in XR Origin");
            return;
        }

        // Position screen in front of camera
        transform.position = mainCamera.transform.position +
                           mainCamera.transform.forward * screenOffset.z +
                           mainCamera.transform.up * screenOffset.y +
                           mainCamera.transform.right * screenOffset.x;

        // Face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);

        GameLog.Log("GameOverUI auto-positioned in front of camera");
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually show game over screen with specific reason
    /// </summary>
    public void ShowGameOver(GameOverReason reason)
    {
        Show(reason);
    }

    /// <summary>
    /// Check if game over screen is currently showing
    /// </summary>
    public bool IsShowing()
    {
        return isShowing;
    }

    #endregion
}