using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPCTalkingController : MonoBehaviour
{
    [Header("References")]
    private NPCBehaviorController behaviorController;
    private NPCVoiceLineController voiceLineController;
    private Animator animator;
    private NavMeshAgent agent;
    private Transform playerTransform;

    [Header("Talking")]
    [SerializeField] private float talkingMinimumDuration = 3.8f; // floor for Talk animation; actual duration is Mathf.Max(this, clip length)
    [SerializeField] private float yellingMinimumDuration = 7.6f; // floor for Talk2 animation; actual duration is Mathf.Max(this, clip length)

    public bool IsTalking { get; private set; }

    private float talkEndTime = -1f;
    private Coroutine talkMonitorCoroutine;
    private float talkSessionStartTime = -1f;

    void Awake()
    {
        behaviorController = GetComponent<NPCBehaviorController>();
        voiceLineController = GetComponent<NPCVoiceLineController>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        voiceLineController.OnLineStarted += StartTalking;
    }

    void Start()
    {
        GameObject xrRig = GameObject.Find("XR Origin (XR Rig)");
        if (xrRig != null)
        {
            playerTransform = xrRig.transform;
        }
        else
        {
            GameLog.LogError("Could not find XR Origin (XR Rig)!");
        }
    }

    void OnDestroy()
    {
        if (voiceLineController != null)
        {
            voiceLineController.OnLineStarted -= StartTalking;
        }
    }

    void Update()
    {
        if (IsTalking)
        {
            FacePlayer();
        }
    }

    void FacePlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    void StartTalking(float clipLength)
    {
        if (behaviorController.isPermanentlyDefeated || behaviorController.combatInterrupted) return;

        float duration = behaviorController.CurrentState == NPCBehaviorController.BehaviorState.Agitated
            ? Mathf.Max(yellingMinimumDuration, clipLength)
            : Mathf.Max(talkingMinimumDuration, clipLength);

        if (talkSessionStartTime < 0f) talkSessionStartTime = Time.time;
        float requestedEndTime = Time.time + duration;
        float absoluteCeiling = talkSessionStartTime + 60f;
        talkEndTime = Mathf.Min(requestedEndTime, absoluteCeiling);

        IsTalking = true;
        if (agent != null) agent.isStopped = true;
        animator.SetTrigger(behaviorController.CurrentState == NPCBehaviorController.BehaviorState.Agitated ? "Talk2" : "Talk");

        if (talkMonitorCoroutine == null)
            talkMonitorCoroutine = StartCoroutine(MonitorTalkEnd());
    }

    IEnumerator MonitorTalkEnd()
    {
        while (Time.time < talkEndTime)
            yield return null;

        talkMonitorCoroutine = null;
        talkSessionStartTime = -1f;
        ForceStopTalking("talk sequence naturally ended");
    }

    public void ForceStopTalking(string reason = "force stop")
    {
        voiceLineController?.StopCurrentLine(reason, notifyInterrupted: false);

        if (!IsTalking) return;

        if (talkMonitorCoroutine != null)
        {
            StopCoroutine(talkMonitorCoroutine);
            talkMonitorCoroutine = null;
        }

        IsTalking = false;
        animator.SetTrigger("StopTalk");
        if (agent != null && !behaviorController.combatInterrupted && !behaviorController.isPermanentlyDefeated)
            agent.isStopped = false;
    }
}
