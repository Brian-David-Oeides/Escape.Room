using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Slider progressBar; 
    [SerializeField] private RectTransform spinningLoader; 

    [Header("Spinner Settings")]
    [SerializeField] private float spinSpeed = 200f; // Degrees per second

    [Header("World Space Settings")]
    [SerializeField] private float distanceFromCamera = 2f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, 0);

    private Transform playerCamera;

    private void Awake()
    {
        // Make this canvas persist across scenes
        DontDestroyOnLoad(gameObject);

        // Hide loading screen by default
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Rotate the spinner continuously
        if (spinningLoader != null && loadingPanel != null && loadingPanel.activeSelf)
        {
            spinningLoader.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        }
    }

    public void Show()
    {
        if (loadingPanel != null)
        {
            // Get the VR camera reference
            if (playerCamera == null)
            {
                if (PlayerController.Instance?.XROrigin != null)
                {
                    // Find the Camera component within the XR Origin hierarchy
                    Camera cam = PlayerController.Instance.XROrigin.GetComponentInChildren<Camera>();
                    if (cam != null)
                    {
                        playerCamera = cam.transform;
                    }
                }
            }

            // Position the loading screen in front of the camera
            if (playerCamera != null)
            {
                PositionInFrontOfCamera();
            }

            loadingPanel.SetActive(true);
            GameLog.Log("Loading screen shown");
        }
    }

    public void Hide()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            GameLog.Log("Loading screen hidden");
        }
    }

    public void UpdateLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }

    public void UpdateProgress(float progress)
    {
        // Update progress bar
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
        // Update loading text with percentage
        if (loadingText != null)
        {
            loadingText.text = $"LOADING... {Mathf.RoundToInt(progress * 100)}%";
        }
    }

    private void PositionInFrontOfCamera()
    {
        if (playerCamera == null) return;

        // Position the canvas in front of the camera
        Vector3 targetPosition = playerCamera.position + playerCamera.forward * distanceFromCamera + offset;
        transform.position = targetPosition;

        // Make the canvas face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - playerCamera.position);
    }
}
