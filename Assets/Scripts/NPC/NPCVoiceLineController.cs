using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCVoiceLineController : MonoBehaviour
{
    public event System.Action OnLineStarted;

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

    [Header("Dormant Reassurance Loop")]
    [SerializeField] private float dormantLineInterval = 35f;
    private float dormantLineTimer = 0f;

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

        // OnStateChanged can never fire into Dormant (it's the starting state and
        // states only escalate forward), so the first-contact line needs an explicit
        // kickoff here. Gate on GameManager's loading flag rather than CurrentState:
        // save data restoration is deferred by two WaitForEndOfFrame yields in
        // GameManager.RestoreSaveDataAfterSceneLoad, which runs well after this Start()
        // - so CurrentState would still read Dormant here even on a loaded game that
        // already progressed past it.
        bool loadingFromSave = GameManager.Instance != null && GameManager.Instance.IsLoadingFromSave();
        if (!loadingFromSave)
        {
            PlayNextInSequence(dormantLines, ref dormantIndex);
        }
        dormantLineTimer = dormantLineInterval;
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
                if (voiceAudioSource != null && !voiceAudioSource.isPlaying)
                {
                    voiceAudioSource.PlayOneShot(huntingChaseLoopClip);
                    DebugLog("Playing hunting chase loop line");
                }
                chaseLoopTimer = chaseLoopInterval;
            }
        }

        if (behaviorController.CurrentState == NPCBehaviorController.BehaviorState.Dormant)
        {
            dormantLineTimer -= Time.deltaTime;

            if (dormantLineTimer <= 0f)
            {
                PlayNextInSequence(dormantLines, ref dormantIndex);
                dormantLineTimer = dormantLineInterval;
            }
        }
    }

    void HandleStateChanged(NPCBehaviorController.BehaviorState previous, NPCBehaviorController.BehaviorState current)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsLoadingFromSave())
        {
            DebugLog($"Skipping voice line for restored state {current} (loading from save)");
            return;
        }

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
        if (behaviorController.isPermanentlyDefeated) return;
        if (voiceAudioSource == null || voiceAudioSource.isPlaying) return;

        voiceAudioSource.PlayOneShot(huntingAttackClip);
        DebugLog("Playing hunting attack line");
    }

    public void PlaySabotageLine()
    {
        if (behaviorController.isPermanentlyDefeated) return;
        PlayNextInSequence(sabotageLines, ref sabotageIndex);
    }

    public void StopCurrentLine()
    {
        voiceAudioSource?.Stop();
        DebugLog("Voice line stopped - death cry taking priority");
    }

    void PlayNextInSequence(AudioClip[] clips, ref int index)
    {
        if (behaviorController.isPermanentlyDefeated) return;
        if (clips == null || clips.Length == 0) return;
        if (voiceAudioSource != null && voiceAudioSource.isPlaying) return;

        if (behaviorController.CurrentState != NPCBehaviorController.BehaviorState.Hunting)
        {
            OnLineStarted?.Invoke();
        }

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
