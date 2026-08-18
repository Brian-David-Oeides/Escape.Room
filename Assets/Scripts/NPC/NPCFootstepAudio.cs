using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCFootstepAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource footstepAudioSource;
    private NPCBehaviorController behaviorController;

    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] walkClips;
    [SerializeField] private AudioClip[] runClips;

    [Header("Step Distances")]
    [SerializeField] private float walkStepDistance = 0.6f;
    [SerializeField] private float runStepDistance = 0.4f;

    private Vector3 lastPosition;
    private float distanceAccumulated = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    void Start()
    {
        behaviorController = GetComponent<NPCBehaviorController>();
        lastPosition = transform.position;
    }

    void Update()
    {
        if (behaviorController.isPermanentlyDefeated || behaviorController.combatInterrupted) return;

        Vector3 currentPosition = transform.position;

        Vector3 currentFlat = new Vector3(currentPosition.x, 0f, currentPosition.z);
        Vector3 lastFlat = new Vector3(lastPosition.x, 0f, lastPosition.z);
        float frameDistance = Vector3.Distance(currentFlat, lastFlat);

        distanceAccumulated += frameDistance;
        lastPosition = currentPosition;

        bool isRunning = behaviorController.CurrentState == NPCBehaviorController.BehaviorState.Hunting;
        float stepThreshold = isRunning ? runStepDistance : walkStepDistance;

        if (distanceAccumulated >= stepThreshold)
        {
            PlayFootstep(isRunning);
            distanceAccumulated = 0f;
        }
    }

    private void PlayFootstep(bool isRunning)
    {
        AudioClip[] clips = isRunning ? runClips : walkClips;

        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        footstepAudioSource?.PlayOneShot(clip);

        DebugLog($"Playing {(isRunning ? "run" : "walk")} footstep: {clip.name}");
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
            GameLog.Log($"[NPCFootstepAudio] {message}");
    }
}
