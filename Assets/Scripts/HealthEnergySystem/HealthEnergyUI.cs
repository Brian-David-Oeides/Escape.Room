using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays Health and Energy as circular progress bars in VR HUD
/// Inner circle = Health (Red)
/// Outer circle = Energy (Yellow/Green)
/// </summary>

public class HealthEnergyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthCircle;
    [SerializeField] private Image energyCircle;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI healthLabel;  
    [SerializeField] private TextMeshProUGUI energyLabel;

    [Header("Colors")]
    [SerializeField] private Color healthNormalColor = new Color(1f, 0.2f, 0.2f); // Red
    [SerializeField] private Color healthLowColor = new Color(0.8f, 0f, 0f); // Dark red
    [SerializeField] private Color energyHighColor = new Color(0.2f, 1f, 0.2f); // Green
    [SerializeField] private Color energyMediumColor = new Color(1f, 1f, 0.2f); // Yellow
    [SerializeField] private Color energyLowColor = new Color(1f, 0.5f, 0f); // Orange

    [Header("Warning Effects")]
    [SerializeField] private bool enableWarningPulse = true;
    [SerializeField] private float pulseDuration = 0.5f;

    [Header("Text Display")]
    [SerializeField] private bool showPercentageText = true;
    [SerializeField] private bool showNumericText = false;
    [SerializeField] private bool showLabels = true;

    [Header("Position Settings")]
    [SerializeField] private bool followPlayer = false;
    [Tooltip("X=Left/Right, Y=Up/Down, Z=Distance. Adjust in Play mode to reposition HUD")]
    [SerializeField] private Vector3 hudOffset = new Vector3(0.3f, 0.2f, 0.5f);

    private bool isWarningHealth = false;
    private bool isWarningEnergy = false;

    private void Start()
    {
        // Subscribe to HealthEnergyManager events
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.OnHealthChanged += UpdateHealthDisplay;
            HealthEnergyManager.Instance.OnEnergyChanged += UpdateEnergyDisplay;
            HealthEnergyManager.Instance.OnLowHealth += OnLowHealthWarning;
            HealthEnergyManager.Instance.OnLowEnergy += OnLowEnergyWarning;

            // Initial display
            UpdateHealthDisplay(HealthEnergyManager.Instance.GetCurrentHealth());
            UpdateEnergyDisplay(HealthEnergyManager.Instance.GetCurrentEnergy());

            Debug.Log("HealthEnergyUI initialized and subscribed to events");
        }
        else
        {
            Debug.LogError("HealthEnergyManager not found! UI will not function.");
        }

        // Position HUD
        if (!followPlayer)
        {
            PositionHUD();
        }

        // Show/hide labels based on setting
        UpdateLabelVisibility();
    }

    private void OnValidate()
    {
        // Update label visibility when inspector values change
        if (Application.isPlaying)
        {
            UpdateLabelVisibility();
        }
    }

    private void UpdateLabelVisibility()
    {
        if (healthLabel != null)
        {
            healthLabel.gameObject.SetActive(showLabels);
        }
        if (energyLabel != null)
        {
            energyLabel.gameObject.SetActive(showLabels);
        }

        // Handle percentage/numeric text
        bool showText = showPercentageText || showNumericText;

        if (healthText != null)
        {
            healthText.gameObject.SetActive(showText);
        }
        if (energyText != null)
        {
            energyText.gameObject.SetActive(showText);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.OnHealthChanged -= UpdateHealthDisplay;
            HealthEnergyManager.Instance.OnEnergyChanged -= UpdateEnergyDisplay;
            HealthEnergyManager.Instance.OnLowHealth -= OnLowHealthWarning;
            HealthEnergyManager.Instance.OnLowEnergy -= OnLowEnergyWarning;
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

    private void UpdateHealthDisplay(float currentHealth)
    {
        if (healthCircle == null) return;

        float maxHealth = HealthEnergyManager.Instance.GetMaxHealth();
        float fillAmount = currentHealth / maxHealth;

        // Update fill amount (1 = full circle, 0 = empty)
        healthCircle.fillAmount = fillAmount;

        // Update color based on health level
        if (fillAmount <= 0.25f)
        {
            healthCircle.color = healthLowColor;
        }
        else
        {
            healthCircle.color = healthNormalColor;
        }

        // Update text
        if (healthText != null && healthText.gameObject.activeSelf)
        {
            if (showPercentageText)
            {
                healthText.text = $"{fillAmount * 100:F0}%";
            }
            else if (showNumericText)
            {
                healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
            }
        }
    }

    private void UpdateEnergyDisplay(float currentEnergy)
    {
        if (energyCircle == null) return;

        float maxEnergy = HealthEnergyManager.Instance.GetMaxEnergy();
        float fillAmount = currentEnergy / maxEnergy;

        // Update fill amount
        energyCircle.fillAmount = fillAmount;

        // Update color based on energy level
        if (fillAmount > 0.5f)
        {
            energyCircle.color = energyHighColor; // Green
        }
        else if (fillAmount > 0.25f)
        {
            energyCircle.color = energyMediumColor; // Yellow
        }
        else
        {
            energyCircle.color = energyLowColor; // Orange
        }

        // Update text
        if (energyText != null && energyText.gameObject.activeSelf)
        {
            if (showPercentageText)
            {
                energyText.text = $"{fillAmount * 100:F0}%";
            }
            else if (showNumericText)
            {
                energyText.text = $"{currentEnergy:F0}/{maxEnergy:F0}";
            }
        }
    }

    #endregion

    #region Warning Effects

    private void OnLowHealthWarning()
    {
        if (!isWarningHealth && enableWarningPulse)
        {
            isWarningHealth = true;
            StartCoroutine(PulseWarning(healthCircle));
        }
    }

    private void OnLowEnergyWarning()
    {
        if (!isWarningEnergy && enableWarningPulse)
        {
            isWarningEnergy = true;
            StartCoroutine(PulseWarning(energyCircle));
        }
    }

    private IEnumerator PulseWarning(Image targetImage)
    {
        if (targetImage == null) yield break;

        Color originalColor = targetImage.color;

        // Pulse 3 times
        for (int i = 0; i < 3; i++)
        {
            // Fade to white/bright
            float elapsed = 0f;
            while (elapsed < pulseDuration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration / 2);
                targetImage.color = Color.Lerp(originalColor, Color.white, t);
                yield return null;
            }

            // Fade back to original
            elapsed = 0f;
            while (elapsed < pulseDuration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration / 2);
                targetImage.color = Color.Lerp(Color.white, originalColor, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }

        // Reset
        targetImage.color = originalColor;
    }

    #endregion

    #region Positioning

    private void PositionHUD()
    {
        if (PlayerController.Instance == null || PlayerController.Instance.XROrigin == null)
        {
            Debug.LogWarning("Cannot position HUD - PlayerController or XROrigin not found");
            return;
        }

        Camera mainCamera = PlayerController.Instance.XROrigin.GetComponentInChildren<Camera>();
        if (mainCamera == null)
        {
            Debug.LogWarning("Cannot position HUD - Main Camera not found in XR Origin");
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

    //private void UpdateHUDPosition()
    //{
    //    Camera mainCamera = PlayerController.Instance.XROrigin.GetComponentInChildren<Camera>();
    //    if (mainCamera == null) return;

    //    // Smoothly follow player head position
    //    Vector3 targetPosition = mainCamera.transform.position +
    //                            mainCamera.transform.right * hudOffset.x +
    //                            mainCamera.transform.up * hudOffset.y +
    //                            mainCamera.transform.forward * hudOffset.z;

    //    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);

    //    // Always face camera
    //    transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    //}

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
    /// Show or hide the HUD
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

    #endregion
}
