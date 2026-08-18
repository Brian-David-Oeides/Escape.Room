using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCBehaviorController : MonoBehaviour
{
    // Behavior states
    public enum BehaviorState
    {
        Dormant,      // 0 puzzles
        Observing,    // 1-5 puzzles
        Approaching,  // 6-11 puzzles
        Agitated,     // 12-20 puzzles
        Hunting       // 21+ puzzles
    }

    public BehaviorState CurrentState => currentState;

    public event System.Action<BehaviorState, BehaviorState> OnStateChanged;
    public event System.Action OnPuzzleSolvedSameState;

    public enum CombatPhase
    {
        None,
        Falling,
        GettingUp
    }

    [HideInInspector] public CombatPhase currentCombatPhase = CombatPhase.None;

    [Header("Current State")]
    [SerializeField] private BehaviorState currentState = BehaviorState.Dormant;
    [SerializeField] private bool forceHuntingMode = false;

    [Header("References")]
    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;
    private NPCVoiceLineController npcVoiceLineController;

    [Header("Behavior Settings")]
    [SerializeField] private float observingDistance = 10f;
    [SerializeField] private float approachDistance = 5f;
    [SerializeField] private float huntingSpeed = 5f;
    [SerializeField] private float groundOffset = 0f;
    [SerializeField] private float fallingGroundOffset = -0.3f;
    [SerializeField] private float gettingUpGroundOffset = -0.1f;
    [SerializeField] private float fallTransitionDuration = 0.3f;
    [SerializeField] private float fallDelay = 0.70f;
    [SerializeField] private float getUpTransitionDuration = 0.4f;

    private float phaseStartTime = 0f;
    private float phaseStartOffset = 0f;
    private CombatPhase lastPhase = CombatPhase.None;
    private float smoothedOffset = 0f;

    [Header("Loitering Settings")]
    [SerializeField] private float loiterRadius = 5f;           // How far to wander
    [SerializeField] private float loiterWaitTime = 3f;         // Time to wait at each point
    [SerializeField] private float loiterMoveSpeed = 1.5f;      // Walking speed while loitering

    [Header("Talking")]
    [SerializeField] private float talkingDuration = 3.8f; // matches Talking clip length (113 frames @ 30fps ~= 3.767s) + small buffer

    [HideInInspector] public bool combatInterrupted = false;
    [HideInInspector] public bool isPermanentlyDefeated = false;
    public void SetDefeated(bool defeated) { isPermanentlyDefeated = defeated; }

    private bool isTalking = false;

    private Vector3 loiterTarget;
    private float loiterWaitTimer = 0f;
    private bool isWaitingAtLoiterPoint = false;

    private int currentPuzzleCount = 0;

    void Start()
    {
        // Get components
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Find player (XR Rig)
        GameObject xrRig = GameObject.Find("XR Origin (XR Rig)");
        if (xrRig != null)
        {
            playerTransform = xrRig.transform;
        }
        else
        {
            GameLog.LogError("Could not find XR Origin (XR Rig)!");
        }

        // Configure for root motion
        agent.updatePosition = false;
        agent.updateRotation = false;

        // Subscribe to puzzle events
        SubscribeToPuzzleEvents();

        // Subscribe to voice line events
        npcVoiceLineController = GetComponent<NPCVoiceLineController>();
        if (npcVoiceLineController != null)
        {
            npcVoiceLineController.OnLineStarted += HandleLineStarted;
        }

        // Initialize state
        UpdateBehaviorState();
    }

    void Update()
    {
        // Don't run behaviors if combat has interrupted movement
        if (!combatInterrupted && !isPermanentlyDefeated && !isTalking)
        {
            // Execute current state behavior
            switch (currentState)
            {
                case BehaviorState.Dormant:
                    DormantBehavior();
                    break;
                case BehaviorState.Observing:
                    ObservingBehavior();
                    break;
                case BehaviorState.Approaching:
                    ApproachingBehavior();
                    break;
                case BehaviorState.Agitated:
                    AgitatedBehavior();
                    break;
                case BehaviorState.Hunting:
                    HuntingBehavior();
                    break;
            }

            // Handle rotation
            UpdateRotation();
        }
        else if (isTalking)
        {
            FacePlayer();
        }

        // Update animator
        UpdateAnimator();
    }

    void SubscribeToPuzzleEvents()
    {
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.OnPuzzleCompleted += OnPuzzleCompleted;
            GameLog.Log("[NPCBehaviorController] Subscribed to PuzzleManager events");
        }
        else
        {
            GameLog.LogWarning("[NPCBehaviorController] PuzzleManager not found!");
        }
    }

    void UpdateBehaviorState()
    {
        BehaviorState previousState = currentState;

        // Debug override
        if (forceHuntingMode)
        {
            currentState = BehaviorState.Hunting;
            GameLog.Log($"NPC State FORCED to: {currentState}");
            if (previousState != currentState)
                OnStateChanged?.Invoke(previousState, currentState);
            return;
        }

        // Determine state based on puzzle count
        if (currentPuzzleCount == 0)
            currentState = BehaviorState.Dormant;
        else if (currentPuzzleCount <= 5)
            currentState = BehaviorState.Observing;
        else if (currentPuzzleCount <= 11)
            currentState = BehaviorState.Approaching;
        else if (currentPuzzleCount <= 20)
            currentState = BehaviorState.Agitated;
        else
            currentState = BehaviorState.Hunting;

        GameLog.Log($"NPC State changed to: {currentState} (Puzzles: {currentPuzzleCount})");

        if (previousState != currentState)
            OnStateChanged?.Invoke(previousState, currentState);
        else
            OnPuzzleSolvedSameState?.Invoke();
    }

    void LoiterBehavior(Vector3 centerPoint, float radius)
    {
        // If waiting at a point
        if (isWaitingAtLoiterPoint)
        {
            agent.isStopped = true;
            loiterWaitTimer -= Time.deltaTime;

            if (loiterWaitTimer <= 0f)
            {
                isWaitingAtLoiterPoint = false;
                SetNewLoiterDestination(centerPoint, radius);
            }
        }
        else
        {
            // Moving to loiter point
            agent.isStopped = false;
            agent.speed = loiterMoveSpeed;

            // Check if reached destination
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isWaitingAtLoiterPoint = true;
                loiterWaitTimer = loiterWaitTime;
            }
        }
    }

    void SetNewLoiterDestination(Vector3 centerPoint, float radius)
    {
        // Find random point on NavMesh within radius
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += centerPoint;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // State behaviors (placeholders for now)
    void DormantBehavior()
    {
        // Minimal loitering around starting position
        LoiterBehavior(transform.position, loiterRadius * 0.5f); // Smaller radius for dormant
    }

    void ObservingBehavior()
    {
        // Loiter while maintaining distance from player
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // If too close, move away
            if (distanceToPlayer < observingDistance)
            {
                agent.speed = loiterMoveSpeed;

                Vector3 awayFromPlayer = transform.position - playerTransform.position;
                Vector3 targetPos = transform.position + awayFromPlayer.normalized * 3f;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }
            }
            else
            {
                // Loiter at safe distance
                LoiterBehavior(transform.position, loiterRadius);
            }
        }
    }

    void ApproachingBehavior()
    {
        // Move toward player for dialogue
        if (playerTransform != null)
        {
            agent.speed = loiterMoveSpeed;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > approachDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
            }
            else
            {
                agent.isStopped = true;
                // TODO: Trigger talk animation
            }
        }
    }

    void AgitatedBehavior()
    {
        // Aggressive pacing - loiter near player
        if (playerTransform != null)
        {
            LoiterBehavior(playerTransform.position, loiterRadius * 1.5f); // Larger radius, more erratic
        }
    }

    void HuntingBehavior()
    {
        // Chase player
        if (playerTransform != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = 1.5f;
            agent.SetDestination(playerTransform.position);
            agent.speed = huntingSpeed;
        }
    }

    void UpdateAnimator()
    {
        float speed = agent.desiredVelocity.magnitude;
        animator.SetFloat("Speed", speed);
    }

    void UpdateRotation()
    {
        if (agent.desiredVelocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.desiredVelocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
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

    void HandleLineStarted()
    {
        if (isPermanentlyDefeated || combatInterrupted) return;

        isTalking = true;
        if (agent != null) agent.isStopped = true;
        animator.SetTrigger("Talk");
        StartCoroutine(EndTalkingAfterDuration());
    }

    IEnumerator EndTalkingAfterDuration()
    {
        yield return new WaitForSeconds(talkingDuration);
        isTalking = false;
        if (agent != null && !combatInterrupted && !isPermanentlyDefeated)
            agent.isStopped = false;
    }

    void OnAnimatorMove()
    {
        Vector3 position = transform.position;

        bool inCombatPhase = currentCombatPhase == CombatPhase.Falling
                          || currentCombatPhase == CombatPhase.GettingUp;

        if (inCombatPhase)
        {
            // Apply animation's horizontal root motion so the stumble/getup
            // moves the actual GameObject. Requires loopBlendPositionXZ: 0
            // in both Shoulder Hit And Fall and Getting Up import settings.
            Vector3 delta = animator.deltaPosition;
            position.x += delta.x;
            position.z += delta.z;
        }
        else
        {
            position += agent.desiredVelocity * Time.deltaTime;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas))
        {
            if (currentCombatPhase != lastPhase)
            {
                phaseStartTime = Time.time;
                phaseStartOffset = smoothedOffset;
                lastPhase = currentCombatPhase;
            }

            float targetOffset = groundOffset;
            float transitionDuration = 0.3f;
            float elapsed = Time.time - phaseStartTime;

            if (currentCombatPhase == CombatPhase.Falling)
            {
                targetOffset = fallingGroundOffset;
                transitionDuration = fallTransitionDuration;
                elapsed = Mathf.Max(0f, elapsed - fallDelay);
            }
            else if (currentCombatPhase == CombatPhase.GettingUp)
            {
                targetOffset = groundOffset;
                transitionDuration = getUpTransitionDuration;
            }

            float t = Mathf.Clamp01(elapsed / transitionDuration);
            smoothedOffset = Mathf.Lerp(phaseStartOffset, targetOffset, t);

            position.y = hit.position.y + smoothedOffset;
        }

        transform.position = position;
        agent.nextPosition = position;
    }

    public float GetGroundOffset()
    {
        return groundOffset;
    }

    // Public method to update puzzle count (will be called by PuzzleManager)
    public void OnPuzzleCompleted(int totalCompleted)
    {
        currentPuzzleCount = totalCompleted;
        UpdateBehaviorState();
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.OnPuzzleCompleted -= OnPuzzleCompleted;
        }

        if (npcVoiceLineController != null)
        {
            npcVoiceLineController.OnLineStarted -= HandleLineStarted;
        }
    }

    void OnDrawGizmos()
    {
        // Debug: Draw line to player if detected
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, playerTransform.position);

            // Draw observing distance sphere
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, observingDistance);

            // Draw approach distance sphere
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, approachDistance);
        }
    }
}