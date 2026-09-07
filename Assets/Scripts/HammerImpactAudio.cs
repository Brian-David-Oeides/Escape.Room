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
        GameLog.Log($"HammerImpactAudio: OnCollisionEnter fired on {gameObject.name}, hit {collision.collider.name} (tag: {collision.collider.tag})");

        if (collision.collider.CompareTag("Stake"))
        {
            GameLog.Log("HammerImpactAudio: tagged Stake, suppressing thud");
            return; // Let HammerStrikeTrigger own this hit.
        }

        GameLog.Log($"HammerImpactAudio: about to PlayOneShot - audioSource.enabled: {audioSource.enabled}, audioSource.mute: {audioSource.mute}, thudClip null: {thudClip == null}");
        audioSource.PlayOneShot(thudClip);
    }
}
