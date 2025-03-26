using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    private int Health_Points = 1;
    private Skeleton_AI _skeletonAI;
    private BoxCollider _thisCollider;

    // Start is called before the first frame update
    void Start()
    {
        _skeletonAI = gameObject.GetComponentInParent<Skeleton_AI>();
        _thisCollider = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Trap")
        {
            Debug.Log("I got killed");
            _thisCollider.enabled = false;
            _skeletonAI.YouDied = true;
        }
    }

}

