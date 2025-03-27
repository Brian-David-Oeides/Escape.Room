using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroyPointLight : MonoBehaviour
{
    public float duration = 0.25f; // store duration of point light 

    void Start()
    {
        // Destroy PointLight in 0.25 seconds
        Destroy(gameObject, duration);
    }
}
