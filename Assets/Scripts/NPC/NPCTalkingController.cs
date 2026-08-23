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
        if (behaviorController.isPermanentlyDefeated || behaviorController.combatInterrupted) return;

        IsTalking = true;
        if (agent != null) agent.isStopped = true;

        if (behaviorController.CurrentState == NPCBehaviorController.BehaviorState.Agitated)
        {
            animator.SetTrigger("Talk2");
        }
        else
        {
            animator.SetTrigger("Talk");
        }

        StartCoroutine(EndTalkingAfterDuration(
            behaviorController.CurrentState == NPCBehaviorController.BehaviorState.Agitated
                ? Mathf.Max(yellingMinimumDuration, clipLength)
                : Mathf.Max(talkingMinimumDuration, clipLength)));
    }

    void HandleLineInterrupted()
    {
        StopAllCoroutines();
        EndTalking();
    }

    IEnumerator EndTalkingAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        EndTalking();
    }

    void EndTalking()
    {
        IsTalking = false;
        animator.SetTrigger("StopTalk");
        if (agent != null && !behaviorController.combatInterrupted && !behaviorController.isPermanentlyDefeated)
            agent.isStopped = false;
    }
}
