using UnityEngine;

public class HammerImpactAudio : MonoBehaviour
{
    [SerializeField] private AudioClip thudClip;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameLog.Log($"HammerImpactAudio ({gameObject.name}): hit {collision.collider.name} (tag: {collision.collider.tag})");

        if (collision.collider.CompareTag("Stake"))
        {
            return; // Let HammerStrikeTrigger own this hit.
        }

        GameLog.Log($"HammerImpactAudio ({gameObject.name}): audioSource null={audioSource == null}, thudClip null={thudClip == null}");
        audioSource.PlayOneShot(thudClip);
    }
}
