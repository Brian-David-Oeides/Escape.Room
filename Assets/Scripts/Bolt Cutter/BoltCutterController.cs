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

    // Determine which hand's trigger action to use, based on the grabbing interactor's transform name
    // (matches the existing name-based hand-detection pattern used in DynamicAttachPoint.cs)
    private InputActionReference GetTriggerActionForInteractor(Transform interactorTransform)
    {
        bool isLeftHand = interactorTransform != null && interactorTransform.name.ToLower().Contains("left");
        return isLeftHand ? leftTriggerAction : rightTriggerAction;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Enable and subscribe only the trigger action for the hand that grabbed this
        InputActionReference triggerAction = GetTriggerActionForInteractor(args.interactorObject.transform);
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPressed;
        }

        GameLog.Log($"Bolt cutters grabbed by {args.interactorObject.transform.name} - trigger input enabled");
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Disable and unsubscribe only the trigger action for the hand that released this
        InputActionReference triggerAction = GetTriggerActionForInteractor(args.interactorObject.transform);
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.performed -= OnTriggerPressed;
            triggerAction.action.Disable();
        }

        GameLog.Log($"Bolt cutters released by {args.interactorObject.transform.name} - trigger input disabled");
    }

    void OnTriggerPressed(InputAction.CallbackContext context)
    {
        GameLog.Log("Trigger pressed while holding bolt cutters!");
        TriggerCut();
    }
}




