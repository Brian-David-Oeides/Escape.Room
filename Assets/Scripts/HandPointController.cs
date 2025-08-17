using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class HandPointController : MonoBehaviour
{
    [Header("XR Controller")]
    public ActionBasedController xRController;

    [Header("Animation")]
    public string pointParameterName = "Point";
    public string gripParameterName = "Grip"; // NEW: For grip state

    private Animator handAnimator;

    private void Start()
    {
        StartCoroutine(FindHandAnimatorAfterSpawn());
    }

    private IEnumerator FindHandAnimatorAfterSpawn()
    {
        yield return new WaitForSeconds(0.5f);

        bool isLeftController = gameObject.name.ToLower().Contains("left");

        Animator[] animators = FindObjectsOfType<Animator>();

        foreach (Animator anim in animators)
        {
            if (anim.runtimeAnimatorController != null)
            {
                bool isLeftHand = anim.runtimeAnimatorController.name.Contains("LeftHand");
                bool isRightHand = anim.runtimeAnimatorController.name.Contains("RightHand");

                if ((isLeftController && isLeftHand) || (!isLeftController && isRightHand))
                {
                    handAnimator = anim;
                    Debug.Log($"Found runtime hand: {anim.gameObject.name} for {gameObject.name}");
                    break;
                }
            }
        }

        if (handAnimator == null)
        {
            Debug.LogError($"Could not find runtime hand animator for {gameObject.name}");
        }
    }

    private void Update()
    {
        if (xRController != null && handAnimator != null)
        {
            // Handle trigger for pointing
            bool triggerPressed = xRController.activateInteractionState.active;
            handAnimator.SetBool(pointParameterName, triggerPressed);

            // Handle grip for fist (NEW)
            bool gripPressed = xRController.selectInteractionState.active;
            handAnimator.SetBool(gripParameterName, gripPressed);

            // Debug
            if (triggerPressed || gripPressed)
            {
                Debug.Log($"{gameObject.name} - Point: {triggerPressed}, Grip: {gripPressed}");
            }
        }
    }
}