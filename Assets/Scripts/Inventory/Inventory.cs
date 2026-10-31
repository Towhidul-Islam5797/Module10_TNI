using System.Collections.Generic;
using UnityEngine;

// The Inventory holds everything the player is carrying.
// It is a list of slots. Each slot holds one kind of item and a count.
// Other scripts ask the Inventory to add or remove items; the Inventory
// decides which slot to use and writes what happened to the Console.
public class Inventory : MonoBehaviour
{
    // How many slots the inventory has. The hotbar shows one box per slot.
    // The list is filled in when the game starts. You can watch it in the
    // Inspector while playing.
    [Header("Slots")]
    [SerializeField] private int slotCount = 6;
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    // The slot the player has picked with the number keys (0 = the first slot).
    private int selectedSlotIndex = 0;

    // How many slots there are. The hotbar uses this.
    public int SlotCount
    {
        get { return slots.Count; }
    }

    // Which slot is selected right now.
    public int SelectedSlotIndex
    {
        get { return selectedSlotIndex; }
    }

    // Awake runs once when the game starts, before Start and Update.
    // We make the empty slots here so the inventory is ready before anyone uses it.
    private void Awake()
    {
        CreateEmptySlots();
    }

    // Fills the list with empty slots, one for each slotCount.
    private void CreateEmptySlots()
    {
        slots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            slots.Add(new InventorySlot());
        }
    }

    // Gives back the slot at a position in the list (0 is the first slot).
    public InventorySlot GetSlot(int index)
    {
        return slots[index];
    }

    // Gives back the slot the player has selected.
    public InventorySlot GetSelectedSlot()
    {
        return slots[selectedSlotIndex];
    }

    // Selects a slot. Numbers outside the list are ignored.
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            return;
        }

        selectedSlotIndex = index;
        Debug.Log("Selected slot " + (index + 1));
    }

    // Tries to put one item into the inventory.
    // Step 1: look for a slot that already has this item and still has room (stacking).
    // Step 2: if there is none, put the item into the first empty slot.
    // Returns true if the item was added, false if the inventory is full.
    public bool AddItem(ItemData item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.item == item && slot.count < item.MaxStackSize)
            {
                slot.count++;
                Debug.Log("Added " + item.ItemName + " to slot " + (i + 1) + ". Count: " + slot.count);
                return true;
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.IsEmpty())
            {
                slot.item = item;
                slot.count = 1;
                Debug.Log("Added " + item.ItemName + " to empty slot " + (i + 1));
                return true;
            }
        }

        Debug.Log("Inventory full. No room for " + item.ItemName);
        return false;
    }

    // Takes one item out of a slot.
    // When the count reaches zero, the slot is cleared so it can hold a new item.
    // Returns false if the slot was already empty.
    public bool RemoveItem(int slotIndex)
    {
        InventorySlot slot = slots[slotIndex];

        if (slot.IsEmpty())
        {
            Debug.Log("Slot " + (slotIndex + 1) + " is empty. Nothing to remove.");
            return false;
        }

        string itemName = slot.item.ItemName;
        slot.count--;

        if (slot.count <= 0)
        {
            slot.Clear();
        }

        Debug.Log("Removed one " + itemName + " from slot " + (slotIndex + 1) + ". Count: " + slot.count);
        return true;
    }
}
