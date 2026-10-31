using System.Collections.Generic;
using UnityEngine;

// Shows the player's Inventory as a row of boxes at the bottom of the screen.
//
// How it stays up to date: every frame, Update copies each inventory slot into
// its matching HotbarSlotUI. This is the same idea as our HealthBar script.
// It is the simplest way to keep the screen correct: it does not matter which
// script changed the inventory (a pickup, using a potion, selecting a slot),
// the hotbar always shows the latest state one frame later.
// Six small slots are very cheap to update, so doing it every frame is fine here.
public class HotbarUI : MonoBehaviour
{
    // The inventory we want to show (on the Player).
    [Header("Inventory")]
    [SerializeField] private Inventory inventory;

    // One HotbarSlotUI per inventory slot, in order from left to right.
    [Header("Slots")]
    [SerializeField] private List<HotbarSlotUI> slotViews = new List<HotbarSlotUI>();

    private void Update()
    {
        RefreshSlots();
    }

    // Copies every inventory slot into the matching box on screen.
    private void RefreshSlots()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            // If there are more boxes than inventory slots, the extra boxes are left alone.
            if (i >= inventory.SlotCount)
            {
                return;
            }

            bool isSelected = i == inventory.SelectedSlotIndex;
            slotViews[i].Show(inventory.GetSlot(i), isSelected);
        }
    }
}
