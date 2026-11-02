# Task: Build a "Crafting System" for the Module10_TNI Unity project

You are working in the Unity project at `D:\Unity\Unity Projects\Module10_TNI`. It is a teaching project for a beginner Unity course. Build the new topic **Crafting System** from start to finish: plan it, write the code, set up the scene, test it, and write the docs. Work autonomously. Do not stop to ask for approval between sprints. Stop only if you are truly blocked (for example, Unity cannot be found, or a test keeps failing after three honest fix attempts). If you stop, explain exactly what is blocking you.

## 1. Read first (do not skip)

1. `CLAUDE.md` in the project root: project rules, tech stack, scene hierarchy, input rules, Unity Editor path.
2. `Docs/Milestone3_Inventory_Summary.txt`: what the inventory milestone built, and its known debt.
3. `Docs/InventoryBasics_Lesson.md`: how the inventory is taught. The crafting lesson must follow on from it.
4. Every script in `Assets/Scripts/` (`Interaction/`, `Combat/`, `Enemy/`, `Inventory/`, `UI/`) and in `Assets/Editor/`. Never assume what the code looks like. When a file has `#region (Code) ... #endregion` blocks, the latest uncommented one is the current code.
5. `Assets/Scenes/ThirdPerson.unity` hierarchy (Player, HUDCanvas, Hotbar, ItemPickups).

## 2. Project facts you must respect

- Unity 6000.3.10f1 (Unity 6.3 LTS), URP, Input System 1.18 only (never the old Input Manager), TextMeshPro, Cinemachine 3.
- Input is read with `InputSystem.actions.FindAction("Name")`, cached in `Awake`. Add new actions to the `Player` map of `Assets/InputSystem_Actions.inputactions`. Never create a second input asset.
- The inventory already exists and must be reused, not replaced:
  - `ItemData` (ScriptableObject: name, icon, max stack, heal amount)
  - `InventorySlot` (item + count)
  - `Inventory` (6 slots, `AddItem` stacks first, `RemoveItem`, selected slot)
  - `HotbarUI` / `HotbarSlotUI` (6 boxes at the bottom-centre of `HUDCanvas`, refreshed every frame)
  - `PlayerItemUser` (keys 1-6, Q to use)
  - Extend these scripts only where crafting needs it, and keep their current behaviour working.
- **The scene has no EventSystem, and HUDCanvas has no GraphicRaycaster.** UI clicks and drag-and-drop need both. Add an `EventSystem` with an **InputSystemUIInputModule** (never the old Standalone Input Module) and a `GraphicRaycaster` on `HUDCanvas`. The hotbar images currently have Raycast Target off; turn it on where dragging needs it.
- During normal play the cursor is locked by `ThirdPersonController`.
- Target scene: **ThirdPerson** only. FirstPerson is out of scope.
- Do not touch `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `.vs/`, `.csproj`, `.sln`, or `AGENTS.md`.
- Do not change the behaviour of the combat or movement scripts. Turning them off and on with `enabled` from a crafting script is allowed, the same way `PlayerDeath` does it.

## 3. Code style (required)

Students are beginners who finished Modules 01-09 and Inventory Basics. Write every new script in **Unity tutorial style**:
- Plain methods and simple `if` statements. Do not use:
  - `=>` expression-bodied members, `nameof`, LINQ or lambdas
  - generics beyond `List<T>`
  - static managers or singletons
  - interfaces of your own
- **Exception for this topic:** Unity's drag-and-drop interfaces from `UnityEngine.EventSystems` are allowed: `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IDropHandler`, `IPointerClickHandler`. Explain each one in a comment the first time it is used.
- String names for Animator parameters and Input actions.
- `[SerializeField] private` fields grouped with `[Header]`. Clear, descriptive names.
- **Human-like comments** above each class, field group and method. They explain what the code does and why, in plain English a teacher can read out to students.
- Keep the crafting **logic** (recipes, matching, crafting, uncrafting) in plain scripts that have no UI code, so it can be tested without Play mode. The UI scripts only show the state and forward drags and clicks.
- One clear responsibility per script. Build only what the topic needs.

## 4. Scope of the Crafting System

Teach these ideas:
- recipes as data
- a crafting grid
- matching a recipe
- a preview result
- crafting is completed only when the result is taken
- uncrafting (taking an item apart)
- drag and drop between UI slots

### 4.1 Opening the crafting panel

- A **Craft** button on `HUDCanvas` (bottom-right, label "Craft (C)").
- Clicking it, or pressing **C** (new input action `ToggleCrafting`), opens and closes the crafting panel.
- The cursor is locked during play, so pressing C is the normal way to open the panel. The button is clickable while the panel is open (to close it) and whenever the cursor is free.
- While the panel is open:
  - The cursor is unlocked and visible.
  - These Player scripts are turned off: `ThirdPersonController`, `PlayerCombat`, `PlayerInteractor`, `PlayerItemUser`. Clicking and dragging must not punch, look around or move.
- When the panel is closed: lock the cursor again and turn the scripts back on, **unless the player is dead** (check `Health.IsDead`).
- If the player dies while the panel is open, close it.
- The hotbar stays visible while the panel is open. It is the "inventory" the player drags to and from.

### 4.2 Panel layout

The panel is centred on the screen. From left to right:
- a **3x3 grid** of 9 crafting slots
- a **two-sided arrow** (⇄, an Image or a TMP text). It shows that crafting works in both directions.
- **one result slot**

Each slot looks like a hotbar box: an icon, plus a count when it is 2 or more. Preview (not yet real) items are drawn **half transparent**.

### 4.3 Recipes

- `CraftingRecipe` ScriptableObject (`[CreateAssetMenu]`):
  - a `List` of ingredients, where each ingredient is a small `[System.Serializable]` class: `ItemData` + count
  - a result `ItemData`
  - a result count (default 1)
- Recipes are **shapeless**: the position of the materials in the 9 slots does not matter, only which items and how many. The grid must contain **exactly** the ingredients of a recipe. Extra items mean no match.
- Each crafting grid slot holds **one** item (count 1). This keeps matching easy to explain: "count the coins in the grid".
- Create the recipes in `Assets/Crafting/Recipes/`, and the new items in `Assets/Items/`. Generate their 64x64 icons into `Assets/Items/Icons/` the same way `InventorySceneSetup` does.

| Recipe | Ingredients | Result |
|---|---|---|
| `GoldBar` | 4 Coin | 1 Gold Bar (new item, stack 10, not usable) |
| `BigHealthPotion` | 2 Health Potion + 1 Gem | 1 Big Health Potion (new item, stack 3, heals 60) |
| `GemRing` | 1 Gold Bar + 1 Gem | 1 Gem Ring (new item, stack 1, not usable) |

GemRing shows a two-step recipe: Coins → Gold Bar → Gem Ring.

Add pickups so every recipe can be tried in one play session: 4 more coins (9 in total) and 2 more gems (3 in total). Add them under `ItemPickups`.

### 4.4 Crafting (materials → item)

1. The player drags an item from a hotbar slot onto a crafting slot. **One** item moves: the hotbar stack goes down by 1, and the crafting slot holds that item. Dropping onto an occupied crafting slot is rejected, and the item returns.
2. When the grid matches a recipe, the result slot shows the result as a **preview** (half transparent). It is not real yet.
3. If the grid changes so that it no longer matches, the preview disappears.
4. **Crafting is completed only when the player drags the result from the result slot onto the hotbar:**
   - The result is added with `Inventory.AddItem` (so normal stacking rules decide which slot).
   - Then the ingredients in the grid are consumed (removed).
   - If the inventory is full, nothing changes: the result stays a preview and the materials stay.
5. The player can drag a material from the grid back onto the hotbar. That cancels it and returns the item.

### 4.5 Uncrafting (item → materials)

1. With an **empty** grid, the player drags a craftable item (one that is the result of a recipe) from the hotbar onto the result slot. One item moves out of the hotbar.
   - Items that are not the result of any recipe are rejected and return.
   - Dropping onto the result slot while the grid is not empty is also rejected.
2. The 9 slots then show that recipe's ingredients as **previews** (half transparent), one item per slot.
3. **Uncrafting is completed when the player drags the first preview material onto the hotbar:**
   - That material is added to the Inventory.
   - The crafted item in the result slot is consumed.
   - All the other previews become real items in the grid. The player drags them to the hotbar one by one.
   - If the inventory is full, nothing changes.
4. Before completing, the player can drag the crafted item from the result slot back onto the hotbar. That cancels: it returns, and the previews disappear.

### 4.6 Closing the panel with items still in it

Every **real** item in the grid, and a crafted item waiting in the result slot (uncraft not completed), is returned to the Inventory with `AddItem`. Previews are simply cleared. Items that do not fit stay in the grid until the panel is opened again; log "Inventory full, some items stay in the crafting grid".

### 4.7 Drag-and-drop feel

- While dragging, the item icon follows the mouse, drawn on top of everything. The source slot shows the item as half transparent until the drop succeeds or fails.
- Dropping anywhere that is not a valid target cancels the drag and puts everything back.
- Log every craft, uncraft, move, rejection and cancel to the Console in plain words. For example: "Crafted Gold Bar from 4 Coin", "Can't uncraft Coin: no recipe makes it".

### 4.8 Suggested scripts

You may adjust this list if a simpler split is clearer. Explain any change in the plan.

| Script | Responsibility |
|---|---|
| `Crafting/CraftingIngredient.cs` | Serializable: item + count |
| `Crafting/CraftingRecipe.cs` | ScriptableObject: ingredients, result, result count |
| `Crafting/CraftingGrid.cs` | On the Player. The logic only: 9 grid slots (reuse `InventorySlot`), the result slot, the recipe list, preview state (crafting / uncrafting / none), `FindMatchingRecipe`, completing a craft or uncraft, return-to-inventory. No UI code. |
| `UI/CraftingPanelUI.cs` | Opens and closes the panel (C key and Craft button), cursor, turns Player scripts off and on, refreshes the 9 + 1 slot views every frame (same idea as `HotbarUI`) |
| `UI/CraftingSlotUI.cs` | Draws one crafting or result slot (icon, count, preview transparency) and receives drops |
| `UI/ItemDragUI.cs` | The icon that follows the mouse during a drag |
| `HotbarSlotUI.cs` (extend) | Becomes draggable and accepts drops. Needs to know its slot index. |

## 5. How to make Unity changes safely

These problems happened in this project before. Avoid them:

- Unity and the code editor overwrite files. Before you start, make sure the Unity Editor is closed: check for `Temp/UnityLockfile`, and for a running `Unity.exe` whose command line has this project path. If it is open, say so and stop. Re-read every file from disk right before you edit it.
- If `git status` shows uncommitted changes in `Assets/Scenes/ThirdPerson.unity` before you start, report them and **do not discard them**. Build on top of them.
- Do not hand-write scene YAML for new objects. Instead, write an Editor script `Assets/Editor/CraftingSceneSetup.cs` (menu `Tools/Setup Crafting Scene`) with a static method that uses the Unity API to:
  - open ThirdPerson
  - create the new items, icons, recipes, extra pickups, the EventSystem, the GraphicRaycaster, the Craft button and the crafting panel
  - assign all references and save

  Run it in batchmode with `-executeMethod`. Make it safe to run twice: find or replace existing objects, no duplicates. Open the scene **before** creating assets (opening a scene unloads assets that are only held in memory).
- New scripts referenced by scenes or assets must get their `.meta` files from Unity (batchmode import), not invented by hand.
- To edit the input asset, keep its exact JSON format (4-space indent, LF, no trailing newline). Only add entries; check that `git diff` shows additions only.

Unity Editor path (from CLAUDE.md): `D:\Unity\Unity 6(6000.0.46f1)\6000.3.10f1\Editor\Unity.exe`. Batchmode pattern:

```
"<Unity.exe>" -batchmode -nographics -quit -projectPath "D:\Unity\Unity Projects\Module10_TNI" -executeMethod <Class.Method> -logFile "<scratch>\unity_<step>.log"
```

## 6. Testing (required, every sprint)

1. **Compile check:** run batchmode with `-quit` and scan the log for `error CS` and `Exception`. Zero allowed.
2. **Logic tests:** write `Assets/Editor/CraftingValidation.cs` with a static `RunAll()` method, runnable with `-executeMethod`, plus a menu item `Tools/Validate Crafting`. Without Play mode it must check:
   - **Matching:** 4 coins anywhere in the grid match GoldBar; 3 coins or 5 coins do not; 4 coins + 1 gem do not; 2 potions + 1 gem in any slots match BigHealthPotion.
   - **Craft:** a preview appears on a match and disappears when an ingredient is removed. Completing the craft adds the result to the Inventory and empties the grid. With a full Inventory, nothing changes.
   - **Uncraft:** a Gold Bar on the result slot with an empty grid shows 4 coin previews. Taking the first coin adds it to the Inventory, consumes the Gold Bar and turns the other 3 into real items. Cancelling returns the Gold Bar. A Coin on the result slot is rejected. A Gold Bar on the result slot with a non-empty grid is rejected.
   - **Closing:** real grid items and an uncompleted uncraft item go back to the Inventory; leftovers stay when the Inventory is full.
   - **Moving items:** moving one item from an inventory slot to a crafting slot lowers the stack by 1. Dropping on an occupied crafting slot is rejected.
   - **Scene:**
     - an EventSystem with InputSystemUIInputModule exists, and HUDCanvas has a GraphicRaycaster
     - the Craft button and the crafting panel exist, with 9 grid slots + 1 result slot wired up; the panel starts hidden
     - the Player has CraftingGrid with all 3 recipes
     - the recipes and the new items have icons
     - the input asset has `ToggleCrafting` bound to C
     - there are 9 coins and 3 gems in `ItemPickups`
   - **The old checks still pass:** run `InventoryValidation.RunAll` logic too, or call its checks, so Inventory Basics does not break.
   - Log `[PASS]` / `[FAIL]` lines and a `[SUMMARY]`. Call `EditorApplication.Exit(1)` if anything fails, `Exit(0)` otherwise. Run it and read the log. Fix and re-run until everything passes.
3. **Play-mode check by the teacher:** you cannot click or drag in the Game view, so write a short manual test list into each SprintDoc for the teacher to run.

## 7. Workflow and deliverables

Work in **Milestone 4: Crafting System**, split into sprints, continuing the numbering from Milestone 3 (start at **Sprint 10**). Suggested split:
- **10:** recipes, new items and CraftingGrid logic (craft and uncraft, tested without UI)
- **11:** EventSystem, Craft button, panel layout, open and close (cursor, Player scripts)
- **12:** drag and drop between the hotbar and the crafting slots; crafting through the UI
- **13:** uncrafting through the UI, closing with items, polish and the lesson guide

Before coding, write the plan to `Docs/Milestone4_Crafting_Plan.txt` (goals, files per sprint, done conditions). Then build sprint by sprint.

For each sprint, in `Docs/`:
- `SprintDoc_SprintN.txt`: goals, how it works in plain words, files changed, done conditions, a manual Play-mode test list, and a "Learn more" section pointing to the relevant Unity Manual / Scripting API pages (EventSystem, drag-and-drop interfaces, ScriptableObject, Cursor.lockState).
- `SprintN_Exercises.txt`: easy, medium, challenge.
- `SprintN_Solutions.txt`: a separate file with the solutions.
- A ready-to-copy Git commit message, at the end of the SprintDoc. Never add Co-Authored-By or Claude-Session lines. Do not run `git commit` or `git push` yourself.

At the end of the milestone:
- `Docs/Milestone4_Crafting_Summary.txt`: what was built, scripts at a glance, the delivery checklist, known debt carried into the next milestone, all commit messages, and one combined commit message for the whole milestone.
- `Docs/CraftingSystem_Lesson.md`: a teacher's lesson guide in the same format as `Docs/InventoryBasics_Lesson.md`:
  - lesson at a glance, big ideas with analogies
  - full script listings that match the project files exactly
  - Editor steps, checkpoints, common mistakes, a quiz with answers, homework
- Update `CLAUDE.md`: project structure, scripts table, input actions, scene hierarchy, current status.

## 8. Final report

Finish with a short report that includes:
- what was built
- the test results (paste the PASS/FAIL summary)
- anything you could not verify
- the manual Play-mode checklist for the teacher
- the combined commit message
