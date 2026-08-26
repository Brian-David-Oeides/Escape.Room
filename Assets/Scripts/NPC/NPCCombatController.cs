using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCCombatController : MonoBehaviour, ISaveable
{
    private HandHaptics[] allHandHaptics;

    [Header("References")]
    private NavMeshAgent agent;
    private NPCBehaviorController behaviorController;
    private NPCVoiceLineController voiceController;
    private NPCTalkingController talkingController;
    private Animator animator;
    private Transform playerTransform;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attackCooldown = 4.0f;
    [SerializeField] private float attackDamage = 20f;

    [Header("Counter-Attack Window")]
    [SerializeField] private float counterWindowDelay = 0.4f;
    [SerializeField] private float counterWindowDuration = 1.2f;
    [SerializeField] private float damageDelay = 1.0f;

    [Header("Defeat Settings")]
    [SerializeField] private int hitsToDefeat = 3;
    [SerializeField] private float deathTiltAngleX = 7.5f;
    [SerializeField] private float deathTiltAngleZ = 1f;
    [SerializeField] private float deathPositionY = 0.08f;

    [Header("Combat Audio")]
    [SerializeField] private AudioClip attackWhooshSound;
    [SerializeField] private AudioClip punchImpactSound;
    [SerializeField] private AudioClip npcPainGruntSound;
    [SerializeField] private AudioClip playerDamageGruntSound;
    [SerializeField] private AudioClip npcGroundImpactSound;
    [SerializeField] private AudioClip npcDeathCrySound;
    [SerializeField] private AudioClip npcDeathGroundImpactSound;

    private AudioSource audioSource;

    // State tracking
    private bool isAttacking = false;
    private bool counterWindowOpen = false;
    private bool playerCounteredThisAttack = false;
    private float attackCooldownTimer = 0f;
    private int currentHitCount = 0;
    private bool isDefeated = false;

    void Start()
    {
        behaviorController = GetComponent<NPCBehaviorController>();
        voiceController = GetComponent<NPCVoiceLineController>();
        talkingController = GetComponent<NPCTalkingController>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();

        GameObject xrRig = GameObject.Find("XR Origin (XR Rig)");
        if (xrRig != null)
            playerTransform = xrRig.transform;
        else
            GameLog.LogError("[NPCCombatController] Could not find XR Origin (XR Rig)!");

        allHandHaptics = FindObjectsByType<HandHaptics>(FindObjectsSortMode.None);
    }

    void Update()
    {
        if (isDefeated) return;
        if (playerTransform == null) return;
        if (behaviorController.combatInterrupted) return;  

        // Only attack during Hunting state
        if (behaviorController.CurrentState != NPCBehaviorController.BehaviorState.Hunting)
        {
            isAttacking = false;
            counterWindowOpen = false;
            return;
        }

        // Tick cooldown
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        // Attempt attack if in range and ready
        if (!isAttacking && attackCooldownTimer <= 0f)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange)
                StartCoroutine(ExecuteAttack());
        }
    }

    IEnumerator ExecuteAttack()
    {
        isAttacking = true;
        playerCounteredThisAttack = false;
        attackCooldownTimer = attackCooldown;

        talkingController?.ForceStopTalking("attack triggered");

        // Trigger attack animation
        animator.SetTrigger("Attack");
        audioSource?.PlayOneShot(attackWhooshSound);
        voiceController?.PlayHuntingAttackLine();
        GameLog.Log("[NPCCombatController] Attack triggered");

        // Wait before opening counter window
        yield return new WaitForSeconds(counterWindowDelay);

        // Check if player is still in range before committing to attack
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > attackRange)
        {
            GameLog.Log("[NPCCombatController] Player escaped range - attack cancelled");
            isAttacking = false;
            yield break;
        }

        // Open counter-attack window
        counterWindowOpen = true;
        GameLog.Log("[NPCCombatController] Counter window OPEN");

        // Schedule damage to player after delay
        StartCoroutine(ScheduleDamage());

        // Hold window open for duration
        yield return new WaitForSeconds(counterWindowDuration);

        // Close window
        counterWindowOpen = false;
        GameLog.Log("[NPCCombatController] Counter window CLOSED");

        // Wait for animation to finish before allowing next attack
        yield return new WaitForSeconds(1.0f);

        isAttacking = false;
    }

    IEnumerator ScheduleDamage()
    {
        yield return new WaitForSeconds(damageDelay);

        // Only deal damage if player did not counter
        if (!playerCounteredThisAttack)
        {
            HealthEnergyManager.Instance?.TakeDamage(attackDamage);
            GameLog.Log($"[NPCCombatController] Player hit for {attackDamage} damage");
            foreach (HandHaptics h in allHandHaptics)
            {
                h.TriggerHaptic(0.9f, 0.5f);
                h.TriggerHapticWithSound(playerDamageGruntSound, 0.9f);
            }
        }
    }

    public void OnHurtboxHit()
    {
        if (!counterWindowOpen) return;
        if (playerCounteredThisAttack) return;
        if (isDefeated) return;

        playerCounteredThisAttack = true;
        counterWindowOpen = false;

        RegisterHit();
    }

    public void PlayPunchSound()
    {
        audioSource?.PlayOneShot(punchImpactSound);
    }

    public void PlayPainSound()
    {
        audioSource?.PlayOneShot(npcPainGruntSound);
    }

    void RegisterHit()
    {
        currentHitCount++;
        GameLog.Log($"[NPCCombatController] NPC hit {currentHitCount}/{hitsToDefeat}");

        PlayPainSound();

        if (agent != null)
            agent.isStopped = true;

        behaviorController.combatInterrupted = true;

        if (currentHitCount >= hitsToDefeat)
        {
            // Death animation's Y motion is fully baked into pose - 
            // leave root offset untouched, do not enter Falling/GettingUp phase
            StartCoroutine(ExecuteDefeat());
        }
        else
        {
            behaviorController.currentCombatPhase = NPCBehaviorController.CombatPhase.Falling;
            StartCoroutine(TransitionToGetUp());
            StartCoroutine(PlayGroundImpactSound());
            talkingController?.ForceStopTalking("combat hit");
            animator.SetTrigger("Hit");
            StartCoroutine(ResumeAfterStagger());
        }
    }

    IEnumerator ResumeAfterStagger()
    {
        // Total Hit -> Fall -> GetUp -> Idle sequence is exactly 5.55s
        yield return new WaitForSeconds(5.55f);

        if (agent != null && !isDefeated)
        {
            behaviorController.currentCombatPhase = NPCBehaviorController.CombatPhase.None;
            behaviorController.combatInterrupted = false;
            agent.isStopped = false;
        }
    }
    IEnumerator PlayGroundImpactSound()
    {
        yield return new WaitForSeconds(1.33f);
        audioSource?.PlayOneShot(npcGroundImpactSound);
    }

    IEnumerator TransitionToGetUp()
    {
        // Shoulder Hit And Fall plays for exactly 2.85s before Getting Up begins
        yield return new WaitForSeconds(2.85f);
        behaviorController.currentCombatPhase = NPCBehaviorController.CombatPhase.GettingUp;
    }

    IEnumerator ExecuteDefeat()
    {
        isDefeated = true;
        behaviorController.SetDefeated(true);
        talkingController?.ForceStopTalking("death cry taking priority");

        if (agent != null)
            agent.isStopped = true;

        isAttacking = false;
        counterWindowOpen = false;

        animator.SetTrigger("Death");
        audioSource?.PlayOneShot(npcDeathCrySound);
        GameLog.Log("[NPCCombatController] NPC defeated!");

        // Wait for the body to hit the floor before playing impact sound
        yield return new WaitForSeconds(1.8f);
        audioSource?.PlayOneShot(npcDeathGroundImpactSound);

        // NPC remains active and frozen on the Death animation's last frame
        // No gameObject.SetActive(false) - character stays visible permanently

        // Wait for the animation to fully settle before freezing
        yield return new WaitForSeconds(1.2f);

        // Disable Animator so it stops overwriting Transform rotation each frame
        animator.enabled = false;

        // Fully disable the NavMeshAgent - it's permanently retired and was 
        // silently re-leveling X/Z rotation via updateUpAxis even with isStopped = true
        if (agent != null)
            agent.enabled = false;

        // Now apply the final position and rotation correction - it will stick 
        // since neither Animator nor NavMeshAgent control the Transform anymore
        Vector3 currentEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(deathTiltAngleX, currentEuler.y, deathTiltAngleZ);

        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, deathPositionY, currentPos.z);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = counterWindowOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    #region ISaveable Implementation

    public string SaveID => "npc_combat_defeat";

    public void SaveState(SaveData saveData)
    {
        if (isDefeated)
        {
            MoveableObjectData objectState = new MoveableObjectData(
                SaveID, transform.position, transform.rotation, true, "");
            saveData.moveableObjects.Add(objectState);
        }
    }

    public void LoadState(SaveData saveData)
    {
        MoveableObjectData savedState = saveData.moveableObjects.Find(obj => obj.objectID == SaveID);
        if (savedState != null)
        {
            isDefeated = true;

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Play("Death", 0, 1f);
                animator.Update(0f);
                animator.enabled = false;
            }

            if (agent != null) agent.enabled = false;
            transform.position = savedState.position;
            transform.rotation = savedState.rotation;

            if (behaviorController != null)
                behaviorController.SetDefeated(true);
        }
    }

    #endregion
}
