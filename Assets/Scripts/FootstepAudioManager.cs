using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[System.Serializable]
public class FootstepSoundSet
{
    public string surfaceName;
    public AudioClip[] walkClips;
    public AudioClip[] runClips;
    [Range(0f, 1f)] public float volume = 0.5f;
}

public enum SurfaceType
{
    Floor,
    Water
}

public class FootstepAudioManager : MonoSingleton<FootstepAudioManager>
{
    [Header("Sound Sets")]
    [SerializeField] private FootstepSoundSet floorSounds;
    [SerializeField] private FootstepSoundSet waterSounds;

    [Header("Timing Settings")]
    [Tooltip("Distance in meters between walk footsteps")]
    [SerializeField] private float walkStepDistance = 0.6f;

    [Tooltip("Distance in meters between run footsteps")]
    [SerializeField] private float runStepDistance = 0.4f;

    [Tooltip("Thumbstick input threshold to switch from walk to run sounds (0-1 range)")]
    [Range(0.5f, 0.95f)]
    [SerializeField] private float runInputThreshold = 0.8f;

    [Header("Surface Detection")]
    [Tooltip("How far down to raycast for surface detection")]
    [SerializeField] private float raycastDistance = 0.5f;

    [Tooltip("Layer mask for ground detection")]
    [SerializeField] private LayerMask groundLayerMask = -1; // Everything by default

    [Header("Audio Settings")]
    [Tooltip("Random pitch variation range")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    [Header("Debug")]
    [Tooltip("Enable detailed debug logging")]
    [SerializeField] private bool enableDebugLogs = true;

    private AudioSource audioSource;
    private Transform xrOrigin;
    private ActionBasedContinuousMoveProvider moveProvider;

    private Vector3 lastFootstepPosition;
    private float distanceTraveled = 0f;
    private SurfaceType currentSurface = SurfaceType.Floor;

    // Cached layer indices (more reliable than layer masks)
    private int waterLayerIndex;
    private int defaultLayerIndex;

    protected override void Awake()
    {
        base.Awake(); // THIS IS CRITICAL - calls MonoSingleton's Awake first
        Debug.Log("[FootstepAudioManager] Awake called");
    }

    public override void Init()
    {
        Debug.Log("[FootstepAudioManager] Init called");

        DontDestroyOnLoad(gameObject);

        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound

        // Cache layer indices for surface detection
        waterLayerIndex = LayerMask.NameToLayer("Water");
        defaultLayerIndex = LayerMask.NameToLayer("Default");

        Debug.Log($"[FootstepAudioManager] Layer indices - Water: {waterLayerIndex}, Default: {defaultLayerIndex}");

        // Find XR Origin reference
        UpdateXROriginReference();

        Debug.Log("[FootstepAudioManager] Initialized successfully");
    }

    /// <summary>
    /// Find and cache XR Origin reference (called on scene load)
    /// </summary>
    public void UpdateXROriginReference()
    {
        GameObject xrOriginGO = GameObject.Find("XR Origin (XR Rig)");

        if (xrOriginGO != null)
        {
            xrOrigin = xrOriginGO.transform;
            moveProvider = xrOriginGO.GetComponent<ActionBasedContinuousMoveProvider>();

            if (moveProvider != null)
            {
                Debug.Log("[FootstepAudioManager] ✓ XR Origin reference updated successfully");
                lastFootstepPosition = xrOrigin.position;
            }
            else
            {
                Debug.LogError("[FootstepAudioManager] ✗ ActionBasedContinuousMoveProvider not found on XR Origin!");
            }
        }
        else
        {
            Debug.LogWarning("[FootstepAudioManager] ⚠ XR Origin not found in scene - may be in Main Menu");
        }
    }

    private void Update()
    {
        // Only play footsteps during gameplay
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
        {
            return;
        }

        // Can't play footsteps without XR Origin
        if (xrOrigin == null || moveProvider == null)
        {
            return;
        }

        // Check if player is moving
        if (!IsPlayerMoving())
        {
            // Reset distance when not moving
            distanceTraveled = 0f;
            lastFootstepPosition = xrOrigin.position;
            return;
        }

        // Track distance traveled
        float distanceThisFrame = Vector3.Distance(xrOrigin.position, lastFootstepPosition);
        distanceTraveled += distanceThisFrame;
        lastFootstepPosition = xrOrigin.position;

        // Determine if running based on thumbstick input magnitude
        float inputMagnitude = GetInputMagnitude();
        bool isRunning = inputMagnitude > runInputThreshold;
        float requiredDistance = isRunning ? runStepDistance : walkStepDistance;

        // Play footstep when threshold reached
        if (distanceTraveled >= requiredDistance)
        {
            DetectSurface();
            PlayFootstep(isRunning);
            distanceTraveled = 0f;
        }
    }

    private bool IsPlayerMoving()
    {
        if (moveProvider == null) return false;

        // Check if thumbstick has input
        Vector2 moveInput = moveProvider.leftHandMoveAction.action.ReadValue<Vector2>();
        return moveInput.magnitude > 0.1f;
    }

    private float GetInputMagnitude()
    {
        if (moveProvider == null) return 0f;

        // Get thumbstick input magnitude (0-1 range)
        Vector2 moveInput = moveProvider.leftHandMoveAction.action.ReadValue<Vector2>();
        return moveInput.magnitude;
    }

    private void DetectSurface()
    {
        // Raycast downward from slightly above XR Origin
        Vector3 rayStart = xrOrigin.position + Vector3.up * 0.1f;
        RaycastHit hit;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, groundLayerMask))
        {
            int hitLayer = hit.collider.gameObject.layer;
            string hitTag = hit.collider.tag;

            if (enableDebugLogs)
            {
                Debug.Log($"[FootstepAudioManager] Raycast hit: '{hit.collider.name}' | Layer: {hitLayer} ({LayerMask.LayerToName(hitLayer)}) | Tag: '{hitTag}'");
            }

            // Check Water layer FIRST (Layer 4)
            if (hitLayer == waterLayerIndex)
            {
                currentSurface = SurfaceType.Water;
                if (enableDebugLogs)
                {
                    Debug.Log($"[FootstepAudioManager] Surface detected: WATER (layer match: {hitLayer} == {waterLayerIndex})");
                }
                return;
            }

            // Check Floor tag for Default layer objects
            if (hitTag == "Floor")
            {
                currentSurface = SurfaceType.Floor;
                if (enableDebugLogs)
                {
                    Debug.Log($"[FootstepAudioManager] Surface detected: FLOOR (tag match: '{hitTag}')");
                }
                return;
            }

            // Default to Floor for any other surface
            currentSurface = SurfaceType.Floor;
            if (enableDebugLogs)
            {
                Debug.Log($"[FootstepAudioManager] Surface detected: FLOOR (default - untagged surface)");
            }
        }
        else
        {
            // No ground detected, default to floor
            currentSurface = SurfaceType.Floor;
            if (enableDebugLogs)
            {
                Debug.Log($"[FootstepAudioManager] No ground detected in raycast - defaulting to FLOOR");
            }
        }
    }

    private void PlayFootstep(bool isRunning)
    {
        FootstepSoundSet soundSet = GetSoundSetForSurface(currentSurface);
        if (soundSet == null) return;

        // Select appropriate clip array
        AudioClip[] clips = isRunning ? soundSet.runClips : soundSet.walkClips;

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[FootstepAudioManager] No clips for {currentSurface} - {(isRunning ? "Run" : "Walk")}");
            return;
        }

        // Randomly select a clip
        AudioClip clip = clips[Random.Range(0, clips.Length)];

        // Apply volume from AudioManager and sound set
        float masterVolume = AudioManager.Instance != null ? AudioManager.Instance.GetSFXVolume() : 1f;
        audioSource.volume = soundSet.volume * masterVolume;

        // Random pitch variation
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);

        // Play the clip
        audioSource.PlayOneShot(clip);

        if (enableDebugLogs)
        {
            Debug.Log($"[FootstepAudioManager] Playing {currentSurface} footstep - {(isRunning ? "Running" : "Walking")} (clip: {clip.name})");
        }
    }

    private FootstepSoundSet GetSoundSetForSurface(SurfaceType surface)
    {
        switch (surface)
        {
            case SurfaceType.Water:
                return waterSounds;
            case SurfaceType.Floor:
            default:
                return floorSounds;
        }
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (xrOrigin == null) return;

        // Draw raycast line
        Vector3 rayStart = xrOrigin.position + Vector3.up * 0.1f;
        Gizmos.color = currentSurface == SurfaceType.Water ? Color.cyan : Color.green;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * raycastDistance);
    }
}