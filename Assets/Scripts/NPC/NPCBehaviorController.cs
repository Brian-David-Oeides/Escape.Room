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
        Observing,    // 1-2 puzzles
        Approaching,  // 2-3 puzzles
        Agitated,     // 4-5 puzzles
        Hunting       // 6+ puzzles
    }

    public BehaviorState CurrentState => currentState;

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

    [Header("Behavior Settings")]
    [SerializeField] private float observingDistance = 10f;
    [SerializeField] private float approachDistance = 5f;
    [SerializeField] private float huntingSpeed = 5f;
    [SerializeField] private float groundOffset = 0f;
    [SerializeField] private float fallingGroundOffset = -0.3f;
    [SerializeField] private float gettingUpGroundOffset = -0.1f;
    [SerializeField] private float offsetSmoothTime = 0.5f;

    private float currentOffsetVelocity = 0f;
    private float smoothedOffset = 0f;

    [Header("Loitering Settings")]
    [SerializeField] private float loiterRadius = 5f;           // How far to wander
    [SerializeField] private float loiterWaitTime = 3f;         // Time to wait at each point
    [SerializeField] private float loiterMoveSpeed = 1.5f;      // Walking speed while loitering

    [HideInInspector] public bool combatInterrupted = false;

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
            Debug.LogError("Could not find XR Origin (XR Rig)!");
        }

        // Configure for root motion
        agent.updatePosition = false;
        agent.updateRotation = false;

        // Subscribe to puzzle events
        SubscribeToPuzzleEvents();

        // Initialize state
        UpdateBehaviorState();
    }

    void Update()
    {
        // Don't run behaviors if combat has interrupted movement
        if (!combatInterrupted)
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
        }

        // Update animator
        UpdateAnimator();

        // Handle rotation
        UpdateRotation();
    }

    void SubscribeToPuzzleEvents()
    {
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.OnPuzzleCompleted += OnPuzzleCompleted;
            Debug.Log("[NPCBehaviorController] Subscribed to PuzzleManager events");
        }
        else
        {
            Debug.LogWarning("[NPCBehaviorController] PuzzleManager not found!");
        }
    }

    void UpdateBehaviorState()
    {
        // Debug override
        if (forceHuntingMode)
        {
            currentState = BehaviorState.Hunting;
            Debug.Log($"NPC State FORCED to: {currentState}");
            return;
        }

        // Determine state based on puzzle count
        if (currentPuzzleCount == 0)
            currentState = BehaviorState.Dormant;
        else if (currentPuzzleCount <= 2)
            currentState = BehaviorState.Observing;
        else if (currentPuzzleCount <= 3)
            currentState = BehaviorState.Approaching;
        else if (currentPuzzleCount <= 5)
            currentState = BehaviorState.Agitated;
        else
            currentState = BehaviorState.Hunting;

        Debug.Log($"NPC State changed to: {currentState} (Puzzles: {currentPuzzleCount})");
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

    void OnAnimatorMove()
    {
        Vector3 position = transform.position;
        position += agent.desiredVelocity * Time.deltaTime;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas))
        {
            float targetOffset = groundOffset;
            if (currentCombatPhase == CombatPhase.Falling)
                targetOffset = fallingGroundOffset;
            else if (currentCombatPhase == CombatPhase.GettingUp)
                targetOffset = gettingUpGroundOffset;
            // Smoothly transition to target offset instead of snapping
            smoothedOffset = Mathf.SmoothDamp(smoothedOffset, targetOffset, ref currentOffsetVelocity, offsetSmoothTime);
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