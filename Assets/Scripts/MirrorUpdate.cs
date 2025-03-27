using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorUpdate : MonoBehaviour
{
    private Camera _mirrorCamera; // store a reference to myself

    void Start()
    {
        // find the camera on start
        _mirrorCamera = GetComponent<Camera>(); 
    }

    void OnPreRender()
    {
        // // check if Camera exists and if render texture assigned
        if (_mirrorCamera && _mirrorCamera.targetTexture != null)
        {
            _mirrorCamera.Render(); // force camera to render each frame
        }
    }
}
