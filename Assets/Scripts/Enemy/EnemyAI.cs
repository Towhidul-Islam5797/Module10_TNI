using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrol,
    Chase,
    Attack,
    Dead
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    private const float AnimationDampTime = 0.1f;
    private const float PointReachedDistance = 0.5f;
    private const float LeaveAttackRangeMultiplier = 1.2f;
    private const float FaceTurnSpeed = 10f;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int[] AttackHashes =
    {
        Animator.StringToHash("Boxing"),
        Animator.StringToHash("Punching"),
        Animator.StringToHash("Kicking")
    };

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

    [Header("Attack")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private float hitDelay = 0.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float hitAngle = 60f;

    [Header("Death")]
    [SerializeField] private GameObject healthBar;
    [SerializeField] private float destroyDelay = 5f;

    private NavMeshAgent agent;
    private Animator animator;
    private Health health;
    private Health playerHealth;
    private EnemyState currentState = EnemyState.Patrol;
    private int patrolIndex;
    private int attackIndex;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
        playerHealth = player.GetComponent<Health>();
    }

    private void Start()
    {
        GoToCurrentPatrolPoint();
    }

    private void Update()
    {
        if (currentState == EnemyState.Dead)
        {
            return;
        }

        if (health.IsDead)
        {
            Die();
            return;
        }

        UpdateState();

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Chase:
                Chase();
                break;
            case EnemyState.Attack:
                Attack();
                break;
        }

        UpdateAnimation();
    }

    private void UpdateState()
    {
        if (currentState != EnemyState.Patrol && playerHealth.IsDead)
        {
            currentState = EnemyState.Patrol;
            GoToCurrentPatrolPoint();
        }
        else if (currentState == EnemyState.Patrol && !playerHealth.IsDead && CanSeePlayer())
        {
            currentState = EnemyState.Chase;
        }
        else if (currentState == EnemyState.Chase && DistanceToPlayer() <= attackRange)
        {
            currentState = EnemyState.Attack;
        }
        else if (currentState == EnemyState.Chase && DistanceToPlayer() > loseSightRange)
        {
            currentState = EnemyState.Patrol;
            GoToCurrentPatrolPoint();
        }
        else if (currentState == EnemyState.Attack && DistanceToPlayer() > attackRange * LeaveAttackRangeMultiplier)
        {
            currentState = EnemyState.Chase;
        }
    }

    private void Patrol()
    {
        agent.isStopped = false;
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
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = chaseStopDistance;
        agent.SetDestination(player.position);
    }

    private void Attack()
    {
        agent.isStopped = true;
        FacePlayer();

        if (Time.time >= nextAttackTime)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        animator.SetTrigger(AttackHashes[attackIndex]);
        attackIndex = (attackIndex + 1) % AttackHashes.Length;
        Invoke(nameof(TryHitPlayer), hitDelay);
    }

    private void TryHitPlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        bool isInRange = toPlayer.magnitude <= attackRange;
        bool isInFront = Vector3.Angle(transform.forward, toPlayer) <= hitAngle;

        if (isInRange && isInFront)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, FaceTurnSpeed * Time.deltaTime);
    }

    private void Die()
    {
        currentState = EnemyState.Dead;
        CancelInvoke(nameof(TryHitPlayer));

        agent.enabled = false;
        GetComponent<Collider>().enabled = false;
        healthBar.SetActive(false);

        animator.SetTrigger(DieHash);
        Destroy(gameObject, destroyDelay);
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
