using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDoorController : MonoBehaviour
{
    [SerializeField] private Animator animator; // Animator on Door_Hinge
    [SerializeField] private XRGrabInteractable grabInteractable;

    [SerializeField] private string turnHandleParam = "TurnHandle";
    [SerializeField] private string isOpenParam = "IsOpen";
    [SerializeField] private float handleAnimDuration = 1.0f;

    private bool isOpen = false;
    private bool isAnimating = false;

    void Start()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnHandleGrabbed);
        }

        // Ensure states start correctly
        animator.SetBool(turnHandleParam, false);
        animator.SetBool(isOpenParam, false);
    }

    private void OnHandleGrabbed(SelectEnterEventArgs args)
    {
        if (isAnimating) return;

        StartCoroutine(AnimateDoorToggle());
    }

    private IEnumerator AnimateDoorToggle()
    {
        isAnimating = true;

        // Trigger handle animation
        animator.SetBool(turnHandleParam, true);

        yield return new WaitForSeconds(handleAnimDuration);

        // Toggle door state
        isOpen = !isOpen;
        animator.SetBool(isOpenParam, isOpen);

        // Reset handle param (if used in transition)
        animator.SetBool(turnHandleParam, false);

        isAnimating = false;
    }
}

