using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log($"{name} took {amount} damage. Health: {currentHealth}");
    }

    // Gives health back, for example when the player drinks a Health Potion.
    // Health can never go above maxHealth, and a dead character cannot be healed.
    public void Heal(int amount)
    {
        if (IsDead)
        {
            return;
        }

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log(name + " healed " + amount + ". Health: " + currentHealth);
    }
}
