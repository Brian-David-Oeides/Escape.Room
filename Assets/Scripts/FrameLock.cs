using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;

public class FrameLock : PuzzleBase
{
    [Header("Frame Lock Settings")]
    [SerializeField] int[] _lockCode = new int[3] { 1, 2, 3 };
    int[] _enteredCode = { 0, 0, 0 };
    private XRSocketInteractor[] _interactors;

    protected override void Start()
    {
        base.Start(); // CRITICAL: Loads saved completion state!

        _interactors = GetComponentsInChildren<XRSocketInteractor>();
    }

    public void EnterSocket0()
    {
        // Don't allow interaction if puzzle already completed
        if (isCompleted)
        {
            DebugLog("Puzzle already completed - ignoring frame placement");
            return;
        }

        var interactables = _interactors[0].interactablesSelected;
        FrameID id = interactables[0]?.transform.GetComponent<FrameID>();
        if (id != null)
        {
            _enteredCode[0] = id.GetID();
            if (CheckCode())
                CompletePuzzle();
        }
    }

    public void EnterSocket1()
    {
        // Don't allow interaction if puzzle already completed
        if (isCompleted)
        {
            DebugLog("Puzzle already completed - ignoring frame placement");
            return;
        }

        var interactables = _interactors[1].interactablesSelected;
        FrameID id = interactables[0]?.transform.GetComponent<FrameID>();
        if (id != null)
        {
            _enteredCode[1] = id.GetID();
            if (CheckCode())
                CompletePuzzle();
        }
    }

    public void EnterSocket2()
    {
        // Don't allow interaction if puzzle already completed
        if (isCompleted)
        {
            DebugLog("Puzzle already completed - ignoring frame placement");
            return;
        }

        var interactables = _interactors[2].interactablesSelected;
        FrameID id = interactables[0]?.transform.GetComponent<FrameID>();
        if (id != null)
        {
            _enteredCode[2] = id.GetID();
            if (CheckCode())
                CompletePuzzle();
        }
    }

    private bool CheckCode()
    {
        if (_enteredCode.Length == _lockCode.Length)
        {
            // Check if all slots are filled (no zeros = default value)
            bool allSlotsFilled = true;
            foreach (int code in _enteredCode)
            {
                if (code == 0)
                {
                    allSlotsFilled = false;
                    break;
                }
            }

            // Only check correctness if all slots filled
            if (allSlotsFilled)
            {
                // Check if code matches
                for (int i = 0; i < _enteredCode.Length; i++)
                {
                    if (_enteredCode[i] != _lockCode[i])
                    {
                        // Wrong code - register failed attempt
                        RegisterFailedAttempt();
                        return false;
                    }
                }

                // Code is correct!
                return true;
            }

            return false;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Override CompletePuzzle to lock frames in place when solved
    /// </summary>
    public override void CompletePuzzle()
    {
        DebugLog($"Frame lock solved! Code: [{string.Join(", ", _enteredCode)}]");

        // Call base to handle save system and fire OnPuzzleCompleted event
        base.CompletePuzzle();

        // OPTION A FIX: Lock frames in their solved positions
        LockFramesInPlace();
    }

    /// <summary>
    /// OPTION A FIX: Disable grab functionality on all frames in sockets
    /// This prevents players from pulling frames out after puzzle is solved
    /// </summary>
    private void LockFramesInPlace()
    {
        int framesLocked = 0;

        // Loop through all 3 socket interactors
        foreach (var interactor in _interactors)
        {
            if (interactor != null)
            {
                // Get the frame object currently in this socket
                var interactables = interactor.interactablesSelected;
                if (interactables.Count > 0)
                {
                    var frameObject = interactables[0].transform;

                    // Disable the XRGrabInteractable component so it can't be grabbed
                    var grabInteractable = frameObject.GetComponent<XRGrabInteractable>();
                    if (grabInteractable != null)
                    {
                        grabInteractable.enabled = false;
                        framesLocked++;
                        DebugLog($"Locked frame in socket: {frameObject.name}");
                    }
                }
            }
        }

        DebugLog($"Locked {framesLocked} frames in their solved positions");
    }

    /// <summary>
    /// Override ApplyCompletedStateVisuals to restore and lock frames when loading completed state
    /// </summary>
    protected override void ApplyCompletedStateVisuals()
    {
        // Call base to handle colliders/renderers
        base.ApplyCompletedStateVisuals();

        // OPTION A FIX: When loading a completed puzzle, we need to:
        // 1. Find the frame objects in the scene (they spawn at original positions)
        // 2. Move them into the correct sockets
        // 3. Lock them in place
        StartCoroutine(RestoreAndLockFrames());
    }

    /// <summary>
    /// Restore frames to their solved positions and lock them
    /// </summary>
    private IEnumerator RestoreAndLockFrames()
    {
        // Wait for scene to fully load
        yield return new WaitForEndOfFrame();

        DebugLog("Restoring frames to solved positions...");

        // Find all frame objects in the scene (INCLUDING DISABLED ONES)
        FrameID[] allFrames = Resources.FindObjectsOfTypeAll<FrameID>();

        // Filter to only scene objects (not prefabs)
        List<FrameID> sceneFrames = new List<FrameID>();
        foreach (var frame in allFrames)
        {
            // Only include frames that are in the scene (not in prefabs or assets)
            if (frame.gameObject.scene.isLoaded)
            {
                sceneFrames.Add(frame);
            }
        }

        DebugLog($"Found {sceneFrames.Count} frames in scene (including disabled)");

        // Place each frame in its correct socket based on the lock code
        for (int i = 0; i < _lockCode.Length && i < _interactors.Length; i++)
        {
            int requiredFrameID = _lockCode[i];

            // Find the frame with this ID
            FrameID correctFrame = sceneFrames.Find(frame => frame.GetID() == requiredFrameID);

            if (correctFrame != null && _interactors[i] != null)
            {
                // CRITICAL FIX: Enable the frame if it's disabled
                if (!correctFrame.gameObject.activeSelf)
                {
                    correctFrame.gameObject.SetActive(true);
                    DebugLog($"Enabled frame {requiredFrameID} (was disabled by dependency)");
                }

                // Get the attach transform of the socket (where objects snap to)
                Transform attachTransform = _interactors[i].attachTransform;

                if (attachTransform != null)
                {
                    // Disable physics on the frame
                    Rigidbody frameRb = correctFrame.GetComponent<Rigidbody>();
                    if (frameRb != null)
                    {
                        frameRb.isKinematic = true;
                    }

                    // Disable the grab interactable BEFORE positioning
                    var grabInteractable = correctFrame.GetComponent<XRGrabInteractable>();
                    if (grabInteractable != null)
                    {
                        grabInteractable.enabled = false;
                    }

                    // SCALE FIX: Store original world scale before parenting
                    Vector3 originalScale = correctFrame.transform.lossyScale;

                    // Parent frame to socket attach transform (locks it in place)
                    correctFrame.transform.SetParent(attachTransform);

                    // Reset local transform to snap perfectly to socket
                    correctFrame.transform.localPosition = Vector3.zero;
                    correctFrame.transform.localRotation = Quaternion.identity;

                    // SCALE FIX: Restore original world scale by adjusting local scale
                    // We need to compensate for the parent's scale
                    Vector3 parentScale = attachTransform.lossyScale;
                    correctFrame.transform.localScale = new Vector3(
                        originalScale.x / parentScale.x,
                        originalScale.y / parentScale.y,
                        originalScale.z / parentScale.z
                    );

                    DebugLog($"Restored and locked frame {requiredFrameID} to socket {i} (scale preserved)");
                }
            }
            else
            {
                if (correctFrame == null)
                {
                    GameLog.LogWarning($"[FrameLock] Could not find frame with ID {requiredFrameID} in scene!");
                }
            }
        }

        DebugLog("All frames restored and locked in sockets");
    }

    private void RegisterFailedAttempt()
    {
        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.RegisterFailedAttempt(puzzleID);
            DebugLog($"Wrong frame combination: [{string.Join(", ", _enteredCode)}] (expected: [{string.Join(", ", _lockCode)}])");
        }
    }

}