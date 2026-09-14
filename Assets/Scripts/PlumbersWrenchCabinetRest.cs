using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public class PlumbersWrenchCabinetRest : MonoBehaviour
{
    [Tooltip("The Cabinet's Base Transform (parent of 'Cabinet Base floor'/'Cabinet Base side') the wrench rests relative to - followed via script, never Transform-parented (nested Rigidbodies break physics, same reasoning as BronzeKeyDrawerFollow).")]
    [SerializeField] private Transform cabinetBaseTransform;

    [Tooltip("The Cabinet's floor BoxCollider the wrench stands on - collision response against it is disabled so resting here can never push/disturb the Cabinet.")]
    [SerializeField] private Collider cabinetFloorCollider;

    [Tooltip("The Cabinet's side-wall BoxCollider the wrench leans against - collision response against it is disabled for the same reason.")]
    [SerializeField] private Collider cabinetSideCollider;

    [Tooltip("Local position relative to cabinetBaseTransform, standing on the floor and leaning against the side wall. Derived from the floor/side collider bounds as a best-effort starting placement - verify/nudge visually in the Editor.")]
    [SerializeField] private Vector3 localRestPosition = new Vector3(0.2286f, 0.065f, 0f);

    [Tooltip("Local rotation (Euler, relative to cabinetBaseTransform) standing the wrench upright and leaning it against the side wall. Best-effort starting value - verify/nudge visually in the Editor.")]
    [SerializeField] private Vector3 localRestEulerAngles = new Vector3(0f, -90f, 15f);

    [Tooltip("While true, the wrench is moved every FixedUpdate to hold this rest pose instead of behaving as a free Rigidbody. The Cabinet's floor/side panels are static (unlike the Bronze Key's drawer), so this only ever holds one fixed pose rather than tracking a moving target.")]
    [SerializeField] private bool resting = true;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // The wrench has a single BoxCollider on its root (its two attach-point children -
        // AttachPoint_Wrench, SocketAttachPoint - carry no colliders of their own). Disable
        // collision response against both Cabinet panels so resting here can never push or
        // torque the Cabinet, regardless of how closely MovePosition tracks the rest pose -
        // learning directly from the Bronze Key/Drawer fix.
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (cabinetFloorCollider != null)
                Physics.IgnoreCollision(col, cabinetFloorCollider, true);

            if (cabinetSideCollider != null)
                Physics.IgnoreCollision(col, cabinetSideCollider, true);
        }

        if (resting)
        {
            rb.isKinematic = true;

            // Snap into the rest pose immediately, rather than waiting for the first
            // FixedUpdate tick, so it's correct from the very first rendered frame.
            ApplyRestPose();
        }
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void FixedUpdate()
    {
        if (!resting || cabinetBaseTransform == null) return;

        rb.MovePosition(cabinetBaseTransform.TransformPoint(localRestPosition));
        rb.MoveRotation(cabinetBaseTransform.rotation * Quaternion.Euler(localRestEulerAngles));
    }

    /// <summary>
    /// Computes and applies the rest pose directly via Transform, not Rigidbody.MovePosition -
    /// this is what lets it work both at Awake (before physics has stepped) and from the Editor
    /// context menu below (no physics simulation running at all outside Play mode).
    /// </summary>
    private void ApplyRestPose()
    {
        if (cabinetBaseTransform == null)
        {
            GameLog.LogWarning("[PlumbersWrenchCabinetRest] cabinetBaseTransform not assigned - cannot apply rest pose.");
            return;
        }

        transform.position = cabinetBaseTransform.TransformPoint(localRestPosition);
        transform.rotation = cabinetBaseTransform.rotation * Quaternion.Euler(localRestEulerAngles);
    }

    /// <summary>
    /// Right-click this component in the Inspector to preview/adjust localRestPosition and
    /// localRestEulerAngles live in the Scene view without entering Play mode.
    /// </summary>
    [ContextMenu("Snap to Rest Pose")]
    private void SnapToRestPose()
    {
        ApplyRestPose();
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Stop holding the rest pose immediately so this doesn't fight
        // XRGrabInteractable's own MovePosition/MoveRotation calls while held.
        resting = false;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // XRGrabInteractable's Kinematic movement type restores isKinematic to whatever
        // it was at grab-time (true, since the wrench starts kinematic while resting) -
        // undo that here so a dropped/thrown wrench behaves as a normal physics object
        // instead of freezing in place (same fix as Bronze Key).
        rb.isKinematic = false;
    }
}
