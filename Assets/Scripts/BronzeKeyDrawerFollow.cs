using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public class BronzeKeyDrawerFollow : MonoBehaviour
{
    [Tooltip("The drawer's Transform this key rests on top of - followed via script, never Transform-parented (nested Rigidbodies break the drawer's ConfigurableJoint/gravity behavior)")]
    [SerializeField] private Transform drawerTransform;

    [Tooltip("The drawer's floor BoxCollider - collision response against it is disabled so resting here can never push/torque the drawer, without needing the key's own colliders to be triggers")]
    [SerializeField] private Collider drawerFloorCollider;

    [Tooltip("Local position relative to drawerTransform the key rests at while sitting in the drawer - includes a small clearance gap above the floor collider")]
    [SerializeField] private Vector3 localRestOffset = new Vector3(-0.0004091f, -0.0797623f, -0.2105946f);

    [Tooltip("While true, the key is moved every FixedUpdate to follow the drawer instead of behaving as a free Rigidbody")]
    [SerializeField] private bool resting = true;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // The key's compound collider shape spans two BoxColliders (root + AttachPoint_Key1
        // child). Disable collision response against the drawer's floor collider specifically -
        // both stay solid (non-trigger) so XR Interaction Toolkit's grab/ray detection keeps
        // working (it ignores trigger colliders by default), but this pair can never push
        // or torque the drawer regardless of how closely MovePosition tracks the floor.
        if (drawerFloorCollider != null)
        {
            foreach (Collider col in GetComponentsInChildren<Collider>(true))
            {
                Physics.IgnoreCollision(col, drawerFloorCollider, true);
            }
        }

        if (resting)
            rb.isKinematic = true;
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
        if (!resting || drawerTransform == null) return;

        rb.MovePosition(drawerTransform.TransformPoint(localRestOffset));
        rb.MoveRotation(drawerTransform.rotation);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Stop following the drawer immediately so this doesn't fight
        // XRGrabInteractable's own MovePosition/MoveRotation calls while held.
        resting = false;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // XRGrabInteractable restores isKinematic to whatever it was at
        // grab-time (true, since the key starts kinematic while resting) -
        // undo that here so a dropped/thrown key behaves as a normal
        // physics object instead of freezing in place.
        rb.isKinematic = false;
    }
}
