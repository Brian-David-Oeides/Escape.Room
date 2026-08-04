using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SetPokeInteractorPosition : MonoBehaviour
{
    #region Variables and Flags

    [Header("Finger Configuration")]
    [SerializeField] private Transform indexFingerTip;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private bool useCustomOffset = false;

    private XRPokeInteractor _pokeInteractor;
    private Transform _pokeTransform;

    #endregion

    void Start()
    {
        StartCoroutine(InitializePokePositioning());
    }

    IEnumerator InitializePokePositioning()
    {
        // Wait for XR system initialization
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        FindPokeComponents();
        SetupFingerTipPositioning();
    }

    void FindPokeComponents()
    {
        // Find the poke interactor in the controller hierarchy
        _pokeInteractor = GetComponentInParent<XRPokeInteractor>();

        if (_pokeInteractor == null)
        {
            // Search in parent controllers if not found directly
            Transform controller = transform;
            while (controller != null && _pokeInteractor == null)
            {
                _pokeInteractor = controller.GetComponentInChildren<XRPokeInteractor>();
                controller = controller.parent;
            }
        }

        if (_pokeInteractor != null)
        {
            _pokeTransform = _pokeInteractor.transform;
            GameLog.Log($"Found Poke Interactor: {_pokeInteractor.name}");
        }
        else
        {
            //GameLog.LogError("XR Poke Interactor not found!");
        }
    }

    void SetupFingerTipPositioning()
    {
        if (indexFingerTip == null)
        {
            // Auto-find fingertip if not assigned
            indexFingerTip = FindFingerTip();
        }

        if (indexFingerTip == null)
        {
            GameLog.LogError("Index finger tip transform not found!");
            return;
        }

        //GameLog.Log($"Using finger tip: {indexFingerTip.name}");

        // Position the poke interactor at fingertip
        PositionPokeAtFingertip();
    }

    void PositionPokeAtFingertip()
    {
        if (_pokeTransform == null || indexFingerTip == null) return;

        Vector3 targetPosition = indexFingerTip.position;

        if (useCustomOffset)
        {
            // Apply custom offset in fingertip's local space
            targetPosition += indexFingerTip.TransformDirection(positionOffset);
        }

        _pokeTransform.position = targetPosition;
        _pokeTransform.rotation = indexFingerTip.rotation;

        // Clear attach transform since we're positioning directly
        _pokeInteractor.attachTransform = null;

        //GameLog.Log($"Positioned poke interactor at: {targetPosition}");
    }

    Transform FindFingerTip()
    {
        // Determine if this is left or right controller
        bool isLeftController = gameObject.name.ToLower().Contains("left");
        bool isRightController = gameObject.name.ToLower().Contains("right");

        //GameLog.Log($"Controller detection - Left: {isLeftController}, Right: {isRightController} for: {gameObject.name}");

        // Build fingertip names based on controller type
        string[] fingerTipNames;

        if (isLeftController)
        {
            fingerTipNames = new string[] {
                "hands:b_l_index_ignore",
                "hands:b_l_index3",
                "LeftHandIndexTip",
                "index_03_l"
            };
        }
        else if (isRightController)
        {
            fingerTipNames = new string[] {
                "hands:b_r_index_ignore",
                "hands:b_r_index3",
                "RightHandIndexTip",
                "index_03_r"
            };
        }
        else
        {
            // Fallback - search all if controller type unclear
            fingerTipNames = new string[] {
                "hands:b_l_index_ignore",
                "hands:b_r_index_ignore",
                "hands:b_l_index3",
                "hands:b_r_index3",
                "LeftHandIndexTip",
                "RightHandIndexTip",
                "IndexTip",
                "index_03_l",
                "index_03_r"
            };
        }

        //GameLog.Log($"Searching for fingertip from controller: {gameObject.name}");

        foreach (string name in fingerTipNames)
        {
            Transform found = FindChildRecursive(transform.root, name);
            if (found != null)
            {
                GameLog.Log($"Auto-found fingertip: {found.name} for controller: {gameObject.name}");
                return found;
            }
            else
            {
                GameLog.Log($"Did not find: {name}");
            }
        }

        GameLog.LogWarning($"Could not auto-find fingertip transform for controller: {gameObject.name}");

        // List available hand-related transforms for debugging
        //GameLog.Log("Available hand-related transforms:");
        LogHandTransforms(transform.root, 0, isLeftController, isRightController);

        return null;
    }

    void LogHandTransforms(Transform parent, int depth, bool searchLeft, bool searchRight)
    {
        string indent = new string(' ', depth * 2);
        string name = parent.name.ToLower();

        // Log transforms that match the hand we're looking for
        if ((searchLeft && (name.Contains("_l_") || name.Contains("left"))) ||
            (searchRight && (name.Contains("_r_") || name.Contains("right"))) ||
            (!searchLeft && !searchRight && (name.Contains("index") || name.Contains("finger"))))
        {
            //GameLog.Log($"{indent}- {parent.name}");
        }

        foreach (Transform child in parent)
        {
            LogHandTransforms(child, depth + 1, searchLeft, searchRight);
        }
    }

    Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }

    void Update()
    {
        // Continuously update position to follow finger movement
        if (_pokeTransform != null && indexFingerTip != null)
        {
            PositionPokeAtFingertip();
        }
    }

    void LateUpdate()
    {
        // Additional update in LateUpdate for more responsive tracking
        if (_pokeTransform != null && indexFingerTip != null)
        {
            PositionPokeAtFingertip();
        }
    }

    void OnDrawGizmos()
    {
        // Visualize the fingertip position in Scene view
        if (indexFingerTip != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(indexFingerTip.position, 0.01f);

            if (useCustomOffset)
            {
                Vector3 offsetPos = indexFingerTip.position + indexFingerTip.TransformDirection(positionOffset);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(offsetPos, 0.008f);
                Gizmos.DrawLine(indexFingerTip.position, offsetPos);
            }
        }
    }
}