using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainCutterReciever : MonoBehaviour
{
    [Header("Assign the real chain Rigidbody here")]
    public Rigidbody chainBody;

    [Header("Delay before disabling after hit")]
    public float disableDelay = 2f;

    private bool _hasBeenCut = false;

    public void CutChain()
    {
        if (chainBody != null)
        {
            _hasBeenCut = true;
            chainBody.isKinematic = false;
            chainBody.useGravity = true;
            Debug.Log("Chain cut!");
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (_hasBeenCut && collision.gameObject.CompareTag("Floor")) // tag "Floor"
        {
            Debug.Log("Chain hit the floor. Will disable soon...");
            StartCoroutine(DisableChainAfterDelay());
        }
    }
    private IEnumerator DisableChainAfterDelay()
    {
        yield return new WaitForSeconds(disableDelay);
        gameObject.SetActive(false);
    }
}
