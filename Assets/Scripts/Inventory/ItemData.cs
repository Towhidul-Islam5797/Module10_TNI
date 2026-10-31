using UnityEngine;

// ItemData describes one kind of item, for example "Coin" or "Health Potion".
// It is a ScriptableObject, which means it is saved as an asset file in the
// Project window instead of living on a GameObject in the scene.
// Every coin in the game points to the same Coin asset, so if we change the
// icon once, every coin changes.
// Right click in the Project window > Create > Inventory > Item Data to make a new one.
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    // What the item is called and what it looks like in the hotbar.
    [Header("Display")]
    [SerializeField] private string itemName = "New Item";
    [SerializeField] private Sprite icon;

    // How many of this item fit in one inventory slot.
    // 1 means the item does not stack (every one needs its own slot).
    [Header("Stacking")]
    [SerializeField] private int maxStackSize = 1;

    // How much health the item gives back when used.
    // 0 means the item cannot be used.
    [Header("Use")]
    [SerializeField] private int healAmount = 0;

    // Other scripts can read these values, but they cannot change them.
    // Only the asset in the Inspector decides what an item is.
    public string ItemName
    {
        get { return itemName; }
    }

    public Sprite Icon
    {
        get { return icon; }
    }

    public int MaxStackSize
    {
        get { return maxStackSize; }
    }

    public int HealAmount
    {
        get { return healAmount; }
    }

    // An item can be used only if it actually does something.
    public bool IsUsable()
    {
        return healAmount > 0;
    }
}
