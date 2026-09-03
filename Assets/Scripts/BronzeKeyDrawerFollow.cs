using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public class BronzeKeyDrawerFollow : MonoBehaviour
{
    [Tooltip("The drawer's Transform this key rests on top of - followed via script, never Transform-parented (nested Rigidbodies break the drawer's ConfigurableJoint/gravity behavior)")]
    [SerializeField] private Transform drawerTransform;

    [Tooltip("Local position relative to drawerTransform the key rests at while sitting in the drawer")]
    [SerializeField] private Vector3 localRestOffset = new Vector3(-0.0004091f, -0.0822623f, -0.2105946f);

    [Tooltip("While true, the key is moved every FixedUpdate to follow the drawer instead of behaving as a free Rigidbody")]
    [SerializeField] private bool resting = true;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

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
