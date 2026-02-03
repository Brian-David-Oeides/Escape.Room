using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshTest : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Configure for root motion
        agent.updatePosition = false;
        agent.updateRotation = false;

        Debug.Log("NavMeshAgent configured for root motion");
        Debug.Log("Is on NavMesh: " + agent.isOnNavMesh);
    }

    void Update()
    {
        // Press SPACE to set a random destination
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NavMeshHit hit;
            Vector3 randomDirection = Random.insideUnitSphere * 10f;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                Debug.Log("Destination set to: " + hit.position);
            }
        }

        // Calculate speed from NavMeshAgent's desired velocity
        float speed = agent.desiredVelocity.magnitude;

        // Set animator speed parameter
        animator.SetFloat("Speed", speed);

        
        if (speed > 0.1f)
        {
            Debug.Log("Speed being sent to animator: " + speed);
        }

        // Rotate toward movement direction
        if (agent.desiredVelocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.desiredVelocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    void OnAnimatorMove()
    {
        // Use NavMeshAgent velocity to move, not animator root position
        // This works with "In Place" animations
        Vector3 position = transform.position;
        position += agent.desiredVelocity * Time.deltaTime;
        transform.position = position;
        agent.nextPosition = position;
    }
}