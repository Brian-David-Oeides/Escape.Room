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

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Enable trigger input when grabbed
        if (leftTriggerAction != null && leftTriggerAction.action != null)
        {
            leftTriggerAction.action.performed += OnTriggerPressed;
        }

        if (rightTriggerAction != null && rightTriggerAction.action != null)
        {
            rightTriggerAction.action.performed += OnTriggerPressed;
        }

        Debug.Log("Bolt cutters grabbed - trigger input enabled");
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Disable trigger input when released
        if (leftTriggerAction != null && leftTriggerAction.action != null)
        {
            leftTriggerAction.action.performed -= OnTriggerPressed;
        }

        if (rightTriggerAction != null && rightTriggerAction.action != null)
        {
            rightTriggerAction.action.performed -= OnTriggerPressed;
        }

        Debug.Log("Bolt cutters released - trigger input disabled");
    }

    void OnTriggerPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Trigger pressed while holding bolt cutters!");
        TriggerCut();
    }
}




