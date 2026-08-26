using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCVoiceLineController : MonoBehaviour
{
    public event System.Action<float> OnLineStarted;
    public event System.Action OnLineInterrupted;

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

    [System.Serializable]
    public struct SabotageLineEntry
    {
        public SabotageLineCategory category;
        public AudioClip clip;
    }

    [Header("Sabotage Lines")]
    [SerializeField] private List<SabotageLineEntry> sabotageLineEntries = new List<SabotageLineEntry>();

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private int dormantIndex = 0;
    private int observingIndex = 0;
    private int approachingIndex = 0;
    private int agitatedIndex = 0;
    private int huntingIndex = 0;

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

        StopCurrentLine("new state entry taking priority", notifyInterrupted: false);

        switch (current)
        {
            case NPCBehaviorController.BehaviorState.Dormant:
                // No incoming line needed when returning to Dormant - shouldn't normally happen mid-game
                break;
            case NPCBehaviorController.BehaviorState.Observing:
                PlayNextInSequence(observingLines, ref observingIndex, forcePlay: true);
                break;
            case NPCBehaviorController.BehaviorState.Approaching:
                PlayNextInSequence(approachingLines, ref approachingIndex, forcePlay: true);
                break;
            case NPCBehaviorController.BehaviorState.Agitated:
                PlayNextInSequence(agitatedLines, ref agitatedIndex, forcePlay: true);
                break;
            case NPCBehaviorController.BehaviorState.Hunting:
                PlayNextInSequence(huntingLines, ref huntingIndex, forcePlay: true);
                chaseLoopTimer = chaseLoopInterval;
                break;
        }
    }

    void HandlePuzzleSolvedSameState()
    {
        StopCurrentLine("new line taking priority", notifyInterrupted: false);

        switch (behaviorController.CurrentState)
        {
            case NPCBehaviorController.BehaviorState.Dormant:
                PlayNextInSequence(dormantLines, ref dormantIndex, forcePlay: true);
                break;
            case NPCBehaviorController.BehaviorState.Observing:
                PlayNextInSequence(observingLines, ref observingIndex, forcePlay: true);
                break;
            case NPCBehaviorController.BehaviorState.Approaching:
                PlayNextInSequence(approachingLines, ref approachingIndex, forcePlay: true);
                break;
            case NPCBehaviorController.BehaviorState.Agitated:
                PlayNextInSequence(agitatedLines, ref agitatedIndex, forcePlay: true);
                break;
            case NPCBehaviorController.BehaviorState.Hunting:
                PlayNextInSequence(huntingLines, ref huntingIndex, forcePlay: true);
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

    public void PlaySabotageLine(SabotageLineCategory category)
    {
        if (behaviorController.isPermanentlyDefeated) return;

        AudioClip clip = GetSabotageClipForCategory(category);
        if (clip == null)
        {
            DebugLog($"No sabotage clip assigned for category: {category}");
            return;
        }

        StopCurrentLine("sabotage line taking priority", notifyInterrupted: false);
        if (behaviorController.CurrentState != NPCBehaviorController.BehaviorState.Hunting)
        {
            OnLineStarted?.Invoke(clip.length);
        }
        voiceAudioSource?.PlayOneShot(clip);
        DebugLog($"Playing sabotage line for category: {category}");
    }

    private AudioClip GetSabotageClipForCategory(SabotageLineCategory category)
    {
        foreach (var entry in sabotageLineEntries)
        {
            if (entry.category == category) return entry.clip;
        }
        return null;
    }

    public void StopCurrentLine(string reason = "priority interrupt", bool notifyInterrupted = true)
    {
        bool wasPlaying = voiceAudioSource != null && voiceAudioSource.isPlaying;
        voiceAudioSource?.Stop();

        if (wasPlaying && notifyInterrupted)
        {
            DebugLog($"Voice line stopped - {reason}");
            OnLineInterrupted?.Invoke();
        }
    }

    void PlayNextInSequence(AudioClip[] clips, ref int index, bool forcePlay = false)
    {
        if (behaviorController.isPermanentlyDefeated) return;
        if (clips == null || clips.Length == 0) return;
        if (!forcePlay && voiceAudioSource != null && voiceAudioSource.isPlaying) return;

        if (behaviorController.CurrentState != NPCBehaviorController.BehaviorState.Hunting)
        {
            OnLineStarted?.Invoke(clips[index].length);
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
