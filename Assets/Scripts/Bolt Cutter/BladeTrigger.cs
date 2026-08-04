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

    private void OnTriggerEnter(Collider other)
    {
        //GameLog.Log($"BladeTrigger collided with: {other.name}");

        ChainCutterReciever receiver = other.GetComponent<ChainCutterReciever>();

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
}
