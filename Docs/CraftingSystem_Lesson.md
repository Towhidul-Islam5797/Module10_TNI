# Crafting System - Lesson Guide

A teacher's guide for Module 10, Milestone 4. Students add crafting to the **ThirdPerson** scene,
step by step, by hand. It follows straight on from **Inventory Basics**.

Result: the player presses **C** (or clicks **Craft (C)**) to open a crafting panel with a 3x3 grid,
a two-way arrow and a result slot. They drag items from the hotbar into the grid. When the grid
matches a recipe, a half-transparent **preview** of the result appears. Dragging the preview to the
hotbar crafts it for real. It also works backwards: dropping a crafted item on the result slot shows
its materials as previews, and taking one material takes the item apart.

---

## Lesson at a glance

| Part | Topic | New script(s) | Time |
|---|---|---|---|
| 1 | Recipes and the crafting grid (logic only) | `CraftingIngredient`, `CraftingRecipe`, `CraftingGrid`, `Inventory.HasRoomFor` | 75 min |
| 2 | Opening the crafting panel | `CraftingPanelUI`, `CraftingSlotUI` (starter) | 60 min |
| 3 | Drag and drop, crafting through the UI | `ItemDragUI`, `HotbarSlotUI` (extended), `CraftingSlotUI` (full) | 75 min |
| 4 | Uncrafting and closing the panel | (no new script) | 30 min |

**Students should already know:** everything from Inventory Basics (`ItemData`, `InventorySlot`,
`Inventory`, `HotbarUI`, `HotbarSlotUI`, `PlayerItemUser`), ScriptableObjects, Canvas and
TextMeshPro, reading input with `InputSystem.actions.FindAction`, and how `PlayerDeath` switches
scripts off with `enabled = false`.

**By the end, students can explain:**
- why a **recipe is data** (a ScriptableObject) and not code
- how a **shapeless recipe** is matched: the right number of each ingredient, and nothing else
- the difference between a **preview** and a **real** item, and why crafting only happens when the
  result is taken
- how **uncrafting** reads the same recipe backwards
- why UI needs an **EventSystem** and a **GraphicRaycaster**
- how Unity's **drag-and-drop interfaces** work, and in which order they are called
- how a menu "pauses" the player: **free the cursor** and **switch scripts off**
- why the crafting **logic** and the crafting **UI** are separate scripts

**Reference files in the project:** the finished scripts are in `Assets/Scripts/Crafting` and
`Assets/Scripts/UI`. The sprint docs, exercises and solutions are in `Docs/` (Sprint 10 to Sprint 13).

---

## Before class (teacher)

1. Start from the project at the commit **after** Milestone 3 (Inventory Basics), or make a Git
   branch there for students.
2. Copy the icons `Assets/Items/Icons/GoldBar.png`, `BigHealthPotion.png`, `GemRing.png` and
   `Assets/Crafting/Icons/TwoWayArrow.png` from the finished project. Students import these; they do
   not draw icons.
3. Play the finished project for the class for 5 minutes: craft a Gold Bar, then a Gem Ring, then
   take the ring apart again.

**Catch-up tools** (finished project only, menu **Tools**):
- **Setup Crafting Scene** builds the items, recipes, extra pickups, EventSystem, Craft button,
  crafting panel and drag icon, and wires the hotbar. Run **Setup Inventory Scene** first if the
  inventory is missing. Safe to run twice. Note: running **Setup Inventory Scene** again rebuilds
  the hotbar and pickups, so run **Setup Crafting Scene** after it.
- **Validate Crafting** runs the 50 Inventory checks plus 86 crafting checks without Play mode and
  prints `[PASS]` / `[FAIL]` to the Console.

---

# Part 1 - Recipes and the crafting grid

## The big idea

Ask the class: *"A Gold Bar is made from 4 coins. Where should '4 coins make a Gold Bar' be
written down?"*

- In the code, with an `if`? Then every new recipe means new code.
- **In a file**, like an item. That is a **recipe asset** (a ScriptableObject again).

> **Analogy:** a recipe is a **card in a cookbook**. "4 Coin -> 1 Gold Bar". The cook (our
> `CraftingGrid`) reads every card and checks if what is on the kitchen counter matches one.

The 3x3 grid is the **kitchen counter**. Our recipes are **shapeless**: it does not matter where
on the counter the ingredients lie, only which ones and how many.

> **Analogy for the preview:** a **shop window**. You can see the Gold Bar, but it is not yours
> until you take it. Taking it is what pays (uses up the coins).

## Step 1.1 - CraftingIngredient (one line of a recipe)

Create the folder `Assets/Scripts/Crafting`, then `CraftingIngredient.cs`:

```csharp
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
```

**Explain:** this is the same idea as `InventorySlot`: a tiny box of data (item + count).

## Step 1.2 - CraftingRecipe (the cookbook card)

`Assets/Scripts/Crafting/CraftingRecipe.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

// A recipe says: "these ingredients make this result".
// Like ItemData, it is a ScriptableObject, so every recipe is an asset file.
// Adding a new recipe to the game means making a new asset, not writing new code.
// Our recipes are "shapeless": only WHICH items and HOW MANY count,
// not where they sit in the 3x3 grid.
// Right click in the Project window > Create > Crafting > Recipe to make a new one.
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    // What has to be in the crafting grid, for example "4 Coin",
    // or "2 Health Potion" and "1 Gem".
    [Header("Ingredients")]
    [SerializeField] private List<CraftingIngredient> ingredients = new List<CraftingIngredient>();

    // What the player gets, and how many.
    [Header("Result")]
    [SerializeField] private ItemData result;
    [SerializeField] private int resultCount = 1;

    // Other scripts can read the recipe, but only the asset in the Inspector can change it.
    public List<CraftingIngredient> Ingredients
    {
        get { return ingredients; }
    }

    public ItemData Result
    {
        get { return result; }
    }

    public int ResultCount
    {
        get { return resultCount; }
    }

    // Adds up all the ingredient counts. "2 Health Potion + 1 Gem" gives 3.
    // The crafting grid uses this to check there are no extra items in the grid.
    public int GetTotalIngredientCount()
    {
        int total = 0;

        for (int i = 0; i < ingredients.Count; i++)
        {
            total += ingredients[i].count;
        }

        return total;
    }

    // Writes the ingredients as text for the Console, for example "2 Health Potion + 1 Gem".
    public string DescribeIngredients()
    {
        string text = "";

        for (int i = 0; i < ingredients.Count; i++)
        {
            if (i > 0)
            {
                text += " + ";
            }

            text += ingredients[i].count + " " + ingredients[i].item.ItemName;
        }

        return text;
    }
}
```

**Explain:**
- `GetTotalIngredientCount` adds up the counts. We need it to spot **extra** items in the grid.
- `DescribeIngredients` only builds text for the Console: "2 Health Potion + 1 Gem".

## Step 1.3 - New items and the three recipes

1. Import the three item icons into `Assets/Items/Icons`: **Texture Type = Sprite (2D and UI)**, Apply.
2. In `Assets/Items`, **Create > Inventory > Item Data** three times:

| Asset | Item Name | Icon | Max Stack Size | Heal Amount |
|---|---|---|---|---|
| `GoldBar` | Gold Bar | GoldBar | 10 | 0 |
| `BigHealthPotion` | Big Health Potion | BigHealthPotion | 3 | 60 |
| `GemRing` | Gem Ring | GemRing | 1 | 0 |

3. Create the folder `Assets/Crafting/Recipes`. **Create > Crafting > Recipe** three times:

| Asset | Ingredients | Result | Result Count |
|---|---|---|---|
| `GoldBar` | Coin x 4 | GoldBar | 1 |
| `BigHealthPotion` | HealthPotion x 2, Gem x 1 | BigHealthPotion | 1 |
| `GemRing` | GoldBar x 1, Gem x 1 | GemRing | 1 |

Point out that **GemRing needs a Gold Bar**: Coins -> Gold Bar -> Gem Ring is a two-step recipe.

4. More materials to play with: duplicate coins in `ItemPickups` until there are **9 coins**, and
   gems until there are **3 gems**. Spread them out a little.

## Step 1.4 - Inventory.HasRoomFor

Crafting must never lose the materials. Before we take the result, we ask the Inventory
*"is there room?"* without changing anything. Add this method to `Inventory.cs`, under `AddItem`:

```csharp
    // Checks, without changing anything, whether this many of an item would fit.
    // It adds up the free space: the room left in stacks of the same item,
    // plus a whole stack for every empty slot.
    // Crafting uses this so that a full inventory never loses the materials.
    public bool HasRoomFor(ItemData item, int amount)
    {
        int room = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.IsEmpty())
            {
                room += item.MaxStackSize;
            }
            else if (slot.item == item)
            {
                room += item.MaxStackSize - slot.count;
            }
        }

        return room >= amount;
    }
```

## Step 1.5 - CraftingGrid (the cook)

`Assets/Scripts/Crafting/CraftingGrid.cs`. It is long, so type it in three sittings:
**fields and moving**, then **crafting**, then **uncrafting and helpers**.

```csharp
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
```

### How matching works (draw this on the board)

```
Recipe GoldBar: 4 Coin                   total = 4

Grid A: [C][ ][C]   items = 4, coins = 4      -> MATCH (positions don't matter)
        [ ][C][ ]
        [ ][ ][C]

Grid B: [C][C][C]   items = 3                 -> no (3 is not 4)

Grid C: [C][C][G]   items = 5                 -> no (5 is not 4: the gem is extra)
        [C][C][ ]
```

Two questions, always in this order:
1. Is the **number of items** in the grid the recipe's total? (catches extra items)
2. Does **each ingredient** appear the right number of times?

### The two "preview" flags (draw this too)

```
                       grid           result slot
nothing going on       real / empty   empty
craft preview          real items     PREVIEW (isResultPreview)   take it -> CompleteCraft
uncraft preview        PREVIEWS       real item (isGridPreview)   take a material -> CompleteUncraft
                                                                  take the item back -> CancelUncraft
```

**Explain:**
- The script has **no UI code**. That is why the teacher tool can test all of it without Play mode.
- `MoveResultToInventory` does two different jobs depending on the flag: finish crafting, or
  cancel uncrafting. The UI does not need to know which.
- Every move returns `true` or `false` and logs a sentence, so students can follow along in the Console.

### Checkpoint 1

Add `CraftingGrid` to the `Player`, and drag the three recipe assets into **Recipes**.
Press Play, select the Player: the Inspector shows 9 empty **Grid Slots** and a **Result Slot**.
(There is no panel yet to put items in. In the finished project, **Tools > Validate Crafting** tests
all of this logic.)

---

# Part 2 - Opening the crafting panel

## The big idea

A menu needs the mouse. But during play the mouse **turns the camera** and the left button
**punches**. So opening the panel switches the game into **menu mode**:

| | Playing | Panel open |
|---|---|---|
| Cursor | locked, hidden | free, visible |
| `ThirdPersonController`, `PlayerCombat`, `PlayerInteractor`, `PlayerItemUser` | on | **off** (`enabled = false`) |

> **Analogy:** sitting down at a workbench. Your hands are busy with the crafting, so you stop
> walking and fighting. Stand up again (close the panel) and you can move.

This is the same trick `PlayerDeath` uses: switching a script off stops its `Update`.

UI clicks also need two helpers that the scene did not have yet:
- **EventSystem** with an **InputSystemUIInputModule**: turns mouse input into UI events.
- **GraphicRaycaster** on the Canvas: finds which UI image is under the mouse.

## Step 2.1 - EventSystem and GraphicRaycaster

1. **GameObject > UI > Event System**. Select it. If it shows a **Standalone Input Module**, click
   **Replace with InputSystemUIInputModule**.
2. On the module, set **Actions Asset** to `InputSystem_Actions`. Point, Click and the others fill in
   from its **UI** map.
3. Select `HUDCanvas`, **Add Component > Graphic Raycaster**.

## Step 2.2 - The C key

Open `InputSystem_Actions`. In the **Player** map add `ToggleCrafting`, **Button**, binding
**C**. **Save Asset**.

## Step 2.3 - CraftingSlotUI (starter version)

The panel needs a box that can draw an item. Start with only the drawing part
(Part 3 replaces this file with the full version):

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Draws one box of the crafting panel: one of the 9 grid slots, or the result slot.
// It looks like a hotbar box: an icon, plus a count when there are 2 or more.
// A preview (an item that is not real yet) is drawn half transparent.
// It does not decide anything. CraftingPanelUI tells it what to show.
public class CraftingSlotUI : MonoBehaviour
{
    // A preview is drawn at half strength so the player can see it is not real yet.
    private static readonly Color RealColor = Color.white;
    private static readonly Color PreviewColor = new Color(1f, 1f, 1f, 0.5f);

    // The UI pieces that make up this slot. They are children of the slot in the Hierarchy.
    [Header("UI Parts")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    // Which crafting slot this box stands for.
    // Grid slots are numbered 0 to 8 (top-left to bottom-right). The result slot ticks Is Result Slot.
    [Header("Slot")]
    [SerializeField] private bool isResultSlot;
    [SerializeField] private int gridIndex;

    // Updates the box so it matches the crafting slot it represents.
    public void Show(InventorySlot slot, bool isPreview)
    {
        if (slot.IsEmpty())
        {
            iconImage.enabled = false;
            countText.text = "";
            return;
        }

        iconImage.enabled = true;
        iconImage.sprite = slot.item.Icon;

        // Half transparent for a preview, fully visible for a real item.
        if (isPreview)
        {
            iconImage.color = PreviewColor;
            countText.color = PreviewColor;
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
}
```

**Explain:** the same idea as `HotbarSlotUI`, plus one new thing: a **preview** is drawn with
alpha 0.5.

## Step 2.4 - CraftingPanelUI

`Assets/Scripts/UI/CraftingPanelUI.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Opens and closes the crafting panel, with the C key or the "Craft (C)" button.
//
// While the panel is open the player is in "menu mode":
// - the mouse cursor is free and visible, so the player can drag items, and
// - the Player scripts that read the mouse and keyboard are switched off,
//   so clicking does not punch and moving the mouse does not turn the camera.
// Closing the panel switches everything back on (unless the player has died).
//
// While open, it also copies the CraftingGrid into the 9 + 1 boxes every frame,
// the same idea as HotbarUI.
//
// This script sits on an object that is always active. The panel itself is a
// child that we show and hide. (If this script were on the hidden panel,
// its Update would stop and C could never open the panel again.)
public class CraftingPanelUI : MonoBehaviour
{
    // The panel we show and hide (a child of this object).
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    // The Player. We find its scripts in Awake, the same way PlayerDeath does.
    [Header("Player")]
    [SerializeField] private GameObject player;

    // The 9 grid boxes (in order, top-left to bottom-right) and the result box.
    [Header("Slots")]
    [SerializeField] private List<CraftingSlotUI> gridSlotViews = new List<CraftingSlotUI>();
    [SerializeField] private CraftingSlotUI resultSlotView;

    private CraftingGrid craftingGrid;
    private Health health;
    private Animator animator;
    private ThirdPersonController movement;
    private PlayerCombat combat;
    private PlayerInteractor interactor;
    private PlayerItemUser itemUser;
    private InputAction toggleAction;
    private bool isOpen;

    // Other scripts (the hotbar) ask this before they allow dragging.
    public bool IsOpen
    {
        get { return isOpen; }
    }

    // Find the Player's scripts and the ToggleCrafting action (key C) once, when the game starts.
    private void Awake()
    {
        craftingGrid = player.GetComponent<CraftingGrid>();
        health = player.GetComponent<Health>();
        animator = player.GetComponentInChildren<Animator>();
        movement = player.GetComponent<ThirdPersonController>();
        combat = player.GetComponent<PlayerCombat>();
        interactor = player.GetComponent<PlayerInteractor>();
        itemUser = player.GetComponent<PlayerItemUser>();
        toggleAction = InputSystem.actions.FindAction("ToggleCrafting");

        panel.SetActive(false);
    }

    private void Update()
    {
        HandleToggleInput();
        CloseIfPlayerDied();
        RefreshSlots();
    }

    // Pressing C opens the panel when it is closed, and closes it when it is open.
    private void HandleToggleInput()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            TogglePanel();
        }
    }

    // The Craft button calls this too (Button > On Click in the Inspector).
    public void TogglePanel()
    {
        if (isOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    // Shows the panel, frees the cursor and pauses the player's controls.
    private void OpenPanel()
    {
        if (health.IsDead)
        {
            Debug.Log("Can't craft while dead.");
            return;
        }

        isOpen = true;
        panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPlayerControls(false);

        // The movement script is off now, so nobody would reset the run animation.
        // We set Speed to 0 ourselves so the player stands still instead of running on the spot.
        animator.SetFloat("Speed", 0f);

        Debug.Log("Crafting panel opened");
    }

    // Gives the items in the grid back, hides the panel and locks the cursor again.
    // The player's controls only come back if the player is still alive.
    private void ClosePanel()
    {
        craftingGrid.ReturnEverythingToInventory();

        isOpen = false;
        panel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!health.IsDead)
        {
            SetPlayerControls(true);
        }

        Debug.Log("Crafting panel closed");
    }

    // If the enemy kills the player while the panel is open, close it.
    private void CloseIfPlayerDied()
    {
        if (isOpen && health.IsDead)
        {
            ClosePanel();
        }
    }

    // Switches the scripts that read the player's input off (false) or on (true).
    // Switching a script off with "enabled" stops its Update, the same trick PlayerDeath uses.
    private void SetPlayerControls(bool isEnabled)
    {
        movement.enabled = isEnabled;
        combat.enabled = isEnabled;
        interactor.enabled = isEnabled;
        itemUser.enabled = isEnabled;
    }

    // Copies every crafting slot into its box on screen (only while the panel is open).
    private void RefreshSlots()
    {
        if (!isOpen)
        {
            return;
        }

        for (int i = 0; i < gridSlotViews.Count; i++)
        {
            gridSlotViews[i].Show(craftingGrid.GetGridSlot(i), craftingGrid.IsGridPreview);
        }

        resultSlotView.Show(craftingGrid.ResultSlot, craftingGrid.IsResultPreview);
    }
}
```

**Explain:**
- Why is this script **not** on the panel? Because the panel is hidden with `SetActive(false)`.
  A hidden object does not run `Update`, so C could never open it again.
- `animator.SetFloat("Speed", 0f)`: the movement script is off, so nobody would stop the run animation.
- `ClosePanel` gives the items back (Part 4), and only switches the controls back on when the
  player is alive.
- `TogglePanel` is **public** so the Button can call it.

## Step 2.5 - Build the panel in the Editor

Target hierarchy:

```
HUDCanvas                Graphic Raycaster
  Crafting               CraftingPanelUI (stretch to fill, no image)
    CraftButton          Button, "Craft (C)"
    CraftingPanel        Image (starts hidden)
      Title              TMP "Crafting"
      Grid               Grid Layout Group
        GridSlot_1..9    Image, CraftingSlotUI
          Icon           Image
          Count          TMP
      Arrow              Image (TwoWayArrow)
      ResultLabel        TMP "Result"
      ResultSlot         Image, CraftingSlotUI (Is Result Slot ticked)
        Icon, Count
      Hint               TMP help text
```

1. **Right-click HUDCanvas > Create Empty**, name it `Crafting`. Anchor preset **stretch-stretch**,
   all offsets `0`.
2. **Right-click Crafting > UI > Button - TextMeshPro**, name it `CraftButton`. Anchor **bottom-right**
   (Shift+Alt), Pos X `-30`, Pos Y `30`, Width `180`, Height `60`. Text `Craft (C)`, size `28`, bold.
   **Navigation = None** (WASD is also the UI "Navigate" action).
3. **Right-click Crafting > UI > Image**, name it `CraftingPanel`, Width `580`, Height `380`,
   UISprite, Sliced, dark color with alpha about 230.
4. Inside it:
   - `Title` (TMP "Crafting", size 32, bold): anchor top-stretch, Height `60`.
   - `Grid` (Create Empty): Width `260`, Height `260`, Pos X `-125`. **Add Component > Grid Layout
     Group**: Cell Size `80 x 80`, Spacing `10 x 10`, Constraint **Fixed Column Count** `3`.
   - `GridSlot_1`: a UI Image like a hotbar box (UISprite, Sliced, black alpha 128,
     **Raycast Target ON** - items will be dropped on it). Children `Icon` (Image, stretch, padding
     `12`, Preserve Aspect, Raycast Target off) and `Count` (TMP, size 24, bold, bottom-right, Raycast
     Target off). **Add Component > CraftingSlotUI**, drag Icon and Count in, Grid Index `0`.
   - Duplicate it to `GridSlot_9`, and set **Grid Index** `1` ... `8`.
   - `Arrow`: Image, sprite `TwoWayArrow`, `80 x 80`, Pos X `85`, Raycast Target off.
   - `ResultSlot`: like a grid slot, but `90 x 90`, Pos X `210`, a gold tint, **Is Result Slot** ticked.
   - `ResultLabel` (TMP "Result") above it, `Hint` (TMP, size 18) along the bottom.
5. On `Crafting`, **Add Component > CraftingPanelUI**: drag `CraftingPanel` into **Panel**, `Player`
   into **Player**, `GridSlot_1..9` into **Grid Slot Views** (in order), `ResultSlot` into **Result Slot View**.
6. On `CraftButton`, **On Click > +**, drag `Crafting` in, choose **CraftingPanelUI > TogglePanel**.
7. Untick `CraftingPanel` (hidden at start). Save the scene.

### Checkpoint 2

Press Play. Press **C**: the panel opens and the cursor appears. WASD, mouse look, punch, E, 1-6 and Q
do nothing. Click **Craft (C)**: the panel closes and everything works again. Open the panel and let
the enemy kill you: it closes by itself.

---

# Part 3 - Drag and drop

## The big idea

Unity has ready-made **interfaces** for drag and drop. An interface is a promise: "this class has
these methods". If a UI script promises them, the EventSystem calls them:

| Interface | Method | When |
|---|---|---|
| `IBeginDragHandler` | `OnBeginDrag` | the mouse goes down on this box and starts moving |
| `IDragHandler` | `OnDrag` | every frame while dragging |
| `IDropHandler` | `OnDrop` | the mouse is let go **on this box** |
| `IEndDragHandler` | `OnEndDrag` | the mouse is let go (sent to the box the drag **started** on) |

Order when you let go: **`OnDrop` on the target first, then `OnEndDrag` on the source.**

> **Analogy:** moving a card on a table. You pick it up (begin), move your hand (drag), put it down on
> a spot (drop), and your hand is empty again (end). If the spot is not allowed, the card simply
> goes back.

Two rules make our version simple:
1. **Nothing changes while dragging.** Only `OnDrop` changes data, by asking `CraftingGrid`.
   A cancelled drag has nothing to undo.
2. **Where did it come from?** `eventData.pointerDrag` is the GameObject the drag started on.
   `GetComponent<HotbarSlotUI>()` or `GetComponent<CraftingSlotUI>()` tells us which kind.

## Step 3.1 - ItemDragUI (the icon under the mouse)

`Assets/Scripts/UI/ItemDragUI.cs`:

```csharp
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
```

**Explain:** **Raycast Target must be off**. If the icon could be hit, it would always be the thing
under the mouse, and the slot below could never receive the drop.

## Step 3.2 - HotbarSlotUI (now draggable)

Replace `Assets/Scripts/UI/HotbarSlotUI.cs` with:

```csharp
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
```

**Explain:**
- `Show` remembers the slot it drew (`shownSlot`), so the box still does not need the Inventory.
- `CanDrag`: only while the panel is open, and only if there is an item.
- `OnDrop` from a crafting box calls `craftingSource.MoveToInventory()`. Which hotbar box you drop
  on does not matter: `Inventory.AddItem` uses the normal stacking rules.

## Step 3.3 - CraftingSlotUI (full version)

Replace the starter `CraftingSlotUI.cs` with:

```csharp
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
```

**Explain:**
- **Previews can be dragged.** Dragging the result preview to the hotbar is how crafting finishes.
- `OnDisable`: if the panel closes in the middle of a drag, the hidden box gets no `OnEndDrag`,
  so it hides the drag icon itself.
- The `isResultSlot` branch in `OnDrop` starts an uncraft. Part 4 explains it.

## Step 3.4 - Wire it up in the Editor

1. **Right-click HUDCanvas > UI > Image**, name it `DragIcon`, `64 x 64`, **Raycast Target off**,
   Preserve Aspect, and untick the **Image** component. **Add Component > ItemDragUI**.
   Drag it to the **bottom** of HUDCanvas's children (drawn last = on top).
2. Hotbar: on each `Slot_1..6` **tick Raycast Target** on its background Image. In **HotbarSlotUI >
   Drag and Drop**: **Slot Index** `0`...`5`, **Crafting Grid** = Player, **Crafting Panel** = Crafting,
   **Drag Icon** = DragIcon.
3. Each `GridSlot_1..9` and `ResultSlot`: **Crafting Grid** = Player, **Drag Icon** = DragIcon.
   (Select all ten and set them at once.)

### Checkpoint 3

Pick up 5 coins. Press **C**. Drag coins into 4 grid boxes, one at a time: the hotbar count drops each
time. After the 4th, a faded Gold Bar appears on the result slot. Drag one coin back: the preview
disappears. Put it back, then drag the Gold Bar to the hotbar: *Crafted Gold Bar from 4 Coin*.

---

# Part 4 - Uncrafting and closing the panel

## The big idea

Uncrafting is **the same recipe, read backwards**.

```
Crafting:   grid (real)  ->  result (PREVIEW)    take the result      -> crafted
Uncrafting: result (real) -> grid (PREVIEWS)     take one material    -> uncrafted
```

> **Analogy:** a shop's **returns desk**. You put the Gold Bar on the desk. The clerk lays out the 4
> coins you would get back, but you only get them when you pick one up. Change your mind? Take the
> Gold Bar back.

There is no new script: it is already in `CraftingGrid` (Part 1) and in the `isResultSlot` branch of
`CraftingSlotUI.OnDrop` (Part 3). Walk through these methods together:

| Method | What it does |
|---|---|
| `MoveToResult` | grid must be empty, a recipe must make the item; item leaves the hotbar, `ShowIngredientPreviews` fills the grid |
| `CompleteUncraft` | the first material goes to the inventory, the crafted item is used up, the other previews become real |
| `CancelUncraft` | the crafted item goes back, previews are cleared |
| `ReturnEverythingToInventory` | called by `ClosePanel`: real items go back, previews are cleared, leftovers stay if the inventory is full |

**Why is uncrafting only finished when a material is taken?** So the player can change their mind,
and so nothing is lost when the inventory is full: if the first material does not fit, nothing
changes.

### Checkpoint 4 (final test)

1. Craft a Gold Bar. Drop it on the **result** slot: 4 faded coins appear in the grid.
2. Drag the Gold Bar back to the hotbar: the previews disappear (cancelled).
3. Drop it on the result slot again, then drag **one coin** to the hotbar: the Gold Bar is gone, the
   other 3 coins become solid. Drag them back one by one.
4. Drop a **Coin** on the result slot: *Can't uncraft Coin: no recipe makes it*.
5. Put a coin in the grid, then drop a Gold Bar on the result slot: *empty the crafting grid first*.
6. Craft a Gold Bar again, pick up a gem, put both in the grid: Gem Ring preview. Take it.
7. 2 potions + 1 gem: Big Health Potion. Get hurt, close the panel, select it, press **Q**: heals 60.
8. Put 2 coins in the grid and press **C**: they go back to the hotbar.

---

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| Nothing can be clicked or dragged | No **EventSystem**, or no **GraphicRaycaster** on HUDCanvas | Add both (Step 2.1) |
| Console: "You are trying to read Input using the UnityEngine.Input class" | The EventSystem has a **Standalone Input Module** | Replace it with **InputSystemUIInputModule** |
| C does nothing | Action not named exactly `ToggleCrafting`, asset not saved, or `CraftingPanelUI` is on the hidden panel | Check the name, **Save Asset**, put the script on `Crafting` |
| Panel opens but the player still turns or punches | A script was not switched off in `SetPlayerControls` | Check all four lines |
| Player keeps running on the spot | `Speed` was not set to 0 | `animator.SetFloat("Speed", 0f)` in `OpenPanel` |
| Hotbar items can't be dragged | Hotbar background **Raycast Target** is off, or **Crafting Panel** is empty on the slot | Tick it / assign it |
| Dropping never works, the icon "eats" the drop | DragIcon **Raycast Target** is on | Untick it |
| The drag icon is hidden behind the panel | DragIcon is not the last child of HUDCanvas | Drag it to the bottom of the list |
| Everything goes into crafting slot 1 | All grid slots have **Grid Index** 0 | Number them 0-8 |
| 4 coins show no preview | Recipe not in the Player's **Recipes** list, or different Coin assets | Add the recipe; all coins use the one Coin asset |
| 4 coins + a gem show a Gold Bar | The "total count" check is missing in `GridMatchesRecipe` | Compare `CountAllItemsInGrid()` with `GetTotalIngredientCount()` first |

---

## Quick quiz (end of class)

1. Why is a recipe a ScriptableObject and not an `if` in the code?
2. The grid holds 2 potions, 1 gem and 1 coin. Does it match BigHealthPotion? Which check says no?
3. A Gold Bar preview is on the result slot. Is there a Gold Bar in the inventory? When does that change?
4. Why does `CompleteCraft` call `HasRoomFor` before `AddItem`?
5. Name the four drag-and-drop interfaces. Which is called first when you let go: `OnDrop` or `OnEndDrag`?
6. Why must the DragIcon have Raycast Target switched off?
7. Why is `CraftingPanelUI` not on the `CraftingPanel` object itself?
8. You drop a Gold Bar on the result slot and then close the panel. Where does the Gold Bar go?

<details>
<summary>Answers</summary>

1. Recipes are data. A new recipe is a new asset, made in the Editor, without changing or
   recompiling code. Every crafting grid can share the same recipe assets.
2. No. The first check fails: the grid has 4 items, the recipe's total is 3 (the coin is extra).
3. No, it is only a preview. It changes when the player drags it to the hotbar (`CompleteCraft`).
4. So a full inventory never loses the materials: if there is no room, nothing changes at all.
5. `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IDropHandler`. `OnDrop` comes first
   (on the target), then `OnEndDrag` (on the source).
6. Otherwise the icon itself is what the mouse "hits", and the slot below never gets `OnDrop`.
7. The panel is hidden with `SetActive(false)`. A hidden object does not run `Update`, so the C key
   could never open the panel again.
8. Back into the inventory: `ClosePanel` calls `ReturnEverythingToInventory`, which cancels the uncraft.

</details>

---

## Homework

Use the exercise sheets in `Docs/` (solutions are in the matching `_Solutions` files):

| Part | Exercises |
|---|---|
| 1 | `Sprint10_Exercises.txt`: a new recipe without code, count craftable recipes, result count 2 |
| 2 | `Sprint11_Exercises.txt`: restyle the panel, Escape closes it, pause with Time.timeScale |
| 3 | `Sprint12_Exercises.txt`: bigger drag icon, right-click to return, swap hotbar slots |
| 4 | `Sprint13_Exercises.txt`: a new two-step recipe, recipe hint text, a craft counter |

---

## Learn more

- [ScriptableObject (Unity Manual)](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html)
- [CreateAssetMenuAttribute](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CreateAssetMenuAttribute.html)
- [EventSystem](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/EventSystem.html)
- [Input System: UI support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/UISupport.html)
- [Graphic Raycaster](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-GraphicRaycaster.html)
- [Supported Events (drag-and-drop interfaces)](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/SupportedEvents.html)
- [PointerEventData](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.EventSystems.PointerEventData.html)
- [Grid Layout Group](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-GridLayout.html)
- [Button](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-Button.html)
- [Cursor.lockState](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Cursor-lockState.html)
- [Behaviour.enabled](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Behaviour-enabled.html)
- [C# interfaces](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/interfaces)
