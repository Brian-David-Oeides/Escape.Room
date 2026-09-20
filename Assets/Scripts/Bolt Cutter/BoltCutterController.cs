using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; 
using UnityEngine.InputSystem;

public class BoltCutterController : MonoBehaviour
{
    [Header("Blade Transforms")]
    public Transform bladeLeftPivot;
    public Transform bladeRightPivot;

    [Header("Cutting Rotation")]
    public float maxRotation = 30f;
    public float speed = 90f; // degrees per second
    public float holdClosedDuration = 1f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftTriggerAction;
    [SerializeField] private InputActionReference rightTriggerAction;

    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;

    // The trigger action belonging to the hand currently holding this tool. Both hands'
    // callbacks stay subscribed while enabled, so the handler filters on this instead.
    private InputActionReference heldTriggerAction;

    private float currentRotation = 0f;
    private bool isCutting = false;
    private bool isResetting = false;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Subscribe to grab/release events
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void OnEnable()
    {
        // Subscribe to both hands' trigger callbacks; OnTriggerPressed filters on the holding hand.
        // These are the rig-owned Activate actions - never Enable()/Disable() them from here.
        if (leftTriggerAction != null && leftTriggerAction.action != null)
        {
            leftTriggerAction.action.performed += OnTriggerPressed;
        }

        if (rightTriggerAction != null && rightTriggerAction.action != null)
        {
            rightTriggerAction.action.performed += OnTriggerPressed;
        }
    }

    void OnDisable()
    {
        if (leftTriggerAction != null && leftTriggerAction.action != null)
        {
            leftTriggerAction.action.performed -= OnTriggerPressed;
        }

        if (rightTriggerAction != null && rightTriggerAction.action != null)
        {
            rightTriggerAction.action.performed -= OnTriggerPressed;
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    void Update()
    {
        if (isCutting && !isResetting)
        {
            if (currentRotation < maxRotation)
            {
                float rotationStep = speed * Time.deltaTime;
                rotationStep = Mathf.Min(rotationStep, maxRotation - currentRotation);
                bladeLeftPivot.localRotation *= Quaternion.Euler(0, 0, -rotationStep);
                bladeRightPivot.localRotation *= Quaternion.Euler(0, 0, rotationStep);
                currentRotation += rotationStep;

                if (currentRotation >= maxRotation)
                {
                    StartCoroutine(ResetBlades());
                }
            }
        }
    }

    public void TriggerCut()
    {
        if (!isCutting)
        {
            isCutting = true;
            currentRotation = 0f;
            BoltCutterCutState.IsCutting = true;
        }
    }

    private IEnumerator ResetBlades()
    {
        isResetting = true;
        yield return new WaitForSeconds(holdClosedDuration);

        float resetRotation = 0f;
        while (resetRotation < maxRotation)
        {
            float rotationStep = speed * Time.deltaTime;
            rotationStep = Mathf.Min(rotationStep, maxRotation - resetRotation);
            bladeLeftPivot.localRotation *= Quaternion.Euler(0, 0, rotationStep);
            bladeRightPivot.localRotation *= Quaternion.Euler(0, 0, -rotationStep);
            resetRotation += rotationStep;
            yield return null;
        }

        isCutting = false;
        isResetting = false;
        BoltCutterCutState.IsCutting = false; // reset the flag
    }

    // Determine which hand's trigger action to use. The interactor GameObjects themselves are
    // named "Direct Interactor"/"Ray Interactor" for both hands, so walk up to the hand's
    // controller object ("Left Controller"/"Right Controller") and check that name instead -
    // the same object DynamicAttachPoint.cs does its name-based hand detection on.
    private InputActionReference GetTriggerActionForInteractor(Transform interactorTransform)
    {
        ActionBasedController controller = interactorTransform != null
            ? interactorTransform.GetComponentInParent<ActionBasedController>()
            : null;

        for (Transform t = controller != null ? controller.transform : interactorTransform; t != null; t = t.parent)
        {
            string name = t.name.ToLower();
            if (name.Contains("left")) return leftTriggerAction;
            if (name.Contains("right")) return rightTriggerAction;
        }

        GameLog.LogWarning($"[BoltCutterController] Could not determine hand for interactor '{interactorTransform?.name}' - defaulting to right");
        return rightTriggerAction;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        // Remember which hand's trigger should drive the cut. The rig-owned Activate action
        // must never be enabled/disabled from here - the XR controllers depend on it.
        heldTriggerAction = GetTriggerActionForInteractor(args.interactorObject.transform);

        GameLog.Log($"Bolt cutters grabbed by {args.interactorObject.transform.name} - listening for {heldTriggerAction?.name}");
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        heldTriggerAction = null;

        GameLog.Log($"Bolt cutters released by {args.interactorObject.transform.name}");
    }

    void OnTriggerPressed(InputAction.CallbackContext context)
    {
        // Only cut if grabbed, and only for the trigger of the hand holding this tool
        if (!isGrabbed || heldTriggerAction == null || context.action != heldTriggerAction.action)
        {
            return;
        }

        GameLog.Log("Trigger pressed while holding bolt cutters!");
        TriggerCut();
    }
}




