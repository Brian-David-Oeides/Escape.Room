using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GazeDebugger : MonoBehaviour
{
    private XRGazeInteractor gazeInteractor;
    private LineRenderer lineRenderer;

    void Start()
    {
        gazeInteractor = GetComponent<XRGazeInteractor>();

        // Add a LineRenderer to visualize the ray
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        //lineRenderer.color = Color.red;
        lineRenderer.widthMultiplier = 0.01f;
        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        if (gazeInteractor != null && lineRenderer != null)
        {
            // Show the gaze ray
            Vector3 startPoint = transform.position;
            Vector3 endPoint = startPoint + transform.forward * 10f; // 10 meter ray

            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);

            // Debug what we're hitting
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 10f))
            {
                Debug.Log("Gaze ray hit: " + hit.collider.gameObject.name + " on layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
            }
        }
    }
}