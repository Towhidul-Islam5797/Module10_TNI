# Inventory Basics - Lesson Guide

A teacher's guide for Module 10, Milestone 3. Students build a small inventory in the
**ThirdPerson** scene, step by step, by hand.

Result: coins, potions and a gem lie on the ground. The player looks at one and presses **E**
to pick it up. It appears in a row of 6 boxes at the bottom of the screen. Keys **1-6**
select a box, and **Q** uses the item. A Health Potion heals the player, and the green
health bar goes up.

---

## Lesson at a glance

| Part | Topic | New script(s) | Time |
|---|---|---|---|
| 1 | Item data and the Inventory | `ItemData`, `InventorySlot`, `Inventory` | 60 min |
| 2 | Picking items up | `ItemPickup` | 45 min |
| 3 | Showing the inventory on screen | `HotbarSlotUI`, `HotbarUI` | 60 min |
| 4 | Selecting and using items | `PlayerItemUser`, `Health.Heal` | 45 min |

**Students should already know:** the ThirdPerson scene, the `IInteractable` /
`PlayerInteractor` system (E to interact), `Health` and `HealthBar`, Canvas and TextMeshPro,
reading input with `InputSystem.actions.FindAction`.

**By the end, students can explain:**
- why item information lives in a **ScriptableObject** asset
- how an inventory is a **list of slots**, each holding an item and a count
- how **stacking** works: fill an existing stack first, then use an empty slot
- how a new item type plugs into an existing **interface** (`IInteractable`)
- how the UI **shows data** from another script, and why we refresh it every frame
- how one key press goes from **input action**, through the inventory, to **health**

**Reference files in the project:** the finished scripts live in `Assets/Scripts/Inventory` and
`Assets/Scripts/UI`. The sprint docs, exercises and solutions are in `Docs/` (Sprint 6 to Sprint 9).

---

## Before class (teacher)

1. Make a copy of the project, or a Git branch, at the commit **before** Milestone 3, for
   students to start from. Students then build everything themselves.
2. Copy the three icons `Assets/Items/Icons/Coin.png`, `HealthPotion.png` and `Gem.png` from the
   finished project. Students import these; they do not draw icons.
3. Keep the finished project open on your own machine to demo the end result first (5 minutes
   of play is the best motivation).

**Catch-up tools** (finished project only, menu **Tools**):
- **Setup Inventory Scene** builds all the assets, pickups and the hotbar in ThirdPerson.
  Use it if a student falls far behind. It is safe to run twice.
- **Validate Inventory** runs 50 checks without Play mode and prints `[PASS]` / `[FAIL]` to the
  Console. It is a quick way to find what is missing in a student's project.

---

# Part 1 - Item data and the Inventory

## The big idea

Ask the class: *"There are 5 coins on the ground. Where should the word 'Coin', its picture
and 'max 99 per slot' be stored?"*

- On every coin? Then changing the icon means changing 5 objects (or 500).
- **Once, in a shared file**, which every coin points to. That is a **ScriptableObject**.

> **Analogy:** a ScriptableObject is like a **recipe card**. The card says what a Health Potion
> is. Every potion in the world is "made from" that one card.

Then the inventory itself:

> **Analogy:** the inventory is an **egg box with 6 cups**. Each cup (a **slot**) can hold one
> *kind* of thing, plus a number saying how many. Coins stack up to 99 in one cup. A Gem needs
> its own cup.

## Step 1.1 - ItemData (the recipe card)

Create the folder `Assets/Scripts/Inventory`, then the script `ItemData.cs`:

```csharp
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
```

**Talk through:**
- `: ScriptableObject` instead of `: MonoBehaviour`, so this script is never added to a GameObject.
- `[CreateAssetMenu]` adds the new entry to the **Create** menu.
- The fields are private and have **read-only properties** (only a `get`). Other scripts can
  *read* how big a stack is, but they cannot change the rules of the game.

## Step 1.2 - Import the icons

1. Create `Assets/Items/Icons` and drag in `Coin.png`, `HealthPotion.png` and `Gem.png`.
2. Select all three. In the Inspector: **Texture Type = Sprite (2D and UI)**, **Generate Mip Maps**
   off, **Apply**.

**Why:** UI `Image` components can only show **Sprites**, not plain textures.

## Step 1.3 - Make the three items

In `Assets/Items`: **right-click > Create > Inventory > Item Data**, three times.

| Asset | Item Name | Icon | Max Stack Size | Heal Amount |
|---|---|---|---|---|
| `Coin` | Coin | Coin | 99 | 0 |
| `HealthPotion` | Health Potion | HealthPotion | 5 | 25 |
| `Gem` | Gem | Gem | 1 | 0 |

**Ask the class:** *"Which of these items is usable? How does the code know?"*
(Only the potion, because `HealAmount > 0`.)

## Step 1.4 - InventorySlot (one cup of the egg box)

`Assets/Scripts/Inventory/InventorySlot.cs`:

```csharp
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
```

**Talk through:** there is no `using UnityEngine;` and no `MonoBehaviour`. This is plain C#, a
small box of data. `null` means "nothing here".

## Step 1.5 - Inventory (the whole egg box)

`Assets/Scripts/Inventory/Inventory.cs`:

```csharp
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
```

Add **Inventory** to the `Player` in ThirdPerson.

### How AddItem works (draw this on the board)

```
AddItem(Coin)
   |
   |-- Loop 1: any slot with Coin AND count < 99?  -- yes --> count + 1, return true
   |
   |-- Loop 2: any empty slot?                       -- yes --> put Coin there, count 1, return true
   |
   '-- nothing found --> "Inventory full", return false
```

**Board exercise (5 minutes):** draw 6 empty boxes. Call out items one at a time and let the
class fill the boxes using only the two rules:
`Coin, Coin, Gem, Potion, Gem, Coin, Potion`
→ `[Coin 3] [Gem 1] [Potion 2] [Gem 1] [ ] [ ]`

Then ask: *"Potion has max stack 5. What happens with the 6th potion?"* (It goes to a new empty slot.)

**Why two loops and not one?** If we put the item into the first empty slot straight away, coins
would be spread over many slots instead of stacking.

### Checkpoint 1

Press Play, select `Player`, and expand **Inventory > Slots**: there are 6 empty slots.
Nothing can add items yet; that comes in Part 2.

---

# Part 2 - Picking items up

## The big idea

We **already** have a system that says: *"Look at something and press E."* (`IInteractable`).
A pickup is just one more thing you can interact with, like `ColorBox` and `PickupBox`.

> **Ask the class:** *"What do we need to change in PlayerInteractor to support pickups?"*
> **Answer: nothing.** That is the point of an interface: new objects plug in without changing
> the player.

## Step 2.1 - ItemPickup

`Assets/Scripts/Inventory/ItemPickup.cs`:

```csharp
using UnityEngine;

// An item lying in the world that the player can pick up with E.
// It uses the interaction system we already have: PlayerInteractor looks at
// it with a raycast, shows our PromptText, and calls Interact when E is pressed.
// The pickup needs a Collider, otherwise the raycast cannot hit it.
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    // Which item this pickup gives the player (for example the Coin asset).
    [Header("Item")]
    [SerializeField] private ItemData itemData;

    // Lets other scripts (and the validation tool) see which item this is.
    public ItemData ItemData
    {
        get { return itemData; }
    }

    // The text PlayerInteractor shows on screen while the player looks at this item.
    public string PromptText
    {
        get { return "Press E to pick up " + itemData.ItemName; }
    }

    // Called by PlayerInteractor when the player presses E while looking at us.
    // The Inventory lives on the same Player GameObject as the PlayerInteractor,
    // so we can find it with GetComponent.
    public void Interact(PlayerInteractor interactor)
    {
        Inventory inventory = interactor.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("The player has no Inventory component.");
            return;
        }

        bool wasAdded = inventory.AddItem(itemData);

        // Only remove the item from the world if it really went into the inventory.
        // If the inventory is full, the item stays here so the player can come back for it.
        if (wasAdded)
        {
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventory full");
        }
    }
}
```

**Talk through:** `AddItem` returns `true` or `false`, and **we use that answer**. We only
destroy the coin if it really went into the inventory. Otherwise the coin would vanish forever.

## Step 2.2 - Materials

Create `Assets/Items/Materials`. **Right-click > Create > Material**, three times (the URP **Lit**
shader is the default):

| Material | Base Map color | Metallic | Smoothness |
|---|---|---|---|
| `Coin` | gold (255, 199, 38) | 0.8 | 0.7 |
| `HealthPotion` | red (230, 31, 51) | 0 | 0.8 |
| `Gem` | blue (51, 153, 255) | 0.3 | 0.9 |

## Step 2.3 - Place the pickups

1. **GameObject > Create Empty**, name it `ItemPickups`, position `0, 0, 0`.
2. Make one of each as a child, add **ItemPickup**, assign the **Item Data** and the material:

| Object | Shape (3D Object >) | Position | Rotation | Scale | Item Data |
|---|---|---|---|---|---|
| `Coin_1` | Cylinder | -3, 0.35, 0 | 90, 0, 0 | 0.5, 0.04, 0.5 | Coin |
| `HealthPotion_1` | Capsule | 3, 0.3, 0 | 0, 0, 0 | 0.3, 0.3, 0.3 | HealthPotion |
| `Gem_1` | Cube | 4.5, 0.5, -1.5 | 45, 0, 45 | 0.35, 0.35, 0.35 | Gem |

3. Duplicate (**Ctrl+D**) to get 5 coins and 2 potions, and spread them out:
   coins at `(-4, 0.35, 0.8)`, `(-5, 0.35, 1.6)`, `(-4, 0.35, -1)`, `(-5, 0.35, -0.2)`,
   and the second potion at `(4, 0.3, 1)`.
4. Save the scene.

**Why it works without extra setup:** every 3D primitive comes with a collider, so the raycast
from `PlayerInteractor` can hit it.

### Checkpoint 2

Press Play. Aim the crosshair at a coin: *"Press E to pick up Coin"*. Press **E**: the coin
disappears, the Console says `Added Coin to empty slot 1`, and the Inspector shows it in Slot 1.
Pick up a second coin: `Count: 2`.

**Test "full":** stop Play, set **Slot Count = 2** on the Inventory, and press Play. Pick up a coin
and a potion, then try the gem: it stays, and the Console says *Inventory full*. Set it back to 6.

---

# Part 3 - Showing the inventory on screen

## The big idea

The inventory is **data**. The hotbar is a **picture of that data**. Keep them separate:

- `Inventory` does not know the screen exists.
- `HotbarUI` only *reads* the inventory and draws it.

> **Analogy:** the inventory is the **scoreboard data**, and the hotbar is the **TV screen**
> showing it. Unplugging the TV does not change the score.

**How does the screen stay up to date?** Every frame, `HotbarUI` copies each slot into its box.
This is exactly what `HealthBar` already does with health.

- **Pro:** it is always correct, whoever changed the inventory.
- **Pro:** it is tiny and easy to read.
- **Con:** it does work even when nothing changed. That is fine for 6 boxes. Bigger games use
  **events** ("tell the UI only when something changes"), which is a good next step later.

## Step 3.1 - HotbarSlotUI (draws one box)

`Assets/Scripts/UI/HotbarSlotUI.cs`:

```csharp
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
```

## Step 3.2 - HotbarUI (draws all boxes)

`Assets/Scripts/UI/HotbarUI.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

// Shows the player's Inventory as a row of boxes at the bottom of the screen.
//
// How it stays up to date: every frame, Update copies each inventory slot into
// its matching HotbarSlotUI. This is the same idea as our HealthBar script.
// It is the simplest way to keep the screen correct: it does not matter which
// script changed the inventory (a pickup, using a potion, selecting a slot),
// the hotbar always shows the latest state one frame later.
// Six small slots are very cheap to update, so doing it every frame is fine here.
public class HotbarUI : MonoBehaviour
{
    // The inventory we want to show (on the Player).
    [Header("Inventory")]
    [SerializeField] private Inventory inventory;

    // One HotbarSlotUI per inventory slot, in order from left to right.
    [Header("Slots")]
    [SerializeField] private List<HotbarSlotUI> slotViews = new List<HotbarSlotUI>();

    private void Update()
    {
        RefreshSlots();
    }

    // Copies every inventory slot into the matching box on screen.
    private void RefreshSlots()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            // If there are more boxes than inventory slots, the extra boxes are left alone.
            if (i >= inventory.SlotCount)
            {
                return;
            }

            bool isSelected = i == inventory.SelectedSlotIndex;
            slotViews[i].Show(inventory.GetSlot(i), isSelected);
        }
    }
}
```

## Step 3.3 - Build the hotbar in the Editor

Target hierarchy:

```
HUDCanvas
  Hotbar          HotbarUI, Horizontal Layout Group
    Slot_1        Image (background), HotbarSlotUI
      Icon        Image
      Count       TextMeshPro text
      KeyLabel    TextMeshPro text "1"
    Slot_2 ... Slot_6
```

1. **Right-click HUDCanvas > Create Empty**, name it `Hotbar`.
   - Rect Transform: anchor preset **bottom-center** (hold **Shift+Alt** to also set the pivot),
     Pos Y `30`, Width `530`, Height `80`.
   - **Add Component > Horizontal Layout Group**: Spacing `10`, Child Alignment `Middle Center`,
     untick all **Control Child Size** and **Child Force Expand** boxes.
2. **Right-click Hotbar > UI > Image**, name it `Slot_1`, Width `80`, Height `80`.
   - Source Image `UISprite` (rounded), Image Type `Sliced`, Color black with alpha about 128,
     untick **Raycast Target**.
3. Inside `Slot_1`:
   - **UI > Image** `Icon`: anchor preset **stretch-stretch**, Left/Right/Top/Bottom `12`,
     tick **Preserve Aspect**, untick **Raycast Target**.
   - **UI > Text - TextMeshPro** `Count`: stretch, padding `5`, font size `24`, **Bold**,
     alignment bottom-right, empty text.
   - **UI > Text - TextMeshPro** `KeyLabel`: stretch, padding `5`, font size `16`,
     alignment top-left, text `1`.
4. On `Slot_1`, **Add Component > HotbarSlotUI**. Drag `Slot_1` (its own Image) into
   **Background**, `Icon` into **Icon Image**, and `Count` into **Count Text**.
5. Duplicate `Slot_1` five times (`Slot_2` to `Slot_6`) and change each KeyLabel to `2` ... `6`.
   The layout group lines them up for you.
6. On `Hotbar`, **Add Component > HotbarUI**. Drag `Player` into **Inventory**. Lock the
   Inspector (padlock), select `Slot_1` to `Slot_6` and drag them onto **Slot Views**, in order.
7. Save the scene.

**Why untick Raycast Target?** These images never need mouse clicks. Leaving it on makes the UI
do extra work and can block clicks on other UI later.

### Checkpoint 3

Press Play. Six numbered boxes appear at the bottom, with box 1 yellow. Pick up coins: the coin
icon shows, and a number appears from 2 coins on. Resize the Game view: the bar stays centred.

---

# Part 4 - Selecting and using items

## The big idea

Follow one key press all the way through:

```
Press Q
  -> Input action "UseItem"
  -> PlayerItemUser.UseSelectedItem()
       -> Inventory.GetSelectedSlot()      what is selected?
       -> ItemData.IsUsable()              can it be used?
       -> Health.Heal(25)                  do the effect
       -> Inventory.RemoveItem(slot)       use it up
  -> next frame: HealthBar grows and HotbarUI shows the new count (both by themselves)
```

Each script does **one job**. `PlayerItemUser` is the one that connects them.

## Step 4.1 - Input actions

Open `Assets/InputSystem_Actions` (double-click). In the **Player** map add these actions, each
with **Action Type = Button** and one keyboard binding:

| Action | Binding |
|---|---|
| `Slot1` ... `Slot6` | `1` ... `6` (Keyboard > By Character Mapped to Key, or use **Listen**) |
| `UseItem` | `Q` |

Click **Save Asset**. **Do not** create a second input asset.

**Why six actions and not one?** The name `Slot3` tells you exactly what the key does, so
there is no guessing in the code.

## Step 4.2 - Health.Heal

Open `Assets/Scripts/Combat/Health.cs` and add this method under `TakeDamage`. Change nothing else.

```csharp
    // Gives health back, for example when the player drinks a Health Potion.
    // Health can never go above maxHealth, and a dead character cannot be healed.
    public void Heal(int amount)
    {
        if (IsDead)
        {
            return;
        }

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log(name + " healed " + amount + ". Health: " + currentHealth);
    }
```

**Ask the class:** *"Health is 90 and the potion heals 25. Without `Mathf.Min`, what happens?"*
(115 / 100, so the health bar overflows.) `Mathf.Min` picks the **smaller** of the two numbers.

## Step 4.3 - PlayerItemUser

`Assets/Scripts/Inventory/PlayerItemUser.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player choose a hotbar slot with the number keys 1-6
// and use the item in that slot with Q.
// Right now the only usable item is the Health Potion: it heals the player
// and one potion is removed from the stack.
[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(Health))]
public class PlayerItemUser : MonoBehaviour
{
    // How many number keys we listen to. It matches the 6 hotbar slots.
    private const int SlotKeyCount = 6;

    private Inventory inventory;
    private Health health;
    private InputAction[] slotActions;
    private InputAction useItemAction;

    // Find the scripts and input actions once, when the game starts.
    // The actions Slot1 to Slot6 and UseItem live in InputSystem_Actions.
    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        health = GetComponent<Health>();
        useItemAction = InputSystem.actions.FindAction("UseItem");

        slotActions = new InputAction[SlotKeyCount];
        for (int i = 0; i < SlotKeyCount; i++)
        {
            slotActions[i] = InputSystem.actions.FindAction("Slot" + (i + 1));
        }
    }

    private void Update()
    {
        HandleSlotSelection();
        HandleUseItem();
    }

    // If a number key was pressed this frame, select that slot.
    // Key 1 is slot index 0, key 2 is slot index 1, and so on.
    private void HandleSlotSelection()
    {
        for (int i = 0; i < slotActions.Length; i++)
        {
            if (slotActions[i].WasPressedThisFrame())
            {
                inventory.SelectSlot(i);
            }
        }
    }

    // If Q was pressed this frame, try to use the selected item.
    private void HandleUseItem()
    {
        if (useItemAction.WasPressedThisFrame())
        {
            UseSelectedItem();
        }
    }

    // Checks, one at a time, whether the selected item can be used.
    // If any check fails we log the reason and stop, so nothing is wasted.
    private void UseSelectedItem()
    {
        // A dead player cannot drink potions.
        if (health.IsDead)
        {
            return;
        }

        InventorySlot slot = inventory.GetSelectedSlot();
        if (slot.IsEmpty())
        {
            Debug.Log("The selected slot is empty.");
            return;
        }

        ItemData item = slot.item;
        if (!item.IsUsable())
        {
            Debug.Log("Can't use " + item.ItemName);
            return;
        }

        // Don't waste a potion when health is already full.
        if (health.CurrentHealth >= health.MaxHealth)
        {
            Debug.Log("Health is already full. " + item.ItemName + " was not used.");
            return;
        }

        health.Heal(item.HealAmount);
        inventory.RemoveItem(inventory.SelectedSlotIndex);
    }
}
```

Add **PlayerItemUser** to the `Player`.

**Talk through:**
- `"Slot" + (i + 1)` builds the names `Slot1`, `Slot2`, ... in a loop. Key **1** is index **0**
  (programmers count from 0, players count from 1).
- `UseSelectedItem` is a list of **guard checks**: each `if` stops early with a reason. Only if
  every check passes do we heal and remove. This pattern is easy to read and easy to extend.

### Checkpoint 4 (final test)

1. Pick up both potions. Press **1-6**: the yellow box moves.
2. Select the potions at full health and press **Q**: *Health is already full*, and the count
   stays the same.
3. Let the enemy hit you, then press **Q**: the green bar grows by 25, and the count drops by one.
4. Select a coin and press **Q**: *Can't use Coin*.
5. Die, then press **Q** on a potion: nothing happens.
6. E, jump, punch and kick still work.

---

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| No "Inventory > Item Data" in the Create menu | `ItemData.cs` has a compile error, or `[CreateAssetMenu]` is missing | Check the Console, then fix the error |
| No prompt when looking at an item | The item has no collider, or is on the **Ignore Raycast** layer | Use a 3D primitive, layer Default |
| Prompt shows, but E does nothing | `Inventory` is not on the Player | Add Inventory to `Player` |
| `NullReferenceException` in `ItemPickup.PromptText` | **Item Data** is empty on that pickup | Assign the ItemData asset |
| Icon is white or missing | PNG is not imported as a **Sprite** | Texture Type = Sprite (2D and UI), Apply |
| `NullReferenceException` in `HotbarUI` | **Inventory** or a **Slot View** is not assigned | Drag Player and Slot_1..6 in |
| Boxes overlap or are huge | Layout group "Control Child Size" is ticked | Untick it, and set each slot to 80 x 80 |
| Keys 1-6 or Q do nothing | Action name is misspelled, or the asset was not saved | Names must be exactly `Slot1`...`Slot6`, `UseItem`; click **Save Asset** |
| Coins fill several slots instead of stacking | Loop 1 in `AddItem` is missing, or the coins use **different** ItemData assets | Check `AddItem`; all coins must use the one Coin asset |

---

## Quick quiz (end of class)

1. Why is `ItemData` a ScriptableObject and not a MonoBehaviour?
2. You pick up: Gem, Coin, Gem, Coin. Draw the 6 slots.
3. What does `AddItem` return when the inventory is full, and who uses that answer?
4. Why did we not need to change `PlayerInteractor` to pick up items?
5. Name one advantage and one disadvantage of refreshing the hotbar every frame.
6. Key **3** selects which slot index?
7. Health 90, potion heals 25. What is the health afterwards, and which line makes sure?

<details>
<summary>Answers</summary>

1. The item's information is shared by every copy of the item. It is stored once, as an asset,
   and is not tied to any GameObject in the scene.
2. `[Gem 1] [Coin 2] [Gem 1] [ ] [ ] [ ]`. Gem has max stack 1, so the second gem needs a new slot.
3. `false`. `ItemPickup` uses it: it only destroys itself when the answer is `true`.
4. `ItemPickup` implements `IInteractable`, and `PlayerInteractor` already works with anything
   that implements it.
5. Pro: always correct and simple. Con: it works every frame even when nothing changed.
6. Index **2** (`SelectSlot(i)` with `i = 2`, from the action `Slot3`).
7. **100**. `Mathf.Min(currentHealth + amount, maxHealth)` in `Health.Heal`.

</details>

---

## Homework

Use the exercise sheets in `Docs/` (solutions are in the matching `_Solutions` files):

| Part | Exercises |
|---|---|
| 1 | `Sprint6_Exercises.txt`: new Apple item, `CountItem`, adding several items at once |
| 2 | `Sprint7_Exercises.txt`: extra coin, spinning pickups, "Inventory full" on screen |
| 3 | `Sprint8_Exercises.txt`: colors and size, selected item name, bigger selected box |
| 4 | `Sprint9_Exercises.txt`: potion heals 40, mouse-wheel selection, dropping items |

---

## Learn more

- [ScriptableObject (Unity Manual)](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html)
- [CreateAssetMenuAttribute](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CreateAssetMenuAttribute.html)
- [Serializable](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Serializable.html)
- [C# List&lt;T&gt;](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)
- [C# interfaces](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/interfaces)
- [Object.Destroy](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.Destroy.html)
- [UI Image](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-Image.html)
- [Horizontal Layout Group](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-HorizontalLayout.html)
- [Input System: Actions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/Actions.html)
- [Mathf.Min](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mathf.Min.html)
