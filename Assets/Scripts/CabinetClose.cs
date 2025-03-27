using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;

public class CabinetClose : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Vector3 temp = transform.localPosition;
        temp.z = 0.6893f;
        transform.localPosition = temp;
        Destroy(this);
    }
}
