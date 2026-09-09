using UnityEngine;

// TEMPORARY diagnostic script - not part of the game. Attached to hammer
// head alongside HammerImpactAudio to isolate whether collision events are
// reaching this GameObject at all, independent of HammerImpactAudio itself.
public class TempCollisionTest : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"TEST HIT: {gameObject.name} collided with {collision.collider.name}");
    }
}
