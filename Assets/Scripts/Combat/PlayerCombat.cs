using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    private const float ChestHeight = 1.2f;
    private const float DebugRayDuration = 1f;
    private const string KickTag = "Kick";
    private const string PunchTag = "Punch";
    private const int BaseLayer = 0;
    private const int UpperBodyLayer = 1;
    private const float UpperBodyBlendSpeed = 10f;
    private static readonly int PunchHash = Animator.StringToHash("Punch");
    private static readonly int KickHash = Animator.StringToHash("Kick");

    [Header("Punch")]
    [SerializeField] private int punchDamage = 10;
    [SerializeField] private float punchCooldown = 0.4f;

    [Header("Kick")]
    [SerializeField] private int kickDamage = 20;
    [SerializeField] private float kickCooldown = 0.8f;

    [Header("Raycast")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask enemyLayers;

    private Animator animator;
    private InputAction punchAction;
    private InputAction kickAction;
    private float nextAttackTime;

    public bool IsKicking => IsInTaggedState(BaseLayer, KickTag);

    private bool IsPunching => IsInTaggedState(UpperBodyLayer, PunchTag);

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        punchAction = InputSystem.actions.FindAction("Attack");
        kickAction = InputSystem.actions.FindAction("Kick");
    }

    private void Update()
    {
        HandleUpperBodyWeight();
        HandleAttackInput();
    }

    private void HandleUpperBodyWeight()
    {
        float targetWeight = IsPunching ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(UpperBodyLayer);
        float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, UpperBodyBlendSpeed * Time.deltaTime);

        animator.SetLayerWeight(UpperBodyLayer, newWeight);
    }

    private void HandleAttackInput()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (punchAction.WasPressedThisFrame())
        {
            Attack(PunchHash, punchDamage, punchCooldown);
        }
        else if (kickAction.WasPressedThisFrame())
        {
            Attack(KickHash, kickDamage, kickCooldown);
        }
    }

    private void Attack(int triggerHash, int damage, float cooldown)
    {
        nextAttackTime = Time.time + cooldown;
        animator.SetTrigger(triggerHash);
        DealDamage(damage);
    }

    private bool IsInTaggedState(int layer, string tag)
    {
        return animator.GetCurrentAnimatorStateInfo(layer).IsTag(tag) ||
               animator.GetNextAnimatorStateInfo(layer).IsTag(tag);
    }

    private void DealDamage(int damage)
    {
        Vector3 origin = transform.position + Vector3.up * ChestHeight;
        bool hasHit = Physics.Raycast(origin, transform.forward, out RaycastHit hit, attackRange, enemyLayers);

        Debug.DrawRay(origin, transform.forward * attackRange, hasHit ? Color.green : Color.red, DebugRayDuration);

        if (hasHit && hit.collider.TryGetComponent(out Health enemyHealth))
        {
            enemyHealth.TakeDamage(damage);
        }
    }
}
