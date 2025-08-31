using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValveOilDetection : MonoBehaviour
{
    [Header("Socket Rotator Reference")]
    [SerializeField] private SocketRotator _socketRotator;

    [Header("Oil Detection")]
    [SerializeField] private bool _isOiled = false;

    private void Start()
    {
        // Initially disable the socket rotator if not oiled
        if (_socketRotator != null && !_isOiled)
        {
            _socketRotator.enabled = false;
        }
    }

    // This method is called when particles collide with this object
    private void OnParticleCollision(GameObject other)
    {
        // Check if the collision is from the oil spray particles
        if (other.name == "Oil_Stream" && !_isOiled)
        {
            Debug.Log("Valve has been oiled! Socket Rotator enabled.");

            // Mark as oiled (permanent)
            _isOiled = true;

            // Enable the socket rotator functionality
            if (_socketRotator != null)
            {
                _socketRotator.enabled = true;
            }
        }
    }

    // Public method to check if valve is oiled (optional for other scripts)
    public bool IsOiled()
    {
        return _isOiled;
    }

    // Public method to manually oil the valve (for testing or other mechanics)
    public void OilValve()
    {
        if (!_isOiled)
        {
            _isOiled = true;
            if (_socketRotator != null)
            {
                _socketRotator.enabled = true;
            }
        }
    }
}
