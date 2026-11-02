using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The picture of an item that follows the mouse while the player drags it.
// It is the last child of HUDCanvas, so it is drawn on top of everything else.
//
// Its Image has Raycast Target switched off. If it were on, the icon itself would be
// the thing under the mouse, and the slot below it could never receive the drop.
//
// The slots tell it when to appear, where to go and when to disappear.
// It never changes the inventory or the crafting grid.
[RequireComponent(typeof(Image))]
public class ItemDragUI : MonoBehaviour
{
    private Image iconImage;

    // Find our Image and start hidden.
    private void Awake()
    {
        iconImage = GetComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.enabled = false;
    }

    // Shows the item's icon at the mouse position.
    public void BeginDrag(Sprite icon, Vector2 screenPosition)
    {
        iconImage.sprite = icon;
        iconImage.enabled = true;
        Follow(screenPosition);
    }

    // Moves the icon to the mouse. HUDCanvas is a "Screen Space - Overlay" canvas,
    // so a UI position is the same as a position on the screen in pixels.
    public void Follow(Vector2 screenPosition)
    {
        transform.position = screenPosition;
    }

    // Hides the icon when the mouse button is let go.
    // If the mouse was not over any slot, nothing happened to the item, so we tell the
    // player in the Console that the drag was cancelled.
    public void EndDrag(PointerEventData eventData)
    {
        Hide();

        if (!IsOverASlot(eventData))
        {
            Debug.Log("Drag cancelled. The item stays where it was.");
        }
    }

    // Hides the icon straight away, for example when the panel closes in the middle of a drag.
    public void Hide()
    {
        iconImage.enabled = false;
    }

    // True when the mouse was let go over a hotbar box or a crafting box.
    // pointerCurrentRaycast is what the GraphicRaycaster found under the mouse.
    private bool IsOverASlot(PointerEventData eventData)
    {
        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        if (target == null)
        {
            return false;
        }

        return target.GetComponent<HotbarSlotUI>() != null || target.GetComponent<CraftingSlotUI>() != null;
    }
}
