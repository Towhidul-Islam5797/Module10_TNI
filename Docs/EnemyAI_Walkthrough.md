# Enemy AI Walkthrough (Patrol and Chase)

How to build the enemy in the FirstPerson or ThirdPerson scene by hand.
Do every step once per scene.

Result: a red dummy walks between 4 points. When it sees the player it chases them.
When the player gets far enough away, it goes back to patrolling.

---

## Step 1 - The Enemy object

1. **GameObject > Create Empty**, name it `Enemy`, position `0, 0, 14`, rotation Y `180`
   (so it faces the player).
2. **Add Component > Nav Mesh Agent**:
   - Radius `0.3`, Height `1.8`
   - Speed `2`, Angular Speed `360`, Stopping Distance `0`
3. Drag `Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M Red` onto `Enemy`
   so it becomes a child. Rename it `EnemyDummy`, position `0, 0, 0`.

**Why:** `Enemy` holds the logic, `EnemyDummy` is only the visual (same idea as the Player).
The **NavMeshAgent** is Unity's built-in "walk to a point and avoid obstacles" component.

---

## Step 2 - Patrol points

1. **GameObject > Create Empty**, name it `PatrolPoints`, position `0, 0, 0`.
2. Add 4 empty children:

| Name | Position |
|---|---|
| PatrolPoint_1 | -8, 0, 8 |
| PatrolPoint_2 | 8, 0, 8 |
| PatrolPoint_3 | 8, 0, 20 |
| PatrolPoint_4 | -8, 0, 20 |

**Why:** the enemy walks to point 1, then 2, 3, 4, and back to 1.

---

## Step 3 - Bake the NavMesh

1. **GameObject > Create Empty**, name it `Navigation`.
2. **Add Component > NavMesh Surface**:
   - Collect Objects = `Volume`, Size `60, 10, 60`, Center `0, 2, 10`
   - Include Layers: everything **except** `Ignore Raycast` (so the Player is not baked as a wall)
   - Use Geometry = `Physics Colliders`
3. Click **Bake**. A blue area appears on the floor, with holes around the boxes.
4. Save the scene.

**Why:** a NavMesh is a map of where agents can walk. Without it, the NavMeshAgent cannot move.
The Volume keeps the bake small. Baking the whole 10,000-unit plane would be very slow.

---

## Step 4 - The Enemy Animator

1. **Project > Assets/Animations > right-click > Create > Animator Controller**, name it `EnemyAnimator`.
2. Open it. Add a **Float** parameter `Speed`.
3. Right-click the grid > **Create State > From New Blend Tree**, rename the state `Locomotion`.
   It becomes the default (orange) state.
4. Double-click it, select the blend node:
   - Parameter = `Speed`
   - Untick **Automate Thresholds**
   - Add 3 motion fields:

| Motion | Threshold | Time Scale |
|---|---|---|
| Idle | 0 | 1 |
| Walking | 2 | 1 |
| Walking | 4 | 2 |

5. Select `EnemyDummy`, drag `EnemyAnimator` into the Animator's **Controller** field,
   and untick **Apply Root Motion**.

**Why:** the agent's speed is 0 when standing, 2 while patrolling and 4 while chasing.
The blend tree plays the matching animation. Time Scale 2 makes the walk play twice as fast
for chasing (swap in a Run clip later if you have one).

---

## Step 5 - The EnemyAI script

1. Add `Assets/Scripts/Enemy/EnemyAI.cs` to `Enemy`.
2. Fill in the fields:

| Field | Value |
|---|---|
| Player | `Player` (drag from the Hierarchy) |
| Player Chest Height | 1.2 |
| Patrol Points | size 4, drag PatrolPoint_1 to _4 in order |
| Patrol Speed | 2 |
| Chase Speed | 4 |
| Chase Stop Distance | 1.5 |
| Sight Range | 10 |
| Lose Sight Range | 15 |
| View Angle | 90 |
| Eye Height | 1.6 |

3. Save the scene.

### How the script works

`Update` runs three steps every frame:

```
UpdateState()      decide: Patrol or Chase?
Patrol() / Chase() do the current job
UpdateAnimation()  send the agent's speed to the Animator
```

- **UpdateState:** Patrol switches to Chase when `CanSeePlayer()` is true.
  Chase switches back to Patrol when the player is further than `loseSightRange`.
  Lose Sight Range is bigger than Sight Range, so the enemy does not flicker between states.
- **CanSeePlayer** checks three things in order:
  1. **Distance:** is the player within `sightRange`?
  2. **Angle:** is the player inside the view cone? (`Vector3.Angle` against `viewAngle / 2`)
  3. **Line of sight:** `Physics.Linecast` from the enemy's eyes to the player's chest.
     If it hits a box, the player is hidden. The Player is on Ignore Raycast, so only walls block it.
- **Patrol:** when the agent is within 0.5 of its point, go to the next one.
  `% patrolPoints.Length` wraps back to the first point after the last.
- **Chase:** `agent.SetDestination(player.position)` every frame at chase speed,
  stopping 1.5 units away.
- **UpdateAnimation:** `animator.SetFloat("Speed", agent.velocity.magnitude)` with a small damp time
  so the blend is smooth.

---

## Step 6 - Test

Press Play and check:

- [ ] The enemy walks the square and avoids the boxes
- [ ] Walk in front of it (within 10 units): it chases you, playing the faster walk
- [ ] Hide behind a box: it does not see you (red line in the Scene view)
- [ ] Run more than 15 units away: it goes back to patrolling
- [ ] Select `Enemy` in the Scene view: yellow circle = sight range, gray circle = lose sight range
- [ ] No errors in the Console

---

## Troubleshooting

| Problem | Fix |
|---|---|
| "SetDestination can only be called on an active agent that has been placed on a NavMesh" | Bake the NavMesh (Step 3) and make sure the Enemy stands inside the blue area |
| Enemy never chases | Check Player is assigned, and that you are in front of it (view angle) and within 10 units |
| Enemy sees through boxes | The boxes need colliders. Re-bake if you moved them |
| Enemy slides without animating | EnemyDummy's Animator needs `EnemyAnimator` and Apply Root Motion off |
| Enemy walks around a box that is not there anymore | You moved a pickup box after baking. Re-bake, or add a **NavMesh Obstacle** to pickup boxes |

---

## Learn more

- Unity Manual: NavMesh Agent, NavMesh Surface
- Scripting API: NavMeshAgent.SetDestination, Physics.Linecast, Vector3.Angle
- Unity Manual: Blend Trees
