using UnityEngine;

// TEMPORARY diagnostic script - not part of the game. Attached to a
// brand-new, minimal test object sharing no lineage with the hammer,
// to isolate whether OnCollisionEnter fires at all for a maximally
// simple non-kinematic Rigidbody + BoxCollider setup.
public class MinimalCollisionTest : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"MINIMAL TEST HIT: {gameObject.name} collided with {collision.collider.name}");
    }
}
