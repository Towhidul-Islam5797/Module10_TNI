using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrol,
    Chase
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    private const float AnimationDampTime = 0.1f;
    private const float PointReachedDistance = 0.5f;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private float playerChestHeight = 1.2f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float chaseStopDistance = 1.5f;

    [Header("Vision")]
    [SerializeField] private float sightRange = 10f;
    [SerializeField] private float loseSightRange = 15f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float eyeHeight = 1.6f;

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyState currentState = EnemyState.Patrol;
    private int patrolIndex;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        GoToCurrentPatrolPoint();
    }

    private void Update()
    {
        UpdateState();

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Chase:
                Chase();
                break;
        }

        UpdateAnimation();
    }

    private void UpdateState()
    {
        if (currentState == EnemyState.Patrol && CanSeePlayer())
        {
            currentState = EnemyState.Chase;
        }
        else if (currentState == EnemyState.Chase && DistanceToPlayer() > loseSightRange)
        {
            currentState = EnemyState.Patrol;
            GoToCurrentPatrolPoint();
        }
    }

    private void Patrol()
    {
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0f;

        if (!agent.pathPending && agent.remainingDistance <= PointReachedDistance)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            GoToCurrentPatrolPoint();
        }
    }

    private void Chase()
    {
        agent.speed = chaseSpeed;
        agent.stoppingDistance = chaseStopDistance;
        agent.SetDestination(player.position);
    }

    private void GoToCurrentPatrolPoint()
    {
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    private bool CanSeePlayer()
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 playerChest = player.position + Vector3.up * playerChestHeight;
        Vector3 toPlayer = playerChest - eyePosition;

        if (toPlayer.magnitude > sightRange)
        {
            return false;
        }

        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f)
        {
            return false;
        }

        bool isBlocked = Physics.Linecast(eyePosition, playerChest);
        Debug.DrawLine(eyePosition, playerChest, isBlocked ? Color.red : Color.green);

        return !isBlocked;
    }

    private float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    private void UpdateAnimation()
    {
        animator.SetFloat(SpeedHash, agent.velocity.magnitude, AnimationDampTime, Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseSightRange);
    }
}
