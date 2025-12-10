using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValveOilDetection : PuzzleBase
{
    [Header("Socket Rotator Reference")]
    [SerializeField] private SocketRotator _socketRotator;

    [Header("Oil Detection")]
    [SerializeField] private bool _isOiled = false;

    protected override void Start()
    {
        base.Start(); // CRITICAL: Loads saved completion state!

        // If already oiled from save, enable socket rotator
        if (isCompleted)
        {
            _isOiled = true;
            if (_socketRotator != null)
            {
                _socketRotator.enabled = true;
            }
            DebugLog("Valve already oiled from save - SocketRotator enabled");
        }
        // Initially disable the socket rotator if not oiled
        else if (_socketRotator != null && !_isOiled)
        {
            _socketRotator.enabled = false;
        }
    }

    // This method is called when particles collide with this object
    private void OnParticleCollision(GameObject other)
    {
        // Prevent re-triggering if already completed
        if (isCompleted || _isOiled)
        {
            return;
        }

        // Check if the collision is from the oil spray particles
        if (other.name == "Oil_Stream")
        {
            DebugLog("Valve has been oiled! Socket Rotator enabled.");

            // Mark as oiled (permanent)
            _isOiled = true;

            // Enable the socket rotator functionality
            if (_socketRotator != null)
            {
                _socketRotator.enabled = true;
            }

            // Complete the puzzle (saves state)
            CompletePuzzle();
        }
    }

    /// <summary>
    /// Override CompletePuzzle to log valve oiling completion
    /// </summary>
    public override void CompletePuzzle()
    {
        DebugLog("Valve oiling puzzle completed!");

        // Call base to handle save system and fire OnPuzzleCompleted event
        base.CompletePuzzle();
    }

    /// <summary>
    /// Override ApplyCompletedStateVisuals to enable SocketRotator when loading
    /// </summary>
    protected override void ApplyCompletedStateVisuals()
    {
        // Call base to handle colliders/renderers
        base.ApplyCompletedStateVisuals();

        // Apply oiled state
        _isOiled = true;
        if (_socketRotator != null)
        {
            _socketRotator.enabled = true;
        }

        DebugLog("Valve oiled state restored from save");
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
