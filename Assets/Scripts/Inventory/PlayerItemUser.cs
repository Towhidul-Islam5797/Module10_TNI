using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player choose a hotbar slot with the number keys 1-6
// and use the item in that slot with Q.
// Right now the only usable item is the Health Potion: it heals the player
// and one potion is removed from the stack.
[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(Health))]
public class PlayerItemUser : MonoBehaviour
{
    // How many number keys we listen to. It matches the 6 hotbar slots.
    private const int SlotKeyCount = 6;

    private Inventory inventory;
    private Health health;
    private InputAction[] slotActions;
    private InputAction useItemAction;

    // Find the scripts and input actions once, when the game starts.
    // The actions Slot1 to Slot6 and UseItem live in InputSystem_Actions.
    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        health = GetComponent<Health>();
        useItemAction = InputSystem.actions.FindAction("UseItem");

        slotActions = new InputAction[SlotKeyCount];
        for (int i = 0; i < SlotKeyCount; i++)
        {
            slotActions[i] = InputSystem.actions.FindAction("Slot" + (i + 1));
        }
    }

    private void Update()
    {
        HandleSlotSelection();
        HandleUseItem();
    }

    // If a number key was pressed this frame, select that slot.
    // Key 1 is slot index 0, key 2 is slot index 1, and so on.
    private void HandleSlotSelection()
    {
        for (int i = 0; i < slotActions.Length; i++)
        {
            if (slotActions[i].WasPressedThisFrame())
            {
                inventory.SelectSlot(i);
            }
        }
    }

    // If Q was pressed this frame, try to use the selected item.
    private void HandleUseItem()
    {
        if (useItemAction.WasPressedThisFrame())
        {
            UseSelectedItem();
        }
    }

    // Checks, one at a time, whether the selected item can be used.
    // If any check fails we log the reason and stop, so nothing is wasted.
    private void UseSelectedItem()
    {
        // A dead player cannot drink potions.
        if (health.IsDead)
        {
            return;
        }

        InventorySlot slot = inventory.GetSelectedSlot();
        if (slot.IsEmpty())
        {
            Debug.Log("The selected slot is empty.");
            return;
        }

        ItemData item = slot.item;
        if (!item.IsUsable())
        {
            Debug.Log("Can't use " + item.ItemName);
            return;
        }

        // Don't waste a potion when health is already full.
        if (health.CurrentHealth >= health.MaxHealth)
        {
            Debug.Log("Health is already full. " + item.ItemName + " was not used.");
            return;
        }

        health.Heal(item.HealAmount);
        inventory.RemoveItem(inventory.SelectedSlotIndex);
    }
}
