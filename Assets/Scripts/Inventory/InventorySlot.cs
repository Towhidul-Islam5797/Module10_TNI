// One box in the inventory. It remembers which item is inside and how many.
// [System.Serializable] lets Unity show the slot in the Inspector, so you can
// watch the inventory fill up while the game is running.
// This is a plain C# class, not a MonoBehaviour, so it is not put on a GameObject.
// Its two fields are public to keep it as simple as possible: it is only a
// small container of data that the Inventory script fills in.
[System.Serializable]
public class InventorySlot
{
    // The kind of item in this slot. null means the slot is empty.
    public ItemData item;

    // How many of that item are in the slot.
    public int count;

    // A slot is empty when it has no item or the count has dropped to zero.
    public bool IsEmpty()
    {
        return item == null || count <= 0;
    }

    // Makes the slot empty again.
    public void Clear()
    {
        item = null;
        count = 0;
    }
}
