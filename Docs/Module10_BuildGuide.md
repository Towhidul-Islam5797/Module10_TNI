# Module 10 - Build a 3D Player: Build Guide

This guide explains how everything in the Module10_TNI project is built, step by step in the Unity Editor,
and why each step is done. Follow it from top to bottom to rebuild the project from an empty Universal 3D project.

Unity version: 6000.3.10f1 (Unity 6.3 LTS), Universal 3D (URP) template.

---

## Part 0 - Project Setup

1. Unity Hub > New project > **Universal 3D** template > name `Module10_TNI`.
2. **Edit > Project Settings > Player > Other Settings > Active Input Handling** = `Input System Package (New)`.
   The template already does this.
3. **Edit > Project Settings > Input System Package**: check that **Project-wide Actions** is `InputSystem_Actions`.
   This asset comes with the template and already has the actions we need:
   Move (WASD), Look (Mouse Delta), Jump (Space), Interact (E).
4. **Window > Package Manager > Unity Registry**: install **Cinemachine**.
5. **Window > TextMeshPro > Import TMP Essential Resources**. This creates `Assets/TextMesh Pro`.
6. Import the **Human Character Dummy** asset (Kevin Iglesias). It lands in `Assets/Kevin Iglesias`.
7. Create the folders `Assets/Scripts` and `Assets/Scripts/Interaction`.

**Why:** Project-wide Actions means any script can read input through `InputSystem.actions` without a
Player Input component. Cinemachine and TextMeshPro are needed for the camera and the on-screen prompt.

---

## Part 1 - Scenes, Ground and Character

Do this twice: once for `FirstPerson`, once for `ThirdPerson`.

1. **File > New Scene > Basic (URP)**. Save it in `Assets/Scenes` as `FirstPerson` (then `ThirdPerson`).
   The scene starts with a Main Camera, a Directional Light and a Global Volume.
2. **GameObject > 3D Object > Plane**. Position `0, 0, 0`, Scale `1000, 1, 1000`.
   A Unity plane is 10 x 10 units at scale 1, so this is a very large floor.
   It already has a **Mesh Collider**, so the player can stand on it.
3. **GameObject > Create Empty**, name it `Player`, position `0, 0, 0`.
4. Drag `Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M Blue` onto `Player` so it becomes
   a child. Rename it `HumanDummy` and set its position to `0, 0, 0`.

**Why:** `Player` holds the logic (controller, scripts). `HumanDummy` is only the visual. Keeping them
separate means you can swap the character model later without touching any script.

---

## Part 2 - Character Controller Player

### 2.1 Character Controller (both scenes)

Select `Player` > **Add Component > Character Controller**:

| Setting | Value |
|---|---|
| Slope Limit | 45 |
| Step Offset | 0.3 |
| Skin Width | 0.08 |
| Center | 0, 0.9, 0 |
| Radius | 0.3 |
| Height | 1.8 |

**Why:** A Character Controller is a capsule collider that moves with `Move()` and slides along walls.
It has no physics, so gravity and jumping are calculated in our script. That is why the player has
**no Rigidbody**.

### 2.2 First-person player (FirstPerson scene)

1. Right-click `Player` > **Create Empty**, name it `CameraRoot`, position `0, 1.6, 0` (eye height).
2. Select `HumanDummy`, find its **Skinned Mesh Renderer**, set **Cast Shadows** = `Shadows Only`.
   The body is invisible to the camera but still casts a shadow.
3. Add the script `Assets/Scripts/FirstPersonController.cs` to `Player`.
4. Drag `CameraRoot` into its **Camera Root** field.

### 2.3 Third-person player (ThirdPerson scene)

1. Right-click `Player` > **Create Empty**, name it `CameraTarget`, position `0, 1.5, 0` (chest/head height).
2. Add the script `Assets/Scripts/ThirdPersonController.cs` to `Player`.
3. Drag `Main Camera` into its **Camera Transform** field.
4. Drag `CameraTarget` into its **Camera Target** field.

### 2.4 How the movement code works

Both scripts follow the same pattern:

- **Awake** caches the Character Controller and finds the actions:
  `InputSystem.actions.FindAction("Move")`, `"Look"`, `"Jump"`.
- **Start** locks and hides the cursor.
- **Update** only calls small methods, in this order:

FirstPersonController:
```
HandleLook()            mouse X turns the Player, mouse Y tilts CameraRoot (clamped to +-80 degrees)
HandleGravityAndJump()  applies gravity, starts a jump when grounded and Space was pressed
HandleMovement()        WASD relative to the Player's facing, then CharacterController.Move
```

ThirdPersonController:
```
GetCameraRelativeDirection()  WASD relative to where the camera looks (flattened on the ground)
HandleRotation()              turns the Player smoothly toward the move direction (Slerp)
HandleLook()                  mouse X/Y rotate CameraTarget in world space (pitch clamped -30 to 70)
HandleGravityAndJump()        same as first person
HandleMovement()              CharacterController.Move
```

Key ideas:

- **Keyboard input:** `moveAction.ReadValue<Vector2>()` gives WASD as x (left/right) and y (forward/back).
- **Mouse input:** `lookAction.ReadValue<Vector2>()` gives how far the mouse moved this frame.
- **Gravity:** every frame `verticalVelocity += gravity * Time.deltaTime`. When grounded it is reset to
  `GroundedStickForce` (-2) so `isGrounded` stays reliable.
- **Jump:** `Mathf.Sqrt(jumpHeight * -2 * gravity)` is the start speed needed to reach `jumpHeight`
  (from the physics formula v^2 = 2gh).
- **WasPressedThisFrame()** is true only on the frame Space or E goes down, so holding the key does not repeat.

---

## Part 3 - Cinemachine Cameras

### 3.1 Cinemachine Brain (both scenes)

Select `Main Camera` > **Add Component > Cinemachine Brain**.

**Why:** The Brain moves the real camera to match whichever Cinemachine Camera is active. You never move
the Main Camera yourself.

### 3.2 First-person camera (FirstPerson scene)

1. **GameObject > Cinemachine > Cinemachine Camera**, name it `FirstPersonCamera`.
2. **Tracking Target** = `CameraRoot`.
3. **Position Control** = `Hard Lock to Target` (camera sits exactly on CameraRoot).
4. **Rotation Control** = `Rotate With Follow Target` (camera copies CameraRoot's rotation).
5. **Lens > Field Of View** = 60.

**Why:** The script still owns mouse look (it rotates CameraRoot). Cinemachine just glues the camera to it.
This is the same setup Unity's Starter Assets use.

### 3.3 Third-person camera (ThirdPerson scene)

1. **GameObject > Cinemachine > Cinemachine Camera**, name it `ThirdPersonCamera`.
2. **Tracking Target** = `CameraTarget`.
3. **Position Control** = `Third Person Follow`. **Rotation Control** = `None`.
4. On **Cinemachine Third Person Follow**:

| Setting | Value |
|---|---|
| Damping | 0.1, 0.5, 0.3 |
| Shoulder Offset | 0.5, -0.2, 0 |
| Vertical Arm Length | 0.4 |
| Camera Side | 1 |
| Camera Distance | 4 |

5. **Lens > Field Of View** = 60.

**Why:** ThirdPersonController owns the mouse, just like first person. `HandleLook` rotates CameraTarget,
and Third Person Follow keeps the camera behind CameraTarget, at the shoulder, facing the same way.
CameraTarget's rotation is set in **world** space, so it ignores the Player turning toward its move direction.
ThirdPersonController reads the camera's direction, so W always moves away from the camera.

Tweak tips: **Camera Distance** = how far behind, **Shoulder Offset X** = over-the-shoulder amount
(0 = centered), **Vertical Arm Length** = how high. Mouse speed is **Mouse Sensitivity** on ThirdPersonController.

---

## Part 4 - Raycasting and the Interaction System

### 4.1 The scripts (Assets/Scripts/Interaction)

| Script | What it does |
|---|---|
| `IInteractable` | A contract: any usable object has a `PromptText` and an `Interact()` method |
| `PlayerInteractor` | Casts a ray from the camera every frame. E calls `Interact()` on the hit object |
| `ColorBox` | E switches to the next color in a list |
| `PickupBox` | E attaches the box to HoldPoint. E again drops it |

How `PlayerInteractor` works, in order, every frame:

1. **FindInteractable:** `Physics.Raycast` shoots an invisible line from the camera forward, up to
   `interactDistance`. If it hits a collider, `GetComponent<IInteractable>()` asks "can this be used?".
   `Debug.DrawRay` shows the ray in the Scene view (green = hit, red = no hit).
2. **HandleInteractInput:** if E was pressed: drop the held box if there is one, otherwise call
   `Interact()` on the object under the crosshair.
3. **UpdatePrompt:** shows "Press E to drop", the object's own prompt, or nothing.

**Why an interface:** PlayerInteractor does not know or care what kind of object it hit. To add a door or a
light switch later, write one new script that implements `IInteractable`. PlayerInteractor never changes.

### 4.2 Player setup (both scenes)

1. Select `Player` > set **Layer** = `Ignore Raycast` > choose **No, this object only**.
   Without this, the ray in third person would hit the player's own capsule first.
2. Create the hold point:
   - FirstPerson: right-click `CameraRoot` > Create Empty > `HoldPoint`, position `0, -0.3, 1.2`.
   - ThirdPerson: right-click `Player` > Create Empty > `HoldPoint`, position `0, 1.2, 1`.
3. Add `PlayerInteractor` to `Player`:

| Field | FirstPerson | ThirdPerson |
|---|---|---|
| Camera Transform | Main Camera | Main Camera |
| Hold Point | HoldPoint | HoldPoint |
| Interact Distance | 3 | 8 (camera is about 4 units behind the player) |
| Prompt Text | PromptText (Part 5) | PromptText (Part 5) |

### 4.3 Boxes (both scenes)

Color boxes - **GameObject > 3D Object > Cube** three times:

| Name | Position | Scale |
|---|---|---|
| ColorBox_1 | -3, 0.5, 4 | 1 |
| ColorBox_2 | 0, 0.5, 4 | 1 |
| ColorBox_3 | 3, 0.5, 4 | 1 |

Add `ColorBox` to each.

Pickup boxes - **GameObject > 3D Object > Cube** two times:

| Name | Position | Scale |
|---|---|---|
| PickupBox_1 | -1.5, 0.25, 2.5 | 0.5 |
| PickupBox_2 | 1.5, 0.25, 2.5 | 0.5 |

Add **Rigidbody** (Mass 1, Use Gravity on) and `PickupBox` to each.

**Why the pickup works:** while carried, the Rigidbody is set to **kinematic** (physics stops moving it) and
its collider is turned off so it does not block the player or the ray. Dropping turns both back on, so the
box falls with gravity. This is where Rigidbody physics meets the Character Controller player.

---

## Part 5 - HUD (Prompt Text and Crosshair)

Do this in both scenes.

1. **GameObject > UI > Text - TextMeshPro**. This also creates a `Canvas` (rename it `HUDCanvas`).
   Delete the `EventSystem` if you do not need clickable UI.
2. On `HUDCanvas` > **Canvas Scaler**: UI Scale Mode = `Scale With Screen Size`,
   Reference Resolution = `1920 x 1080`, Match = `0.5`.
3. Rename the text to `PromptText`:
   - Anchor preset: **bottom center**. Pos Y = `200`. Width `800`, Height `80`.
   - Text: empty. Font Size `40`. Alignment: center and middle. Color white.
   - Turn off **Raycast Target**.
4. **GameObject > UI > Image**, name it `Crosshair`, anchor **middle center**, Width `8`, Height `8`,
   Raycast Target off.
5. Drag `PromptText` into **Prompt Text** on `Player > PlayerInteractor`.

**Why:** A Screen Space - Overlay canvas is drawn on top of the camera image, so the HUD needs no camera.
Anchors decide which part of the screen each element sticks to. The crosshair shows where the ray points.

---

## Part 6 - Test Checklist

FirstPerson scene:
- [ ] WASD moves relative to where you look
- [ ] Mouse turns left/right and looks up/down, but not past straight up or down
- [ ] Space jumps only when standing on the ground, and the player falls back down
- [ ] Looking at a big box shows "Press E to change color", E changes its color
- [ ] Looking at a small box shows "Press E to pick up", E picks it up, E again drops it
- [ ] The ray is visible in the Scene view while playing

ThirdPerson scene:
- [ ] Mouse orbits the camera around the player, and up/down stops at the limits
- [ ] W moves away from the camera, and the dummy turns to face the direction it moves
- [ ] Space jumps, gravity pulls the player back down
- [ ] Interaction works the same as in first person

Console: no errors or warnings.

---

## Troubleshooting

| Problem | Fix |
|---|---|
| Player does not move | Check Project-wide Actions is `InputSystem_Actions` and the actions are named Move, Look, Jump, Interact |
| Third-person mouse does nothing | ThirdPersonController > Camera Target must be `CameraTarget`, and ThirdPersonCamera must track it |
| Player sinks or floats | Adjust Character Controller Center and Height to fit the model |
| Camera sees inside the body (FP) | HumanDummy Skinned Mesh Renderer > Cast Shadows = Shadows Only |
| Ray hits the player (TP) | Player layer must be Ignore Raycast |
| Prompt text pink or missing | PromptText > Font Asset = `LiberationSans SDF` (import TMP Essentials first) |
| Changes disappear after editing files outside Unity | Reload the scene in Unity, do not save the old open copy over it |

---

## Learn More

- Unity Manual: Character Controller component
- Input System: Project-Wide Actions, Workflows
- Cinemachine 3: Cinemachine Brain, Cinemachine Camera, Third Person Follow
- Unity Scripting API: Physics.Raycast, Rigidbody.isKinematic, Debug.DrawRay
- Unity Manual: Canvas, Basic Layout (anchors), Canvas Scaler
- C# docs: Interfaces
