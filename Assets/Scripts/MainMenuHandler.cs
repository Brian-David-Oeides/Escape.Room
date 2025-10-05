using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class MainMenuHandler : MonoBehaviour
{
    [Header("Menu UI References")]
    public GameObject mainMenuUI;
    public Transform mainMenuSpawnPoint; // reference for the MainMenuPosition

    [Header("Exit Confirmation")]
    public GameObject exitConfirmationPanel; // Reference to the ExitConfirmationPanel

    [Header("Position Lock Settings")]
    [SerializeField] private bool lockPositionInMenu = true;

    private Vector3 lockedPosition;
    private bool isInMainMenu = false;

    private void Start()
    {
        // Subscribe to GameManager state changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        // Initialize menu if we're in main menu scene
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            StartCoroutine(InitializeMainMenuDelayed());
        }
        else
        {
            // In gameplay scene - hide menu UI and disable this script
            if (mainMenuUI != null)
            {
                mainMenuUI.SetActive(false);
            }
            this.enabled = false;
        }
    }

    private IEnumerator InitializeMainMenuDelayed()
    {
        // Wait for PlayerController to be initialized
        while (PlayerController.Instance == null || PlayerController.Instance.XROrigin == null)
        {
            yield return null;
        }

        // Wait one more frame to ensure everything is set up
        yield return null;

        InitializeMainMenu();

        // Force disable movement again to ensure it's locked
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableMovement();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }

    private void InitializeMainMenu()
    {
        // Position and orient the XR Origin
        if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            GameObject xrOrigin = PlayerController.Instance.XROrigin;

            if (mainMenuSpawnPoint != null)
            {
                xrOrigin.transform.position = mainMenuSpawnPoint.position;
                xrOrigin.transform.rotation = mainMenuSpawnPoint.rotation;

                // Store the locked position
                lockedPosition = mainMenuSpawnPoint.position;

                if (mainMenuUI != null)
                {
                    RotateOriginToFace(mainMenuUI.transform.position);
                }

                Debug.Log("Player positioned at main menu spawn point");
            }
        }

        // Show main menu UI
        if (mainMenuUI != null)
        {
            mainMenuUI.SetActive(true);
        }

        // Hide exit confirmation
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }

        isInMainMenu = true;
    }

    private void LateUpdate()
    {
        // Lock player position in main menu
        if (lockPositionInMenu && isInMainMenu && PlayerController.Instance?.XROrigin != null)
        {
            PlayerController.Instance.XROrigin.transform.position = lockedPosition;
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
                if (mainMenuUI != null)
                {
                    mainMenuUI.SetActive(true);
                }
                isInMainMenu = true;
                break;

            case GameState.Loading:
                if (mainMenuUI != null)
                {
                    mainMenuUI.SetActive(false);
                }
                isInMainMenu = false;
                break;

            default:
                isInMainMenu = false;
                break;
        }
    }

    // Public methods called by UI buttons
    public void StartGame()
    {
        Debug.Log("StartGame() called from UI button");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }

    public void ShowExitConfirmation()
    {
        Debug.Log("ShowExitConfirmation() called");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ExitConfirmationPanel is not assigned!");
        }
    }

    public void ConfirmExit()
    {
        Debug.Log("Exit confirmed by user");
        ExitGame();
    }

    public void CancelExit()
    {
        Debug.Log("Exit cancelled by user");

        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
        }
    }

    public void ExitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void RotateOriginToFace(Vector3 targetPosition)
    {
        if (PlayerController.Instance?.XROrigin == null) return;

        GameObject xrOrigin = PlayerController.Instance.XROrigin;
        Camera camera = xrOrigin.GetComponentInChildren<Camera>();
        if (camera == null) return;

        Vector3 headsetForward = camera.transform.forward;
        headsetForward.y = 0;

        Vector3 directionToUI = targetPosition - camera.transform.position;
        directionToUI.y = 0;

        float angle = Vector3.SignedAngle(headsetForward, directionToUI, Vector3.up);
        xrOrigin.transform.Rotate(0, angle, 0);
    }
}