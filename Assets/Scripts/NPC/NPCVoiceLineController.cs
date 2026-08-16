using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCVoiceLineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource voiceAudioSource;
    private NPCBehaviorController behaviorController;
    private NPCCombatController combatController;

    [Header("Sequential State Lines")]
    [SerializeField] private AudioClip[] dormantLines;
    [SerializeField] private AudioClip[] observingLines;
    [SerializeField] private AudioClip[] approachingLines;
    [SerializeField] private AudioClip[] agitatedLines;
    [SerializeField] private AudioClip[] huntingLines;

    [Header("Hunting Special Triggers")]
    [SerializeField] private AudioClip huntingChaseLoopClip;
    [SerializeField] private AudioClip huntingAttackClip;
    [SerializeField] private float chaseLoopInterval = 12f;

    [Header("Sabotage Lines")]
    [SerializeField] private AudioClip[] sabotageLines;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private int dormantIndex = 0;
    private int observingIndex = 0;
    private int approachingIndex = 0;
    private int agitatedIndex = 0;
    private int huntingIndex = 0;
    private int sabotageIndex = 0;

    private float chaseLoopTimer = 0f;

    void Start()
    {
        behaviorController = GetComponent<NPCBehaviorController>();
        combatController = GetComponent<NPCCombatController>();

        behaviorController.OnStateChanged += HandleStateChanged;
        behaviorController.OnPuzzleSolvedSameState += HandlePuzzleSolvedSameState;
    }

    void OnDestroy()
    {
        if (behaviorController != null)
        {
            behaviorController.OnStateChanged -= HandleStateChanged;
            behaviorController.OnPuzzleSolvedSameState -= HandlePuzzleSolvedSameState;
        }
    }

    void Update()
    {
        if (behaviorController == null) return;

        bool activelyHunting = behaviorController.CurrentState == NPCBehaviorController.BehaviorState.Hunting
                             && !behaviorController.combatInterrupted
                             && !behaviorController.isPermanentlyDefeated;

        if (activelyHunting)
        {
            chaseLoopTimer -= Time.deltaTime;

            if (chaseLoopTimer <= 0f)
            {
                voiceAudioSource?.PlayOneShot(huntingChaseLoopClip);
                DebugLog("Playing hunting chase loop line");
                chaseLoopTimer = chaseLoopInterval;
            }
        }
    }

    void HandleStateChanged(NPCBehaviorController.BehaviorState previous, NPCBehaviorController.BehaviorState current)
    {
        switch (current)
        {
            case NPCBehaviorController.BehaviorState.Dormant:
                // No incoming line needed when returning to Dormant - shouldn't normally happen mid-game
                break;
            case NPCBehaviorController.BehaviorState.Observing:
                PlayNextInSequence(observingLines, ref observingIndex);
                break;
            case NPCBehaviorController.BehaviorState.Approaching:
                PlayNextInSequence(approachingLines, ref approachingIndex);
                break;
            case NPCBehaviorController.BehaviorState.Agitated:
                PlayNextInSequence(agitatedLines, ref agitatedIndex);
                break;
            case NPCBehaviorController.BehaviorState.Hunting:
                PlayNextInSequence(huntingLines, ref huntingIndex);
                chaseLoopTimer = chaseLoopInterval;
                break;
        }
    }

    void HandlePuzzleSolvedSameState()
    {
        switch (behaviorController.CurrentState)
        {
            case NPCBehaviorController.BehaviorState.Dormant:
                PlayNextInSequence(dormantLines, ref dormantIndex);
                break;
            case NPCBehaviorController.BehaviorState.Observing:
                PlayNextInSequence(observingLines, ref observingIndex);
                break;
            case NPCBehaviorController.BehaviorState.Approaching:
                PlayNextInSequence(approachingLines, ref approachingIndex);
                break;
            case NPCBehaviorController.BehaviorState.Agitated:
                PlayNextInSequence(agitatedLines, ref agitatedIndex);
                break;
            case NPCBehaviorController.BehaviorState.Hunting:
                PlayNextInSequence(huntingLines, ref huntingIndex);
                break;
        }
    }

    public void PlayHuntingAttackLine()
    {
        voiceAudioSource?.PlayOneShot(huntingAttackClip);
        DebugLog("Playing hunting attack line");
    }

    public void PlaySabotageLine()
    {
        PlayNextInSequence(sabotageLines, ref sabotageIndex);
    }

    void PlayNextInSequence(AudioClip[] clips, ref int index)
    {
        if (clips == null || clips.Length == 0) return;

        voiceAudioSource?.PlayOneShot(clips[index]);
        DebugLog($"Playing line {index + 1}/{clips.Length}: {clips[index].name}");

        index++;
        if (index >= clips.Length)
        {
            DebugLog("Sequence exhausted, wrapping back to start");
            index = 0;
        }
    }

    void DebugLog(string message)
    {
        if (showDebugLogs)
            GameLog.Log($"[NPCVoiceLineController] {message}");
    }
}
