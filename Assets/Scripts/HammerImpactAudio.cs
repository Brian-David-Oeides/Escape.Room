using UnityEngine;

public class HammerImpactAudio : MonoBehaviour
{
    [SerializeField] private AudioClip thudClip;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        GameLog.Log($"HammerImpactAudio: Awake, audioSource found: {audioSource != null}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameLog.Log($"HammerImpactAudio: collided with {collision.collider.name} (tag: {collision.collider.tag})");

        if (collision.collider.CompareTag("Stake"))
        {
            GameLog.Log("HammerImpactAudio: tagged Stake, suppressing thud");
            return; // Let HammerStrikeTrigger own this hit.
        }

        GameLog.Log($"HammerImpactAudio: playing thud, clip assigned: {thudClip != null}, audioSource null: {audioSource == null}");
        audioSource.PlayOneShot(thudClip);
    }
}
