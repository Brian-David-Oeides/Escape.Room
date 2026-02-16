using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCCombatController : MonoBehaviour
{
    private HandHaptics[] allHandHaptics;

    [Header("References")]
    private NPCBehaviorController behaviorController;
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

    [Header("Combat Audio")]
    [SerializeField] private AudioClip attackWhooshSound;
    [SerializeField] private AudioClip punchImpactSound;
    [SerializeField] private AudioClip npcPainGruntSound;
    [SerializeField] private AudioClip playerDamageGruntSound;

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
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        GameObject xrRig = GameObject.Find("XR Origin (XR Rig)");
        if (xrRig != null)
            playerTransform = xrRig.transform;
        else
            Debug.LogError("[NPCCombatController] Could not find XR Origin (XR Rig)!");

        allHandHaptics = FindObjectsByType<HandHaptics>(FindObjectsSortMode.None);
    }

    void Update()
    {
        if (isDefeated) return;
        if (playerTransform == null) return;

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

        // Trigger attack animation
        animator.SetTrigger("Attack");
        Debug.Log("[NPCCombatController] Attack triggered");

        audioSource?.PlayOneShot(attackWhooshSound);

        // Wait before opening counter window
        yield return new WaitForSeconds(counterWindowDelay);

        // Open counter-attack window
        counterWindowOpen = true;
        Debug.Log("[NPCCombatController] Counter window OPEN");

        // Schedule damage to player after delay
        // (will be cancelled if player counters successfully)
        StartCoroutine(ScheduleDamage());

        // Hold window open for duration
        yield return new WaitForSeconds(counterWindowDuration);

        // Close window
        counterWindowOpen = false;
        Debug.Log("[NPCCombatController] Counter window CLOSED");

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
            Debug.Log($"[NPCCombatController] Player hit for {attackDamage} damage");
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
        Debug.Log($"[NPCCombatController] NPC hit {currentHitCount}/{hitsToDefeat}");

        PlayPainSound();

        if (currentHitCount >= hitsToDefeat)
            StartCoroutine(ExecuteDefeat());
        else
            animator.SetTrigger("Hit");
    }

    IEnumerator ExecuteDefeat()
    {
        isDefeated = true;
        isAttacking = false;
        counterWindowOpen = false;

        animator.SetTrigger("Hit");
        Debug.Log("[NPCCombatController] NPC defeated!");

        // Wait for fall and get up animations to finish
        yield return new WaitForSeconds(6.0f);

        // Disable NPC after defeat sequence
        gameObject.SetActive(false);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = counterWindowOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
