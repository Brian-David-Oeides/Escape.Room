using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Consumable object that can be grabbed and consumed to restore health/energy
/// Attach to food items like candy
/// Implements ISaveable to track consumption across save/load
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]

public class ConsumableObject : MonoBehaviour, ISaveable
{
    [Header("Save System")]
    [Tooltip("Unique ID for this consumable - MUST be unique in scene")]
    [SerializeField] private string consumableID = "candy_001";

    [Tooltip("If true, this consumable is always present regardless of difficulty. If false, it may be hidden on harder difficulties.")]
    [SerializeField] private bool isCore = true;

    [Header("Consumable Properties")]
    [SerializeField] private float energyRestoreAmount = 30f;
    [SerializeField] private float healthRestoreAmount = 15f;

    [Header("Consumption Method")]
    [SerializeField] private ConsumptionMode consumptionMode = ConsumptionMode.BringToMouth;
    [SerializeField] private float mouthProximityDistance = 0.2f; // Distance from head to consume
    [SerializeField] private KeyCode consumeButton = KeyCode.E; // For testing without VR

    [Header("VR Button Consumption")]
    [Tooltip("If enabled, player can press a button while holding to consume")]
    [SerializeField] private bool allowButtonConsumption = true;
    [SerializeField] private UnityEngine.InputSystem.InputActionProperty consumeAction;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject consumeEffect; // Particle effect (optional)
    [SerializeField] private float shrinkDuration = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip consumeSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isConsumed = false;
    private bool isBeingHeld = false;
    private Transform headTransform;

    public enum ConsumptionMode
    {
        BringToMouth,      // Bring object close to head to consume
        ButtonPress,       // Press button while holding to consume
        Automatic          // Consume immediately when grabbed
    }

    public string SaveID => consumableID;
    public bool IsCore => isCore;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // Get or create AudioSource
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.playOnAwake = false;
        }

        // Subscribe to grab events
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        // Enable button action if using button consumption
        if (allowButtonConsumption && consumeAction.action != null)
        {
            consumeAction.action.Enable();
        }
    }

    private void Start()
    {
        // If already consumed (loaded from save), exit early
        if (isConsumed)
        {
            DebugLog($"{gameObject.name} already consumed - staying deactivated");
            return;
        }

        // Find head transform (camera)
        if (PlayerController.Instance != null && PlayerController.Instance.XROrigin != null)
        {
            Camera mainCamera = PlayerController.Instance.XROrigin.GetComponentInChildren<Camera>();
            if (mainCamera != null)
            {
                headTransform = mainCamera.transform;
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        // Symmetric disable for consumeAction
        if (allowButtonConsumption && consumeAction.action != null)
        {
            consumeAction.action.Disable();
        }
    }

    private void Update()
    {
        if (isConsumed || !isBeingHeld) return;

        // Check consumption based on mode
        switch (consumptionMode)
        {
            case ConsumptionMode.BringToMouth:
                CheckMouthProximity();
                break;

            case ConsumptionMode.ButtonPress:
                CheckButtonPress();
                break;
        }

        // Also check for VR button consumption if enabled
        if (allowButtonConsumption && consumeAction.action != null)
        {
            if (consumeAction.action.WasPressedThisFrame())
            {
                Consume();
            }
        }

        // Keyboard test (non-VR testing)
        if (Input.GetKeyDown(consumeButton))
        {
            Consume();
        }
    }

    #region Interaction Events

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (isConsumed) return;

        isBeingHeld = true;
        DebugLog($"Grabbed by {args.interactorObject.transform.name}");

        // Automatic consumption mode
        if (consumptionMode == ConsumptionMode.Automatic)
        {
            Consume();
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isBeingHeld = false;
        DebugLog("Released");
    }

    #endregion

    #region Consumption Logic

    private void CheckMouthProximity()
    {
        if (headTransform == null) return;

        float distance = Vector3.Distance(transform.position, headTransform.position);

        if (distance <= mouthProximityDistance)
        {
            Consume();
        }
    }

    private void CheckButtonPress()
    {
        // For non-VR testing, check keyboard
        if (Input.GetKeyDown(consumeButton))
        {
            Consume();
        }
    }

    public void Consume()
    {
        if (isConsumed) return;

        isConsumed = true;

        DebugLog($"Consumed! Restoring {healthRestoreAmount} health and {energyRestoreAmount} energy");

        // Restore health and energy
        if (HealthEnergyManager.Instance != null)
        {
            HealthEnergyManager.Instance.RestoreHealth(healthRestoreAmount);
            HealthEnergyManager.Instance.RestoreEnergy(energyRestoreAmount);
        }
        else
        {
            GameLog.LogError("HealthEnergyManager not found! Cannot restore health/energy.");
        }

        // Visual feedback
        if (consumeEffect != null)
        {
            Instantiate(consumeEffect, transform.position, Quaternion.identity);
        }

        // Audio feedback
        if (consumeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(consumeSound);
        }

        // Animate consumption (shrink and destroy)
        StartCoroutine(ConsumeAnimationAndHide());
    }

    /// <summary>
    /// Animate consumption and hide object (keep it in scene for save system)
    /// </summary>
    private IEnumerator ConsumeAnimationAndHide()
    {
        // STEP 1: Immediately disable interaction (but keep visible for animation)
        var xrGrab = GetComponent<XRGrabInteractable>();
        if (xrGrab != null)
        {
            xrGrab.enabled = false;
        }

        var rigidBody = GetComponent<Rigidbody>();
        if (rigidBody != null)
        {
            rigidBody.isKinematic = true;
        }

        // STEP 2: Play shrink animation (object still visible)
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        // STEP 3: After animation completes, spawn particle effect
        if (consumeEffect != null)
        {
            Instantiate(consumeEffect, transform.position, Quaternion.identity);
        }

        // STEP 4: NOW disable visuals (after animation finished)
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        var rend = GetComponent<MeshRenderer>();
        if (rend != null)
        {
            rend.enabled = false;
        }

        // Keep transform at scale zero (invisible)
        transform.localScale = Vector3.zero;

        DebugLog($"{gameObject.name} ({consumableID}) hidden but kept in scene for save system");

        // DO NOT use Destroy(gameObject) - we need it for save system!

        // COMMENT OUT DESTROY METHOD // Destroy the object
        //DebugLog("Destroying consumed object");
        //gameObject.SetActive(false); // Deactivate instead of destroy (for save system)
    }

    #endregion

    #region ISaveable Implementation

    public void SaveState(SaveData saveData)
    {
        // If consumed, add to consumed list
        if (isConsumed && !saveData.consumedObjectIDs.Contains(consumableID))
        {
            saveData.consumedObjectIDs.Add(consumableID);
            DebugLog($"Saved consumed state for {consumableID}");
        }
    }

    public void LoadState(SaveData saveData)
    {
        // Debug: Show what's in the consumed list
        string consumedList = string.Join(", ", saveData.consumedObjectIDs);
        DebugLog($"LoadState called for {consumableID}. Consumed list: [{consumedList}]");


        // Check if this object was consumed in the save
        isConsumed = saveData.consumedObjectIDs.Contains(consumableID);

        if (isConsumed)
        {
            // Object was already consumed - deactivate it
            DebugLog($"Loading consumed state for {consumableID} - deactivating");
            gameObject.SetActive(false);
        }
        else
        {
            DebugLog($"{consumableID} was NOT consumed - staying active");
        }
    }

    #endregion

    #region Debug

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            GameLog.Log($"[ConsumableObject:{consumableID}] {message}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize mouth proximity distance when BringToMouth mode
        if (consumptionMode == ConsumptionMode.BringToMouth && headTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(headTransform.position, mouthProximityDistance);
        }
    }

    #endregion
}