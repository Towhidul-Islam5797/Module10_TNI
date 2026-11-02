using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Draws one box of the hotbar: the item icon, the stack count and a highlight
// when the slot is selected.
// It does not know anything about the Inventory. HotbarUI tells it what to show.
//
// While the crafting panel is open, the box also takes part in drag and drop:
// you can drag its item onto the crafting panel, and drop crafting items onto it.
// Unity calls our drag methods because the class "implements" these interfaces
// from UnityEngine.EventSystems (an interface is a promise to have certain methods):
//   IBeginDragHandler  OnBeginDrag  the mouse button went down here and the mouse started moving
//   IDragHandler       OnDrag       every frame while that drag goes on
//   IEndDragHandler    OnEndDrag    the mouse button was let go (wherever the mouse is now)
//   IDropHandler       OnDrop       something that was being dragged was let go ON this box
// They only work with an EventSystem in the scene, a GraphicRaycaster on the canvas,
// and Raycast Target switched on for this box's background image.
public class HotbarSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    // While an item is dragged out of this box, its icon here is drawn half transparent.
    private static readonly Color NormalIconColor = Color.white;
    private static readonly Color DraggedIconColor = new Color(1f, 1f, 1f, 0.5f);

    // The UI pieces that make up this slot. They are children of the slot in the Hierarchy.
    [Header("UI Parts")]
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    // The background color of a normal slot and of the selected slot.
    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 0.8f);

    // Which inventory slot this box stands for (0 is the first box), the crafting
    // scripts it passes drops on to, and the icon that follows the mouse.
    [Header("Drag and Drop")]
    [SerializeField] private int slotIndex;
    [SerializeField] private CraftingGrid craftingGrid;
    [SerializeField] private CraftingPanelUI craftingPanel;
    [SerializeField] private ItemDragUI dragIcon;

    // The inventory slot we drew last time, and whether its item is being dragged right now.
    private InventorySlot shownSlot;
    private bool isBeingDragged;

    // The crafting slots ask these when something from this box is dropped on them.
    public int SlotIndex
    {
        get { return slotIndex; }
    }

    public bool IsBeingDragged
    {
        get { return isBeingDragged; }
    }

    // Updates the slot so it matches the inventory slot it represents.
    public void Show(InventorySlot slot, bool isSelected)
    {
        shownSlot = slot;
        ShowItem(slot);
        ShowHighlight(isSelected);
    }

    // Shows the item's icon and count, or hides them when the slot is empty.
    private void ShowItem(InventorySlot slot)
    {
        if (slot.IsEmpty())
        {
            iconImage.enabled = false;
            countText.text = "";
            return;
        }

        iconImage.enabled = true;
        iconImage.sprite = slot.item.Icon;

        if (isBeingDragged)
        {
            iconImage.color = DraggedIconColor;
        }
        else
        {
            iconImage.color = NormalIconColor;
        }

        // A count of 1 is obvious from the icon, so we only show numbers of 2 or more.
        if (slot.count > 1)
        {
            countText.text = slot.count.ToString();
        }
        else
        {
            countText.text = "";
        }
    }

    // Changes the background color so the player can see which slot is selected.
    private void ShowHighlight(bool isSelected)
    {
        if (isSelected)
        {
            background.color = selectedColor;
        }
        else
        {
            background.color = normalColor;
        }
    }

    // ---------------------------------------------------------------
    // Drag and drop
    // ---------------------------------------------------------------

    // Dragging only makes sense while the crafting panel is open and this box holds an item.
    private bool CanDrag()
    {
        if (craftingPanel == null || !craftingPanel.IsOpen)
        {
            return false;
        }

        return shownSlot != null && !shownSlot.IsEmpty();
    }

    // The player starts dragging this box: show the item's icon under the mouse.
    // Nothing moves yet. The item only moves if it is dropped on a crafting box that accepts it.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag())
        {
            return;
        }

        isBeingDragged = true;
        dragIcon.BeginDrag(shownSlot.item.Icon, eventData.position);
    }

    // Every frame of the drag: the icon follows the mouse.
    public void OnDrag(PointerEventData eventData)
    {
        if (isBeingDragged)
        {
            dragIcon.Follow(eventData.position);
        }
    }

    // The mouse button was let go. Unity has already called OnDrop on the box below the
    // mouse (if there was one), so all that is left is to hide the dragged icon.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isBeingDragged)
        {
            return;
        }

        isBeingDragged = false;
        dragIcon.EndDrag(eventData);
    }

    // Something was dropped on this hotbar box.
    // eventData.pointerDrag is the object the drag started on. If it is a crafting box,
    // that item goes back into the inventory. Inventory.AddItem decides which slot it lands in,
    // so it stacks the normal way, whichever hotbar box you drop it on.
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        CraftingSlotUI craftingSource = eventData.pointerDrag.GetComponent<CraftingSlotUI>();
        if (craftingSource != null && craftingSource.IsBeingDragged)
        {
            craftingSource.MoveToInventory();
            return;
        }

        HotbarSlotUI hotbarSource = eventData.pointerDrag.GetComponent<HotbarSlotUI>();
        if (hotbarSource != null && hotbarSource.IsBeingDragged)
        {
            Debug.Log("That item is already in the hotbar. Drop it on the crafting panel instead.");
        }
    }
}
