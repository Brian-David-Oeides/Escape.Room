using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeTrigger : MonoBehaviour
{
    [Header("Hint System")]
    [Tooltip("Unique identifier for this puzzle in the hint system")]
    [SerializeField] private string puzzleID = "chain_cutter";

    [Tooltip("Number of failed attempts before hint becomes available")]
    [SerializeField] private int hintThreshold = 3;

    [Header("Collision Settings")]
    [Tooltip("Cooldown between failed attempt registrations")]
    [SerializeField] private float attemptCooldown = 2f;

    private float lastAttemptTime = -999f;

    private readonly Dictionary<Collider, ChainCutterReciever> _receiverCache = new Dictionary<Collider, ChainCutterReciever>();

    private void OnTriggerEnter(Collider other)
    {
        //GameLog.Log($"BladeTrigger collided with: {other.name}");

        ChainCutterReciever receiver = GetCachedReceiver(other);

        if (receiver != null)
        {
            if (BoltCutterCutState.IsCutting)
            {
                GameLog.Log("Receiver found and IsCutting is true. Calling CutChain()");
                receiver.CutChain();
            }
            else if (Time.time - lastAttemptTime >= attemptCooldown) // Cooldown check
            {
                lastAttemptTime = Time.time;
                ClueManager.Instance?.RegisterFailedAttempt(puzzleID, hintThreshold);
                GameLog.Log("Chain hit without cutting - bolt cutter blades not activated");
            }
        }
    }

    // Handles the case where the blade is already resting against the chain
    // before the trigger is squeezed - OnTriggerEnter already fired (as a
    // failed attempt) before IsCutting became true, so no further Enter event
    // occurs once cutting starts. This picks up the cut while blades stay closed.
    private void OnTriggerStay(Collider other)
    {
        if (!BoltCutterCutState.IsCutting) return;

        ChainCutterReciever receiver = GetCachedReceiver(other);
        receiver?.CutChain();
    }

    private void OnTriggerExit(Collider other)
    {
        _receiverCache.Remove(other);
    }

    private ChainCutterReciever GetCachedReceiver(Collider other)
    {
        if (!_receiverCache.TryGetValue(other, out ChainCutterReciever receiver))
        {
            receiver = other.GetComponent<ChainCutterReciever>();
            _receiverCache[other] = receiver;
        }

        return receiver;
    }
}
