using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CombatDebugLogger : MonoBehaviour
{
    private Animator animator;
    private Transform hipsBone;
    private NPCBehaviorController behaviorController;
    private NavMeshAgent agent;
    private float logExtraTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        behaviorController = GetComponent<NPCBehaviorController>();
        hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
    }

    void Update()
    {
        if (behaviorController.combatInterrupted)
        {
            logExtraTime = 1.0f; // reset window whenever combat is active
        }

        if (behaviorController.combatInterrupted || logExtraTime > 0f)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[COMBAT DEBUG] Phase: {behaviorController.currentCombatPhase} | Pos: ({transform.position.x:F3}, {transform.position.y:F4}, {transform.position.z:F3}) | Rot: {transform.eulerAngles.y:F1} | BodyPos: ({animator.bodyPosition.x:F3}, {animator.bodyPosition.y:F4}, {animator.bodyPosition.z:F3}) | AgentDest: ({agent.destination.x:F3}, {agent.destination.z:F3}) | CombatInterrupted: {behaviorController.combatInterrupted}");

            if (!behaviorController.combatInterrupted)
                logExtraTime -= Time.deltaTime;
        }
    }
}
