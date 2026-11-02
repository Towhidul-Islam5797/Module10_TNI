# Task: Build "Inventory Basics" for the Module10_TNI Unity project

You are working in the Unity project at `D:\Unity\Unity Projects\Module10_TNI`. It is a teaching project for a beginner Unity course. Build the new topic **Inventory Basics** from start to finish: plan it, write the code, set up the scene, test it, and write the docs. Work autonomously. Do not stop to ask for approval between sprints. Stop only if you are truly blocked (for example Unity cannot be found or a test keeps failing after three honest fix attempts), and then explain exactly what is blocking you.

## 1. Read first (do not skip)

1. `CLAUDE.md` in the project root: project rules, tech stack, scene hierarchy, input rules.
2. `Docs/Milestone2_Combat_Summary.txt`: what was built in the last milestone and the known debt.
3. Every script in `Assets/Scripts/` (including `Interaction/`, `Combat/`, `UI/`, `Enemy/`). Never assume what the code looks like. When a file has `#region (Code) ... #endregion` blocks, the latest uncommented one is the current code.
4. `Assets/Scenes/ThirdPerson.unity` hierarchy (Player, HUDCanvas, Enemy).

## 2. Project facts you must respect

- Unity 6000.3.10f1 (Unity 6.3 LTS), URP, Input System 1.18 only (never the old Input Manager), TextMeshPro, Cinemachine 3.
- Input is read with `InputSystem.actions.FindAction("Name")`, cached in `Awake`. Use the existing Project-wide Actions asset `Assets/InputSystem_Actions.inputactions`. Add new actions to its `Player` map. Never create a second input asset.
- An interaction system already exists: `IInteractable` (`PromptText`, `Interact(PlayerInteractor)`), and `PlayerInteractor` raycasts from the camera, shows the prompt text, and calls `Interact` on E. **Item pickups must use this system.**
- `Health.cs` exists (CurrentHealth, MaxHealth, IsDead, TakeDamage). Extend it, do not replace it.
- Target scene: **ThirdPerson** only. FirstPerson is out of scope.
- Do not touch `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `.vs/`, `.csproj`, `.sln`, or `AGENTS.md`.
- Do not change behaviour of the combat scripts, except adding `Heal` to `Health`.

## 3. Code style (required)

Students are beginners who finished Modules 01-09. Write every new script in **Unity tutorial style**:
- Plain methods and simple `if` statements. No `=>` expression-bodied members, no `nameof`, no LINQ, no lambdas, no generics beyond `List<T>`, no static managers or singletons, no interfaces other than the existing `IInteractable`.
- String names for Animator parameters and Input actions.
- `[SerializeField] private` fields grouped with `[Header]`. Clear, descriptive names.
- **Human-like comments** above each class, field group and method, explaining what it does and why, in plain English a teacher can read out to students. This overrides the "no unnecessary comments" line in CLAUDE.md; update that line in CLAUDE.md to say course scripts get explanatory comments.
- One clear responsibility per script. Build only what the topic needs.

## 4. Scope of Inventory Basics

Teach these ideas: item data, an inventory as a list of slots, stacking, picking up, showing the inventory on screen, and using an item.

1. **Item data**: `ItemData` ScriptableObject (`[CreateAssetMenu]`) with item name, icon (Sprite), max stack size, and a simple heal amount (0 = not usable). Create three assets in `Assets/Items/`: `Coin` (stack 99, not usable), `HealthPotion` (stack 5, heals 25), `Gem` (stack 1, not usable).
2. **Inventory**: `Inventory` on the Player. A fixed number of slots (6). Each slot holds an `ItemData` and a count (a small `[System.Serializable]` class `InventorySlot`). `AddItem` fills existing stacks first, then empty slots, and returns false when full. `RemoveItem` from a slot. Logs what happened to the Console.
3. **Pickups**: `ItemPickup` implements `IInteractable`. Prompt: "Press E to pick up <name>". On interact, adds the item to the player's Inventory and destroys itself; if the inventory is full it stays and logs "Inventory full". Place in the scene: 5 coins, 2 health potions, 1 gem, spread near the player start. Use simple primitives (small cylinder for coin, capsule for potion, cube for gem) with distinct colored URP Lit materials in `Assets/Items/Materials/`.
4. **Inventory UI**: a hotbar of 6 slots at the bottom-centre of `HUDCanvas`. Each slot shows the icon and the count (TMP, hidden when count is 1 or 0). The selected slot is highlighted. Keep the update simple and explain it in comments (refreshing the UI when the inventory changes, or every frame, is fine; pick the simplest that a beginner understands and say why).
5. **Using items**: number keys 1-6 select a slot (add actions `Slot1`..`Slot6` to the Input asset, or one action with 6 bindings if clearer). Add an action `UseItem` bound to **Q**. Using a HealthPotion heals the player through `Health.Heal(amount)` (clamped to max, does nothing when dead) and removes one from the stack. Using a non-usable item logs "Can't use <name>". The player green health bar must go up.
6. **Icons**: there is no icon art. Generate simple, clean 64x64 PNG icons (flat colored shapes matching each item) into `Assets/Items/Icons/`, imported as Sprite (2D and UI).

## 5. How to make Unity changes safely

These problems happened in this project before. Avoid them:
- **Unity and the code editor overwrite files.** Before you start, make sure the Unity Editor is closed (check for `Temp/UnityLockfile` or a running Unity process). If it is open, say so and stop. Re-read every file from disk right before editing it.
- **Do not hand-write scene YAML** for new objects. Instead write an Editor script `Assets/Editor/InventorySceneSetup.cs` with a static method that uses the Unity API to open ThirdPerson, create the pickups, the hotbar UI, assign references, create the ItemData assets and materials, set import settings, and save. Run it in batchmode with `-executeMethod`. Make it safe to run twice (find or replace existing objects, no duplicates).
- New scripts referenced by scenes or assets must get their `.meta` files from Unity (batchmode import), not invented by hand.

Find the Editor at `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe` (search Unity Hub install folders if not there). Batchmode pattern:
```
"<Unity.exe>" -batchmode -nographics -quit -projectPath "D:\Unity\Unity Projects\Module10_TNI" -executeMethod <Class.Method> -logFile "<scratch>\unity_<step>.log"
```

## 6. Testing (required, every sprint)

1. **Compile check**: run batchmode with `-quit` and scan the log for `error CS` and `Exception`. Zero allowed.
2. **Logic tests**: write an Editor validation script `Assets/Editor/InventoryValidation.cs` with a static `RunAll()` method, runnable by `-executeMethod`. It must, without Play mode:
   - create an Inventory on a temp GameObject and check: stacking fills existing stacks first, max stack is respected, full inventory returns false, RemoveItem lowers the count and clears an empty slot.
   - check `Health.Heal` clamps at max and does nothing when dead.
   - open ThirdPerson and check: Player has Inventory; HUDCanvas has 6 hotbar slots wired to the Inventory; each pickup has ItemPickup with an ItemData; ItemData assets have icons; the Input asset has UseItem and slot actions.
   - log `[PASS]` / `[FAIL]` lines and call `EditorApplication.Exit(1)` if anything fails, `Exit(0)` otherwise.
   Run it and read the log. Fix and re-run until everything passes. Leave the validation script in the project and add a menu item `Tools/Validate Inventory` for the teacher.
3. **Play-mode check by the teacher**: you cannot click in the Game view, so write a short manual test list into the SprintDoc for the teacher to run.

## 7. Workflow and deliverables

Work in **Milestone 3: Inventory Basics**, split into sprints, continuing the numbering from Milestone 2 (start at Sprint 6). Suggested split: 6 item data and Inventory core; 7 world pickups; 8 hotbar UI; 9 selecting and using items. Before coding, write the plan to `Docs/Milestone3_Inventory_Plan.txt` (goals, files per sprint, done conditions), then build sprint by sprint.

For each sprint, in `Docs/`:
- `SprintDoc_SprintN.txt`: goals, how it works in plain words, files changed, done conditions, manual Play-mode test list, a "Learn more" pointing to the relevant Unity Manual / Scripting API pages.
- `SprintN_Exercises.txt`: easy, medium, challenge.
- `SprintN_Solutions.txt`: separate file with solutions.
- A ready-to-copy Git commit message (put it at the end of the SprintDoc). **Never add Co-Authored-By or Claude-Session lines.** Do not run `git commit` or `git push` yourself.

At the end of the milestone:
- `Docs/Milestone3_Inventory_Summary.txt`: what was built, scripts at a glance, the delivery checklist, known debt carried into the next milestone, all commit messages, and one combined commit message for the whole milestone.
- Update `CLAUDE.md`: project structure, scripts table, input actions, scene hierarchy, current status.

## 8. Final report

Finish with a short report: what was built, test results (paste the PASS/FAIL summary), anything you could not verify, the manual Play-mode checklist for the teacher, and the combined commit message.
