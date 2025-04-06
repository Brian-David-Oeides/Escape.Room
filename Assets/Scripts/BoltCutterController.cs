using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoltCutterController : MonoBehaviour
{
    [Header("Blade Transforms")]
    public Transform bladeLeftPivot;
    public Transform bladeRightPivot;

    [Header("Cutting Rotation")]
    public float maxRotation = 30f;
    public float speed = 90f; // degrees per second
    public float holdClosedDuration = 1f;

    private float currentRotation = 0f;
    private bool isCutting = false;
    private bool isResetting = false;

    void Update()
    {
        if (isCutting && !isResetting)
        {
            if (currentRotation < maxRotation)
            {
                float rotationStep = speed * Time.deltaTime;
                rotationStep = Mathf.Min(rotationStep, maxRotation - currentRotation);
                bladeLeftPivot.localRotation *= Quaternion.Euler(0, 0, -rotationStep);
                bladeRightPivot.localRotation *= Quaternion.Euler(0, 0, rotationStep);
                currentRotation += rotationStep;

                if (currentRotation >= maxRotation)
                {
                    StartCoroutine(ResetBlades());
                }
            }
        }
    }

    public void TriggerCut()
    {
        if (!isCutting)
        {
            isCutting = true;
            currentRotation = 0f;
            BoltCutterCutState.IsCutting = true;
        }
    }

    private IEnumerator ResetBlades()
    {
        isResetting = true;
        yield return new WaitForSeconds(holdClosedDuration);

        float resetRotation = 0f;
        while (resetRotation < maxRotation)
        {
            float rotationStep = speed * Time.deltaTime;
            rotationStep = Mathf.Min(rotationStep, maxRotation - resetRotation);
            bladeLeftPivot.localRotation *= Quaternion.Euler(0, 0, rotationStep);
            bladeRightPivot.localRotation *= Quaternion.Euler(0, 0, -rotationStep);
            resetRotation += rotationStep;
            yield return null;
        }

        isCutting = false;
        isResetting = false;
        BoltCutterCutState.IsCutting = false; // reset the flag
    }
}




