using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"BladeTrigger collided with: {other.name}");

        ChainCutterReciever receiver = other.GetComponent<ChainCutterReciever>();
        if (receiver != null && BoltCutterCutState.IsCutting)
        {
            Debug.Log("Receiver found and IsCutting is true. Calling CutChain()");
            receiver.CutChain();
        }
    }
}
