using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Skeleton_AI : MonoBehaviour
{
    // Start is called before the first frame update
    public float Speed;
    public bool YouDied;
    public bool ChasePlayer;

    private NavMeshAgent _agent;
    [SerializeField]
    private Transform _target;
    [SerializeField]
    private Animator _anim;
    [SerializeField]
    private Transform _skeletonRig;

 

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Speed;
        _anim = transform.Find("Skeleton_01_Rigged").GetComponent<Animator>();
        _skeletonRig = transform.Find("Skeleton_01_Rigged").GetComponent<Transform>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (ChasePlayer == true)
        {
            _agent.SetDestination(_target.position);
        }

        if (_agent.velocity.magnitude > 0.2f)
        {
            _anim.SetBool("Walk", true);
        }
        
        if (_agent.velocity.magnitude < 0.2f)
        {
            _anim.SetBool("Walk", false);
        }

        if (_agent.remainingDistance < 1.5f)
        {
            _anim.SetBool("Attack", true);
            _agent.speed = 0;
        }

        if (_agent.remainingDistance >= 1.5f)
        {
            _anim.SetBool("Attack", false);
            _agent.speed = Speed; 
        }

        if (YouDied == true)
        {
            _anim.SetBool("Death", true);
            Speed = 0;
        }
    }
}
