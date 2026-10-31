# Module10_TNI

## Project Overview

Teaching project for Module 10 "Build a 3D Player" of a Unity course.
Students are beginners who finished Modules 01-09 (Unity basics, C#, movement/input, 2D physics,
enemies/combat, UI/TextMeshPro/scene management, audio/animation/polish, mobile builds,
3D basics/materials/lighting/prefabs/terrain).

Module 10 topics: 3D movement, keyboard and mouse input, Character Controller, Rigidbody physics,
jumping, gravity, first/third-person camera, interaction systems, raycasting.

All code and docs must be easy for these students to read, follow, and modify.

## Tech Stack

- Unity 6000.3.10f1 (Unity 6.3 LTS)
- Universal Render Pipeline 17.3.0 (Universal 3D template)
- Input System 1.18.0, Active Input Handling = Input System Package (New)
- Cinemachine 3.1.7 (installed)
- TextMeshPro (part of com.unity.ugui 2.0.0), TMP Essentials imported into `Assets/TextMesh Pro`
- Character art: Kevin Iglesias "Human Character Dummy" (`Assets/Kevin Iglesias`)

## Project Structure

```
Assets/
  Animations/
    FirstPersonAnimator.controller   Locomotion blend tree (Speed: Idle 0, Run 5) + Jump state (IsGrounded)
    ThirdPersonAnimator.controller   same setup
  Editor/
    InventorySceneSetup.cs           Tools > Setup Inventory Scene (builds item assets, pickups, hotbar; safe to re-run)
    InventoryValidation.cs           Tools > Validate Inventory (50 [PASS]/[FAIL] checks, batchmode: RunAll)
  InputSystem_Actions.inputactions   Project-wide Actions (template asset, kept on purpose)
  Items/
    Coin.asset, HealthPotion.asset, Gem.asset   ItemData assets
    Icons/                           64x64 generated icon PNGs (Sprite)
    Materials/                       URP Lit materials for the pickups
  Kevin Iglesias/                    Human Character Dummy models and prefabs
  Scenes/
    FirstPerson.unity
    ThirdPerson.unity
    SampleScene.unity                template leftover
  Scripts/
    FirstPersonController.cs
    ThirdPersonController.cs
    Interaction/
      IInteractable.cs
      PlayerInteractor.cs
      ColorBox.cs
      PickupBox.cs
    Combat/
      Health.cs, PlayerCombat.cs, PlayerDeath.cs, AttackEventRelay.cs (unused)
    Enemy/
      EnemyAI.cs
    Inventory/
      ItemData.cs, InventorySlot.cs, Inventory.cs, ItemPickup.cs, PlayerItemUser.cs
    UI/
      HealthBar.cs, FaceCamera.cs, HotbarUI.cs, HotbarSlotUI.cs
  Settings/                          URP assets (from template)
  TextMesh Pro/                      TMP Essentials
Docs/                                Build guide, SprintDocs, exercises, solutions
```

### Input

- `InputSystem_Actions` is the Project-wide Actions asset. Do not create another input asset.
- Actions used (Player map): Move (WASD), Look (Mouse Delta), Jump (Space), Interact (E),
  Attack (Left Click), Kick (Right Click), Slot1..Slot6 (keys 1-6), UseItem (Q).
- Template actions Previous/Next are also bound to keys 1/2 but no script uses them.
- Scripts read input with `InputSystem.actions.FindAction("Name")`, cached in `Awake`. No Player Input component.

### Scene Hierarchy

FirstPerson:
```
Main Camera            CinemachineBrain
FirstPersonCamera      CinemachineCamera (Hard Lock To Target, Rotate With Follow Target, FOV 60)
Player                 CharacterController, FirstPersonController, PlayerInteractor (layer Ignore Raycast)
  CameraRoot           y 1.6, tracking target of FirstPersonCamera, tilted by the script
    HoldPoint          where picked-up boxes sit
  HumanDummy           HumanDummy_M Blue prefab, Cast Shadows = Shadows Only
Plane                  scale 1000 x 1 x 1000
ColorBox_1..3, PickupBox_1..2
HUDCanvas              PromptText (TMP), Crosshair (Image)
Directional Light, Global Volume
```

ThirdPerson:
```
Main Camera            CinemachineBrain
ThirdPersonCamera      CinemachineCamera (Third Person Follow, distance 4, no Rotation Control)
Player                 CharacterController, ThirdPersonController, PlayerInteractor, PlayerCombat, Health,
                       PlayerDeath, Inventory, PlayerItemUser (layer Ignore Raycast)
  HumanDummy           HumanDummy_M Blue prefab
  CameraTarget         y 1.5, tracking target of ThirdPersonCamera, rotated by the script (world space)
  HoldPoint            in front of the dummy
Plane                  scale 1000 x 1 x 1000
ColorBox_1..3, PickupBox_1..2
ItemPickups            Coin_1..5, HealthPotion_1..2, Gem_1 (primitives + ItemPickup)
Enemy                  NavMeshAgent, EnemyAI, Health, collider (layer Enemy); EnemyHealthCanvas child
PatrolPoints           PatrolPoint_1..4
Navigation             NavMeshSurface
HUDCanvas              PromptText (TMP), Crosshair (Image), PlayerHealthBar,
                       Hotbar (HotbarUI, bottom-centre) > Slot_1..6 (HotbarSlotUI: Icon, Count, KeyLabel)
Directional Light, Global Volume
```

### Scripts

| Script | Responsibility |
|---|---|
| `FirstPersonController` | WASD move, mouse look (body yaw + CameraRoot pitch), jump, gravity, cursor lock, sets Animator Speed |
| `ThirdPersonController` | Camera-relative WASD move, face move direction, mouse look (rotates CameraTarget), jump, gravity, cursor lock, sets Animator Speed |
| `IInteractable` | Contract for usable objects: `PromptText`, `Interact(PlayerInteractor)` |
| `PlayerInteractor` | Raycast from the camera, E to interact, prompt text, carry/drop, Scene view debug ray |
| `ColorBox` | E cycles through a color list |
| `PickupBox` | E attaches to HoldPoint (kinematic, collider off), drop restores physics |
| `Health` | Max/current health, `TakeDamage`, `Heal` (clamped, not when dead), `IsDead` |
| `PlayerCombat` | Punch/kick combos, raycast damage on the Enemy layer |
| `PlayerDeath` | At 0 health: turn off player scripts, play Dying |
| `EnemyAI` | Patrol, chase, attack, death (NavMesh) |
| `HealthBar` / `FaceCamera` | Filled bar from a Health / world-space bar faces the camera |
| `ItemData` | ScriptableObject: item name, icon, max stack size, heal amount (0 = not usable) |
| `InventorySlot` | Serializable item + count |
| `Inventory` | 6 slots; `AddItem` stacks first then empty slots (false when full), `RemoveItem`, selected slot |
| `ItemPickup` | IInteractable: "Press E to pick up <name>", adds to Inventory and destroys itself |
| `PlayerItemUser` | Keys 1-6 select a slot, Q uses it (potion heals via `Health.Heal`) |
| `HotbarUI` / `HotbarSlotUI` | Every frame copies Inventory slots into 6 boxes (icon, count, highlight) |

## Code Rules

- Simple, readable C# a beginner can follow. No advanced patterns or abstractions.
- One clear responsibility per script and per method.
- `Update` only calls small, well-named methods (`HandleLook`, `HandleGravityAndJump`, `HandleMovement`).
- Clear, descriptive names. Named constants instead of magic numbers.
- Course scripts get explanatory comments above each class, field group and method, in plain English a
  teacher can read out to students (what it does and why).
- New student scripts use Unity tutorial style: plain methods, simple `if`s, no `=>` members, `nameof`, LINQ,
  lambdas, generics beyond `List<T>`, static managers/singletons, or new interfaces; string names for
  Animator parameters and Input actions.
- Use `[SerializeField] private` fields, grouped with `[Header]`.
- Read input only through `InputSystem.actions.FindAction`. Never use the old Input Manager.
- Use current Unity 6 APIs only (for example `linearVelocity`, not `velocity`).
- Character Controller players do not use a Rigidbody. Gravity and jump are calculated in code.
- When reading existing code, check the latest uncommented `#region (Code) ... #endregion` block.
- Build only what the current task needs.

## Workflow Rules

- Work in milestones split into sprints. One task at a time.
- Read the relevant existing files before proposing anything. Never assume what the code looks like.
- Propose a plan (what changes, which files, done condition) and wait for approval before writing code.
- Ask before starting if anything is unclear.
- Explain what code does and why in plain, simple language, and point to a doc page to learn more.
- Do not touch `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `.vs/`, or generated `.csproj`/`.sln` files.
- `AGENTS.md` is generated by the Unity Code Assist plugin. Do not edit it.

### Editing scene files

- Prefer describing Editor steps. Edit `.unity` files directly only when the user asks.
- For larger scene changes, write an Editor script that uses the Unity API and run it in batchmode
  (`-executeMethod`) with the Editor closed. Never hand-write YAML for new objects.
- Unity Editor path: `D:\Unity\Unity 6(6000.0.46f1)\6000.3.10f1\Editor\Unity.exe`
- Before editing a scene or script, re-read it from disk and compare. Unity (open scene + save) and the IDE
  (open old buffer + save) have both overwritten changes in this project before.
- Ask the user to Reload scenes in Unity after a scene file is changed on disk.
- Mouse look is done in code in both scenes. Cinemachine cameras only follow a target; no Input Axis Controller.

## Git Commit Messages

- Provide a ready-to-copy commit message at the end of each task.
- Never add `Co-Authored-By` or `Claude-Session` lines.

## Per-Sprint Deliverables

Stored in `Docs/`:

- `SprintDoc_SprintN.txt` - goals, steps, done conditions, files changed
- `SprintN_Exercises.txt` - easy, medium, challenge
- `SprintN_Solutions.txt` - solutions, separate file
- A ready-to-copy Git commit message

When the milestone is complete: a delivery checklist and a list of known debt carried into the next milestone.

## Current Status

Done:
- FirstPerson and ThirdPerson scenes with plane and HumanDummy
- Character Controller players with move, jump, gravity, keyboard and mouse input
- Cinemachine first-person and third-person cameras
- Raycast interaction system (color boxes, pickup boxes) with E
- HUD prompt text and crosshair
- Build guide: `Docs/Module10_BuildGuide.md`
- Animator Controllers with Idle/Run blend tree; scripts drive `Speed` and `IsGrounded`, Jump state linked to Locomotion (Mixamo clips still to be added by the user)
- Milestone 2 Combat (ThirdPerson, Sprints 1-5): enemy AI, punch/kick combos, health bars, enemy and player death.
  See `Docs/Milestone2_Combat_Summary.txt`
- Milestone 3 Inventory Basics (ThirdPerson, Sprints 6-9): ItemData, Inventory, pickups, hotbar, select and use.
  Validation 50/50 passed in batchmode; Play mode not yet confirmed. See `Docs/Milestone3_Inventory_Summary.txt`

Not done yet (from the original Milestone 1 plan):
- Rigidbody player scene (Rigidbody physics topic)
- Shared TestLevel prefab (ramps, steps, jump platform)
- Template cleanup (`SampleScene`, `TutorialInfo`, `Readme.asset`) and Build Profiles scene list
- SprintDocs, exercises, and solutions for the work above
