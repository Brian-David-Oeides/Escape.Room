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

    private Coroutine currentTalkingCoroutine;

    void Start()
    {
        behaviorController = GetComponent<NPCBehaviorController>();
        voiceLineController = GetComponent<NPCVoiceLineController>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        GameObject xrRig = GameObject.Find("XR Origin (XR Rig)");
        if (xrRig != null)
        {
            playerTransform = xrRig.transform;
        }
        else
        {
            GameLog.LogError("Could not find XR Origin (XR Rig)!");
        }

        voiceLineController.OnLineStarted += HandleLineStarted;
        voiceLineController.OnLineInterrupted += HandleLineInterrupted;
    }

    void OnDestroy()
    {
        if (voiceLineController != null)
        {
            voiceLineController.OnLineStarted -= HandleLineStarted;
            voiceLineController.OnLineInterrupted -= HandleLineInterrupted;
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

    void HandleLineStarted(float clipLength)
    {
        GameLog.Log($"[NPCTalkingController] HandleLineStarted called - currentTalkingCoroutine null? {currentTalkingCoroutine == null}, IsTalking before: {IsTalking}");

        if (behaviorController.isPermanentlyDefeated || behaviorController.combatInterrupted) return;

        if (currentTalkingCoroutine != null)
        {
            StopCoroutine(currentTalkingCoroutine);
        }

        IsTalking = true;
        GameLog.Log($"[NPCTalkingController] IsTalking set to true");
        if (agent != null) agent.isStopped = true;

        if (behaviorController.CurrentState == NPCBehaviorController.BehaviorState.Agitated)
        {
            animator.SetTrigger("Talk2");
        }
        else
        {
            animator.SetTrigger("Talk");
        }

        currentTalkingCoroutine = StartCoroutine(EndTalkingAfterDuration(
            behaviorController.CurrentState == NPCBehaviorController.BehaviorState.Agitated
                ? Mathf.Max(yellingMinimumDuration, clipLength)
                : Mathf.Max(talkingMinimumDuration, clipLength)));
    }

    void HandleLineInterrupted()
    {
        GameLog.Log($"[NPCTalkingController] HandleLineInterrupted called");
        StopAllCoroutines();
        currentTalkingCoroutine = null;
        EndTalking();
    }

    IEnumerator EndTalkingAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        GameLog.Log($"[NPCTalkingController] EndTalkingAfterDuration completed after {duration}s wait");
        currentTalkingCoroutine = null;
        EndTalking();
    }

    void EndTalking()
    {
        GameLog.Log($"[NPCTalkingController] EndTalking called - was IsTalking: {IsTalking}");
        IsTalking = false;
        animator.SetTrigger("StopTalk");
        if (agent != null && !behaviorController.combatInterrupted && !behaviorController.isPermanentlyDefeated)
            agent.isStopped = false;
    }
}
