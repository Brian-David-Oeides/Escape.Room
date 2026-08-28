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
    [SerializeField] private float maxSabotageQueueWaitSeconds = 25f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private int dormantIndex = 0;
    private int observingIndex = 0;
    private int approachingIndex = 0;
    private int agitatedIndex = 0;
    private int huntingIndex = 0;

    private float chaseLoopTimer = 0f;

    private struct PendingSabotageLine
    {
        public SabotageLineCategory category;
        public float timeEnqueued;
    }

    private readonly Queue<PendingSabotageLine> pendingSabotageLines = new Queue<PendingSabotageLine>();

    void Start()
    {
        behaviorController = GetComponent<NPCBehaviorController>();
        combatController = GetComponent<NPCCombatController>();

        behaviorController.OnStateChanged += HandleStateChanged;
        behaviorController.OnPuzzleSolvedSameState += HandlePuzzleSolvedSameState;

        dormantLineTimer = dormantLineInterval;
    }

    // Called by NPCBehaviorController.HandlePuzzlesReset() once the New Game
    // reset sequence (ForceStopTalking, state reset) has already run, so the
    // kickoff line isn't racing GameManager's deferred locomotion-ready callback.
    public void PlayDormantKickoff()
    {
        PlayNextInSequence(dormantLines, ref dormantIndex);
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

        while (pendingSabotageLines.Count > 0 && (voiceAudioSource == null || !voiceAudioSource.isPlaying))
        {
            PendingSabotageLine next = pendingSabotageLines.Dequeue();
            float waitTime = Time.time - next.timeEnqueued;

            if (waitTime > maxSabotageQueueWaitSeconds)
            {
                DebugLog($"Dropping stale queued sabotage line ({next.category}) - waited {waitTime:F1}s");
                continue;
            }

            PlaySabotageClipNow(next.category);
            break;
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
                StopCurrentLine("new state entry taking priority", notifyInterrupted: false);
                PlayNextInSequence(huntingLines, ref huntingIndex, forcePlay: true);
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
                StopCurrentLine("new line taking priority", notifyInterrupted: false);
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

        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            pendingSabotageLines.Enqueue(new PendingSabotageLine { category = category, timeEnqueued = Time.time });
            DebugLog($"Sabotage line queued (voice line in progress): {category}");
            return;
        }

        PlaySabotageClipNow(category);
    }

    private void PlaySabotageClipNow(SabotageLineCategory category)
    {
        AudioClip clip = GetSabotageClipForCategory(category);
        if (clip == null)
        {
            DebugLog($"No sabotage clip assigned for category: {category}");
            return;
        }

        if (behaviorController.CurrentState != NPCBehaviorController.BehaviorState.Hunting)
        {
            OnLineStarted?.Invoke(clip.length);
        }
        voiceAudioSource?.PlayOneShot(clip);
        DebugLog($"Playing sabotage line for category: {category}");
    }

    // Called by NPCCombatController.ExecuteDefeat() so a sabotage line that hasn't
    // played yet doesn't fire on an already-defeated NPC.
    public void ClearPendingSabotageLines()
    {
        pendingSabotageLines.Clear();
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

        // With forcePlay left false (state/puzzle-completion lines outside Hunting no
        // longer pass forcePlay: true), this is what makes those lines DROP - not
        // queue - when a voice line is already playing. The index is deliberately
        // NOT advanced on this early return: these arrays are rotation pools, not
        // one-to-one narrative beats tied to a specific trigger, so the skipped clip
        // simply remains "next up" and may play from a later, unrelated trigger.
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
