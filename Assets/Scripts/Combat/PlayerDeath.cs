using UnityEngine;

// This script watches the player's health.
// When health reaches zero, it plays the death animation
// and switches off every script that lets the player act.
[RequireComponent(typeof(Health))]
public class PlayerDeath : MonoBehaviour
{
    // The UpperBody layer is the second layer in the Animator (index 1).
    // We turn it off so a punch that was playing cannot freeze the arms.
    private const int UpperBodyLayer = 1;

    private Health health;
    private Animator animator;
    private ThirdPersonController movement;
    private PlayerCombat combat;
    private PlayerInteractor interactor;

    private void Awake()
    {
        // Find all the pieces we need once, when the game starts.
        health = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<ThirdPersonController>();
        combat = GetComponent<PlayerCombat>();
        interactor = GetComponent<PlayerInteractor>();
    }

    private void Update()
    {
        // Every frame we ask: is the player dead yet?
        if (health.IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        // Stop the player from moving, attacking, looking around and using objects.
        movement.enabled = false;
        combat.enabled = false;
        interactor.enabled = false;

        // Hide the punch layer and play the death animation.
        animator.SetLayerWeight(UpperBodyLayer, 0f);
        animator.SetTrigger("Die");

        // Turn this script off too, so Die() only ever runs once.
        enabled = false;
    }
}
