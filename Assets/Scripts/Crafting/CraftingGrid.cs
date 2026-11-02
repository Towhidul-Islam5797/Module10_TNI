using System.Collections.Generic;
using UnityEngine;

// The brain of the crafting system. It sits on the Player, next to the Inventory.
// It holds the 9 grid slots and the result slot, knows every recipe, and decides
// what happens when the player moves an item in or out.
//
// It has no UI code at all. The crafting panel only draws what this script holds
// and passes the player's drops on to it. That way we can test crafting
// without pressing Play, and we could build a completely different UI later.
//
// Two ideas to remember:
// - A PREVIEW is an item that is shown but not real yet (drawn half transparent).
// - Nothing is crafted or uncrafted until the player drags the preview to the hotbar.
[RequireComponent(typeof(Inventory))]
public class CraftingGrid : MonoBehaviour
{
    // A 3x3 grid has 9 slots.
    private const int GridSize = 9;

    // Every recipe this crafting grid knows about.
    [Header("Recipes")]
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    // The 9 grid slots and the result slot. They are made when the game starts.
    // You can watch them in the Inspector while playing. Each grid slot holds one item.
    [Header("Slots (filled in while playing)")]
    [SerializeField] private List<InventorySlot> gridSlots = new List<InventorySlot>();
    [SerializeField] private InventorySlot resultSlot = new InventorySlot();

    // isResultPreview: the grid matches a recipe and the result slot shows what you
    //                  WOULD get (crafting, not finished yet).
    // isGridPreview:   a crafted item sits on the result slot and the grid shows what you
    //                  WOULD get back (uncrafting, not finished yet).
    // currentRecipe:   the recipe the preview belongs to.
    private bool isResultPreview;
    private bool isGridPreview;
    private CraftingRecipe currentRecipe;

    private Inventory inventory;

    // How many grid slots there are (9).
    public int GridSlotCount
    {
        get { return gridSlots.Count; }
    }

    // The result slot, so the UI can draw it.
    public InventorySlot ResultSlot
    {
        get { return resultSlot; }
    }

    // True while the result slot only shows a preview (crafting is not finished).
    public bool IsResultPreview
    {
        get { return isResultPreview; }
    }

    // True while the grid only shows previews (uncrafting is not finished).
    public bool IsGridPreview
    {
        get { return isGridPreview; }
    }

    // Find the Inventory and make the empty slots when the game starts.
    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        CreateEmptyGrid();
    }

    // Fills the grid with 9 empty slots and empties the result slot.
    private void CreateEmptyGrid()
    {
        gridSlots.Clear();

        for (int i = 0; i < GridSize; i++)
        {
            gridSlots.Add(new InventorySlot());
        }

        resultSlot.Clear();
        isResultPreview = false;
        isGridPreview = false;
        currentRecipe = null;
    }

    // Gives back one grid slot (0 is the top-left slot), so the UI can draw it.
    public InventorySlot GetGridSlot(int index)
    {
        return gridSlots[index];
    }

    // ---------------------------------------------------------------
    // Moving items (the UI calls these when the player drops something)
    // ---------------------------------------------------------------

    // Hotbar -> grid slot.
    // Only ONE item moves, even if the hotbar slot holds a whole stack.
    // Returns true if the item moved.
    public bool MoveToGrid(int inventorySlotIndex, int gridIndex)
    {
        if (isGridPreview)
        {
            Debug.Log("Finish or cancel the uncraft before adding items to the grid.");
            return false;
        }

        InventorySlot gridSlot = gridSlots[gridIndex];
        if (!gridSlot.IsEmpty())
        {
            Debug.Log("Crafting slot " + (gridIndex + 1) + " already holds " + gridSlot.item.ItemName + ". Drop rejected.");
            return false;
        }

        InventorySlot inventorySlot = inventory.GetSlot(inventorySlotIndex);
        if (inventorySlot.IsEmpty())
        {
            return false;
        }

        // Remember the item first: RemoveItem clears the slot when the last one leaves.
        ItemData item = inventorySlot.item;
        inventory.RemoveItem(inventorySlotIndex);
        gridSlot.item = item;
        gridSlot.count = 1;
        Debug.Log("Moved " + item.ItemName + " from hotbar slot " + (inventorySlotIndex + 1) + " to crafting slot " + (gridIndex + 1));

        UpdateCraftPreview();
        return true;
    }

    // Grid slot -> hotbar.
    // Normally this just gives the item back.
    // While uncrafting, taking the first material is what finishes the uncraft.
    public bool MoveGridToInventory(int gridIndex)
    {
        InventorySlot gridSlot = gridSlots[gridIndex];
        if (gridSlot.IsEmpty())
        {
            return false;
        }

        if (isGridPreview)
        {
            return CompleteUncraft(gridIndex);
        }

        ItemData item = gridSlot.item;
        if (!inventory.AddItem(item))
        {
            Debug.Log("Inventory full. " + item.ItemName + " stays in crafting slot " + (gridIndex + 1));
            return false;
        }

        gridSlot.Clear();
        Debug.Log("Moved " + item.ItemName + " from crafting slot " + (gridIndex + 1) + " back to the inventory");

        UpdateCraftPreview();
        return true;
    }

    // Hotbar -> result slot. This STARTS an uncraft.
    // It only works with an empty grid and an item that some recipe makes.
    public bool MoveToResult(int inventorySlotIndex)
    {
        InventorySlot inventorySlot = inventory.GetSlot(inventorySlotIndex);
        if (inventorySlot.IsEmpty())
        {
            return false;
        }

        ItemData item = inventorySlot.item;

        if (!IsGridEmpty())
        {
            Debug.Log("Can't uncraft " + item.ItemName + ": empty the crafting grid first");
            return false;
        }

        CraftingRecipe recipe = FindRecipeThatMakes(item);
        if (recipe == null)
        {
            Debug.Log("Can't uncraft " + item.ItemName + ": no recipe makes it");
            return false;
        }

        if (recipe.GetTotalIngredientCount() > gridSlots.Count)
        {
            Debug.Log("Can't uncraft " + item.ItemName + ": its recipe has more ingredients than the grid has slots");
            return false;
        }

        // The crafted item leaves the hotbar and waits on the result slot (it is real).
        inventory.RemoveItem(inventorySlotIndex);
        resultSlot.item = item;
        resultSlot.count = 1;

        // The grid shows what the player would get back (not real yet).
        ShowIngredientPreviews(recipe);
        currentRecipe = recipe;
        isGridPreview = true;
        isResultPreview = false;

        Debug.Log("Uncrafting " + item.ItemName + ": drag a material to the hotbar to take it apart, or drag "
            + item.ItemName + " back to cancel");
        return true;
    }

    // Result slot -> hotbar.
    // If the result is a preview, this FINISHES crafting.
    // If the result is a real item waiting to be uncrafted, this CANCELS the uncraft.
    public bool MoveResultToInventory()
    {
        if (resultSlot.IsEmpty())
        {
            return false;
        }

        if (isResultPreview)
        {
            return CompleteCraft();
        }

        return CancelUncraft();
    }

    // Called when the crafting panel closes.
    // Real items go back to the inventory and previews are simply cleared.
    // Items that do not fit stay in the grid until the panel is opened again.
    // Returns true if everything went back.
    public bool ReturnEverythingToInventory()
    {
        bool allReturned = true;

        if (isGridPreview)
        {
            // An uncraft was started but not finished: give the crafted item back.
            allReturned = CancelUncraft();
        }
        else
        {
            ClearResultPreview();

            for (int i = 0; i < gridSlots.Count; i++)
            {
                InventorySlot gridSlot = gridSlots[i];
                if (gridSlot.IsEmpty())
                {
                    continue;
                }

                if (inventory.AddItem(gridSlot.item))
                {
                    gridSlot.Clear();
                }
                else
                {
                    allReturned = false;
                }
            }

            // The leftovers might still match a recipe, so check again.
            UpdateCraftPreview();
        }

        if (!allReturned)
        {
            Debug.Log("Inventory full, some items stay in the crafting grid");
        }

        return allReturned;
    }

    // ---------------------------------------------------------------
    // Crafting (materials -> item)
    // ---------------------------------------------------------------

    // Looks at the grid after every change and shows or hides the result preview.
    private void UpdateCraftPreview()
    {
        // While uncrafting, the result slot holds a real item. Leave it alone.
        if (isGridPreview)
        {
            return;
        }

        CraftingRecipe recipe = FindMatchingRecipe();

        // Nothing changed since last time, so there is nothing to do or to log.
        if (recipe == currentRecipe)
        {
            return;
        }

        if (recipe == null)
        {
            Debug.Log("The grid no longer matches " + currentRecipe.Result.ItemName + ". Preview removed.");
            ClearResultPreview();
            return;
        }

        currentRecipe = recipe;
        resultSlot.item = recipe.Result;
        resultSlot.count = recipe.ResultCount;
        isResultPreview = true;
        Debug.Log("Recipe found: " + recipe.Result.ItemName + ". Drag it to the hotbar to craft it.");
    }

    // Goes through every recipe and gives back the first one the grid matches,
    // or null when none matches.
    public CraftingRecipe FindMatchingRecipe()
    {
        if (IsGridEmpty())
        {
            return null;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            if (GridMatchesRecipe(recipes[i]))
            {
                return recipes[i];
            }
        }

        return null;
    }

    // A recipe matches when the grid holds EXACTLY its ingredients:
    // 1. the total number of items is the same (so there are no extra items), and
    // 2. every ingredient appears the right number of times.
    // Where the items sit in the grid does not matter.
    private bool GridMatchesRecipe(CraftingRecipe recipe)
    {
        if (CountAllItemsInGrid() != recipe.GetTotalIngredientCount())
        {
            return false;
        }

        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            CraftingIngredient ingredient = recipe.Ingredients[i];

            if (CountInGrid(ingredient.item) != ingredient.count)
            {
                return false;
            }
        }

        return true;
    }

    // Finishes crafting: the result goes into the inventory and the materials are used up.
    // If the inventory has no room, nothing changes at all.
    private bool CompleteCraft()
    {
        ItemData result = currentRecipe.Result;

        if (!inventory.HasRoomFor(result, currentRecipe.ResultCount))
        {
            Debug.Log("Inventory full. Can't take " + result.ItemName + ", the materials stay in the grid.");
            return false;
        }

        for (int i = 0; i < currentRecipe.ResultCount; i++)
        {
            inventory.AddItem(result);
        }

        Debug.Log("Crafted " + result.ItemName + " from " + currentRecipe.DescribeIngredients());

        ClearGrid();
        ClearResultPreview();
        return true;
    }

    // Empties the result slot when it only showed a preview.
    private void ClearResultPreview()
    {
        if (isResultPreview)
        {
            resultSlot.Clear();
        }

        isResultPreview = false;
        currentRecipe = null;
    }

    // ---------------------------------------------------------------
    // Uncrafting (item -> materials)
    // ---------------------------------------------------------------

    // Gives back the recipe whose result is this item, or null if no recipe makes it.
    private CraftingRecipe FindRecipeThatMakes(ItemData item)
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i].Result == item)
            {
                return recipes[i];
            }
        }

        return null;
    }

    // Puts the recipe's ingredients into the grid, one item per slot, from the top-left.
    // "2 Health Potion + 1 Gem" fills slot 1 and 2 with a potion and slot 3 with a gem.
    private void ShowIngredientPreviews(CraftingRecipe recipe)
    {
        int gridIndex = 0;

        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            CraftingIngredient ingredient = recipe.Ingredients[i];

            for (int n = 0; n < ingredient.count; n++)
            {
                gridSlots[gridIndex].item = ingredient.item;
                gridSlots[gridIndex].count = 1;
                gridIndex++;
            }
        }
    }

    // Finishes uncrafting when the player takes the first material.
    // That material goes into the inventory, the crafted item is used up,
    // and the other previews become real items the player can take one by one.
    private bool CompleteUncraft(int gridIndex)
    {
        InventorySlot gridSlot = gridSlots[gridIndex];
        ItemData material = gridSlot.item;

        if (!inventory.AddItem(material))
        {
            Debug.Log("Inventory full. Can't uncraft " + resultSlot.item.ItemName + " yet.");
            return false;
        }

        Debug.Log("Uncrafted " + resultSlot.item.ItemName + " into " + currentRecipe.DescribeIngredients()
            + ". Drag the other materials to the hotbar.");

        gridSlot.Clear();
        resultSlot.Clear();
        isGridPreview = false;
        currentRecipe = null;

        // The leftover materials are real now. Check whether they happen to match a recipe.
        UpdateCraftPreview();
        return true;
    }

    // Stops an uncraft: the crafted item goes back to the inventory and the previews disappear.
    private bool CancelUncraft()
    {
        ItemData item = resultSlot.item;

        if (!inventory.AddItem(item))
        {
            Debug.Log("Inventory full. " + item.ItemName + " stays on the result slot.");
            return false;
        }

        resultSlot.Clear();
        ClearGrid();
        isGridPreview = false;
        currentRecipe = null;
        Debug.Log("Uncraft cancelled. " + item.ItemName + " returned to the inventory.");
        return true;
    }

    // ---------------------------------------------------------------
    // Small helpers for the grid
    // ---------------------------------------------------------------

    // True when no grid slot holds anything (real or preview).
    public bool IsGridEmpty()
    {
        return CountAllItemsInGrid() == 0;
    }

    // How many grid slots hold this item.
    private int CountInGrid(ItemData item)
    {
        int count = 0;

        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (!gridSlots[i].IsEmpty() && gridSlots[i].item == item)
            {
                count++;
            }
        }

        return count;
    }

    // How many grid slots hold anything at all.
    private int CountAllItemsInGrid()
    {
        int count = 0;

        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (!gridSlots[i].IsEmpty())
            {
                count++;
            }
        }

        return count;
    }

    // Empties all 9 grid slots.
    private void ClearGrid()
    {
        for (int i = 0; i < gridSlots.Count; i++)
        {
            gridSlots[i].Clear();
        }
    }
}
