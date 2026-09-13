using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValveOilDetection : PuzzleBase
{
    [Header("Socket Rotator Reference")]
    [SerializeField] private SocketRotator _socketRotator;

    [Header("Oil Detection")]
    [SerializeField] private bool _isOiled = false;

    [Header("Miss Detection")]
    [Tooltip("Deliberate approximation: doesn't cover the full 5-10s particle lifetime, only enough to catch an aimed spray landing quickly.")]
    [SerializeField] private float _missCheckDelay = 2.5f;

    private bool _hasHitDuringThisAttempt = false;
    private int _missCount = 0;

    private MissCheckRunner _missCheckRunner;
    private Coroutine _pendingMissCheck;

    // Dedicated coroutine host, separate from this component, so the delayed
    // miss-check keeps running even if this component/GameObject is disabled mid-check.
    private class MissCheckRunner : MonoBehaviour { }

    private MissCheckRunner GetMissCheckRunner()
    {
        if (_missCheckRunner == null)
        {
            GameObject runnerObject = new GameObject("ValveOilDetection_MissCheckRunner");
            runnerObject.transform.SetParent(transform, false);
            _missCheckRunner = runnerObject.AddComponent<MissCheckRunner>();
        }

        return _missCheckRunner;
    }

    /// <summary>
    /// Wired to OilSprayController.OnSprayAttemptStarted in the scene.
    /// </summary>
    public void OnSprayAttemptStarted()
    {
        GameLog.Log("[ValveOilDetection] TEMP-LOG: OnSprayAttemptStarted received"); // TODO: remove after miss-detection debugging

        _hasHitDuringThisAttempt = false;

        // A new attempt superseding a still-pending check from a rapid prior
        // attempt - cancel it so only one check is ever meaningfully active.
        if (_pendingMissCheck != null)
        {
            GetMissCheckRunner().StopCoroutine(_pendingMissCheck);
            _pendingMissCheck = null;
        }
    }

    /// <summary>
    /// Wired to OilSprayController.OnSprayAttemptEnded in the scene.
    /// </summary>
    public void OnSprayAttemptEnded()
    {
        GameLog.Log($"[ValveOilDetection] TEMP-LOG: OnSprayAttemptEnded received (isCompleted={isCompleted})"); // TODO: remove after miss-detection debugging

        if (isCompleted)
        {
            return;
        }

        MissCheckRunner runner = GetMissCheckRunner();

        if (_pendingMissCheck != null)
        {
            runner.StopCoroutine(_pendingMissCheck);
        }

        _pendingMissCheck = runner.StartCoroutine(CheckForMissAfterDelay());
    }

    private IEnumerator CheckForMissAfterDelay()
    {
        GameLog.Log("[ValveOilDetection] TEMP-LOG: CheckForMissAfterDelay coroutine started, waiting"); // TODO: remove after miss-detection debugging

        yield return new WaitForSeconds(_missCheckDelay);

        _pendingMissCheck = null;

        GameLog.Log($"[ValveOilDetection] TEMP-LOG: CheckForMissAfterDelay resolved (isCompleted={isCompleted}, hasHit={_hasHitDuringThisAttempt})"); // TODO: remove after miss-detection debugging

        if (isCompleted || _hasHitDuringThisAttempt)
        {
            yield break;
        }

        _missCount++;
        DebugLog($"Oil spray missed valve ({_missCount} consecutive miss(es))");

        if (_missCount >= 2)
        {
            GameLog.Log($"[ValveOilDetection] TEMP-LOG: calling RegisterFailedAttempt(puzzleID={puzzleID}, hintThreshold={hintThreshold})"); // TODO: remove after miss-detection debugging
            ClueManager.Instance?.RegisterFailedAttempt(puzzleID, hintThreshold);
        }
    }

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

            // Mark this attempt as a hit (used by the miss-detection check)
            _hasHitDuringThisAttempt = true;

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
