using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Draws one box of the hotbar: the item icon, the stack count and a highlight
// when the slot is selected.
// It does not know anything about the Inventory. HotbarUI tells it what to show.
public class HotbarSlotUI : MonoBehaviour
{
    // The UI pieces that make up this slot. They are children of the slot in the Hierarchy.
    [Header("UI Parts")]
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    // The background color of a normal slot and of the selected slot.
    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 0.8f);

    // Updates the slot so it matches the inventory slot it represents.
    public void Show(InventorySlot slot, bool isSelected)
    {
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
}
