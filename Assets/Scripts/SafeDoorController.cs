using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDoorController : MonoBehaviour
{
    [Header("Animator References")]
    [SerializeField] private Animator doorAnimator;     // Animator on the parent
    [SerializeField] private Animator handleAnimator;   // Animator on the child handle

    [Header("Animation Parameters")]
    [SerializeField] private string handleBool = "TurnHandle";
    [SerializeField] private string doorBool = "IsOpen";

    [Header("Timing")]
    [SerializeField] private float handleAnimationDuration = 1.0f;

    private bool isOpen = false;

    void Start()
    {
        // Auto-assign door animator if not set
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();

        // Auto-assign handle animator if not set
        if (handleAnimator == null)
            handleAnimator = GetComponentInChildren<Animator>();

        // Register grab event from the XRGrabInteractable on the handle
        XRGrabInteractable grab = handleAnimator.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.selectEntered.AddListener(OnHandleGrabbed);
            Debug.Log("SafeDoorController: Grab listener registered");
        }
        else
        {
            Debug.LogError("SafeDoorController: XRGrabInteractable not found on handle object");
        }

        // Init door state
        if (doorAnimator != null)
            doorAnimator.SetBool(doorBool, false);
    }

    private void OnHandleGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("SafeDoorController: Handle grabbed");
        StartCoroutine(HandleThenToggleDoor());
    }

    private IEnumerator HandleThenToggleDoor()
    {
        // 1. Play handle animation
        handleAnimator.SetBool(handleBool, true);

        // 2. Wait for handle animation to finish
        yield return new WaitForSeconds(handleAnimationDuration);

        // 3. Toggle door open/close
        isOpen = !isOpen;
        doorAnimator.SetBool(doorBool, isOpen);
        Debug.Log("SafeDoorController: Door toggled to " + (isOpen ? "Open" : "Closed"));
    }
}
