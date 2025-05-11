using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CustomSocketInteractor : MonoBehaviour
{
    [Tooltip("Name tag to identify socketable objects")]
    public string validTag = "Socketable";

    [Tooltip("The transform where the object should snap")]
    public Transform socketAttachPoint;

    [Tooltip("Material for the hover mesh visual")]
    public Material hoverMaterial;

    [Tooltip("Show hover visual while object is still being held")]
    public bool showHoverWhileHeld = false;

    [Tooltip("Scale of the hover visual")]
    public float hoverScale = 1f;

    private Collider _currentHover;
    private XRGrabInteractable _grabCandidate;
    private MeshFilter[] _hoverMeshFilters;

    private bool _isSnapped = false;
    private bool _renderHoverVisual = false;

    private Dictionary<XRGrabInteractable, InteractionLayerMask> _originalInteractionLayers = new();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(validTag)) return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab == null) return;

        // save original interaction layer
        if (!_originalInteractionLayers.ContainsKey(grab))
            _originalInteractionLayers[grab] = grab.interactionLayers;

        _currentHover = other;
        _grabCandidate = grab;
        _hoverMeshFilters = other.GetComponentsInChildren<MeshFilter>();

        _renderHoverVisual = true; // enable hover drawing

        // subscribe to release event if hover visuals are allowed
        if (!grab.isSelected || showHoverWhileHeld)
        {
            grab.selectExited.AddListener(OnGrabReleased);
        }

        _isSnapped = false; // reset snap state for new entry
    }

    private void OnTriggerExit(Collider other)
    {
        if (_grabCandidate != null)
            _grabCandidate.selectExited.RemoveListener(OnGrabReleased);

        _currentHover = null;
        _grabCandidate = null;
        _hoverMeshFilters = null;
        _isSnapped = false;
        _renderHoverVisual = false;
    }

    private void OnGrabReleased(SelectExitEventArgs args)
    {
        if (_currentHover == null || _grabCandidate == null) return;

        float distance = Vector3.Distance(_currentHover.transform.position, socketAttachPoint.position);
        if (distance > 0.05f) return;

        // Freeze physics
        Rigidbody rb = _currentHover.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Snap to socket
        _currentHover.transform.SetPositionAndRotation(socketAttachPoint.position, socketAttachPoint.rotation);

        // Disable future grabbing
        _grabCandidate.interactionLayers = new InteractionLayerMask();

        //StartCoroutine(FadeOutHoverMaterial(0.3f));
        if (hoverMaterial != null)
        {
            Color c = hoverMaterial.color;
            hoverMaterial.color = new Color(c.r, c.g, c.b, 0f); // instantly hide it
        }


        // Cleanup and disable hover visual
        _grabCandidate.selectExited.RemoveListener(OnGrabReleased);
        _isSnapped = true;
        _renderHoverVisual = false;

        _currentHover = null;
        _grabCandidate = null;
        _hoverMeshFilters = null;
    }

    private void OnRenderObject()
    {
        if (!_renderHoverVisual || hoverMaterial == null || _hoverMeshFilters == null) return;

        foreach (var meshFilter in _hoverMeshFilters)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null) continue;

            Matrix4x4 matrix = Matrix4x4.TRS(
                socketAttachPoint.position,
                socketAttachPoint.rotation,
                meshFilter.transform.lossyScale * hoverScale
            );

            for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
            {
                Graphics.DrawMesh(meshFilter.sharedMesh, matrix, hoverMaterial, gameObject.layer, null, i);
            }
        }
    }

}