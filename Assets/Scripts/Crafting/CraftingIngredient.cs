// One line of a recipe, for example "4 Coin".
// It remembers which item is needed and how many of it.
// [System.Serializable] lets Unity show it in the Inspector, inside the recipe asset.
// Like InventorySlot, it is a small plain C# class with two public fields:
// it only holds data, the CraftingGrid does the work.
[System.Serializable]
public class CraftingIngredient
{
    // The item the recipe needs.
    public ItemData item;

    // How many of that item must be in the crafting grid.
    public int count = 1;
}
