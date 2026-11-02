using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Draws one box of the crafting panel: one of the 9 grid slots, or the result slot.
// It looks like a hotbar box: an icon, plus a count when there are 2 or more.
// A preview (an item that is not real yet) is drawn half transparent.
// It does not decide anything. CraftingPanelUI tells it what to show, and
// drops are passed on to CraftingGrid, which decides what really happens.
//
// Drag and drop uses the same four interfaces as HotbarSlotUI:
//   IBeginDragHandler  OnBeginDrag  a drag starts on this box
//   IDragHandler       OnDrag       every frame of that drag
//   IEndDragHandler    OnEndDrag    the mouse button was let go
//   IDropHandler       OnDrop       a dragged item was let go on this box
public class CraftingSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    // A preview, or an item being dragged out of this box, is drawn at half strength.
    private static readonly Color RealColor = Color.white;
    private static readonly Color FadedColor = new Color(1f, 1f, 1f, 0.5f);

    // The UI pieces that make up this slot. They are children of the slot in the Hierarchy.
    [Header("UI Parts")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    // Which crafting slot this box stands for.
    // Grid slots are numbered 0 to 8 (top-left to bottom-right). The result slot ticks Is Result Slot.
    [Header("Slot")]
    [SerializeField] private bool isResultSlot;
    [SerializeField] private int gridIndex;

    // The crafting logic (on the Player) and the icon that follows the mouse.
    [Header("Drag and Drop")]
    [SerializeField] private CraftingGrid craftingGrid;
    [SerializeField] private ItemDragUI dragIcon;

    // The crafting slot we drew last time, and whether its item is being dragged right now.
    private InventorySlot shownSlot;
    private bool isBeingDragged;

    // The hotbar asks this when something from this box is dropped on it.
    public bool IsBeingDragged
    {
        get { return isBeingDragged; }
    }

    // Updates the box so it matches the crafting slot it represents.
    public void Show(InventorySlot slot, bool isPreview)
    {
        shownSlot = slot;

        if (slot.IsEmpty())
        {
            iconImage.enabled = false;
            countText.text = "";
            return;
        }

        iconImage.enabled = true;
        iconImage.sprite = slot.item.Icon;

        // Half transparent for a preview or while dragging, fully visible for a real item.
        if (isPreview || isBeingDragged)
        {
            iconImage.color = FadedColor;
            countText.color = FadedColor;
        }
        else
        {
            iconImage.color = RealColor;
            countText.color = RealColor;
        }

        // Like the hotbar, a count of 1 is obvious from the icon.
        if (slot.count > 1)
        {
            countText.text = slot.count.ToString();
        }
        else
        {
            countText.text = "";
        }
    }

    // Called by HotbarSlotUI when this box's item is dropped on the hotbar.
    // CraftingGrid decides what that means: give a grid item back, finish crafting
    // (result preview), finish uncrafting (material preview) or cancel an uncraft.
    public void MoveToInventory()
    {
        if (isResultSlot)
        {
            craftingGrid.MoveResultToInventory();
        }
        else
        {
            craftingGrid.MoveGridToInventory(gridIndex);
        }
    }

    // ---------------------------------------------------------------
    // Drag and drop
    // ---------------------------------------------------------------

    // Start dragging: show the item's icon under the mouse. Previews can be dragged too,
    // because dragging a preview to the hotbar is how crafting and uncrafting are finished.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (shownSlot == null || shownSlot.IsEmpty())
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

    // The mouse button was let go. Any drop has already happened, so just hide the icon.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isBeingDragged)
        {
            return;
        }

        isBeingDragged = false;
        dragIcon.EndDrag(eventData);
    }

    // The panel was closed in the middle of a drag. A hidden object gets no more drag events,
    // so we stop the drag here and hide the icon ourselves.
    private void OnDisable()
    {
        if (isBeingDragged)
        {
            isBeingDragged = false;
            dragIcon.Hide();
        }
    }

    // Something was dropped on this crafting box. Only items from the hotbar are accepted.
    // - On a grid box: CraftingGrid moves one item in (it says no if the box is taken).
    // - On the result box: CraftingGrid starts an uncraft (it says no if the grid is not
    //   empty, or if no recipe makes that item).
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        CraftingSlotUI craftingSource = eventData.pointerDrag.GetComponent<CraftingSlotUI>();
        if (craftingSource != null && craftingSource.IsBeingDragged)
        {
            Debug.Log("Items can't move between crafting boxes. Drag it to the hotbar instead.");
            return;
        }

        HotbarSlotUI hotbarSource = eventData.pointerDrag.GetComponent<HotbarSlotUI>();
        if (hotbarSource == null || !hotbarSource.IsBeingDragged)
        {
            return;
        }

        if (isResultSlot)
        {
            craftingGrid.MoveToResult(hotbarSource.SlotIndex);
        }
        else
        {
            craftingGrid.MoveToGrid(hotbarSource.SlotIndex, gridIndex);
        }
    }
}
