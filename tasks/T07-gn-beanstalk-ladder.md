# T07 — Geometry Nodes: Beanstalk ladder plant

**Owner:** A3 Plant Smith · **Depends on:** T06
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A climbable beanstalk generator: a thick twisting stalk with rungs/leaf-steps the player can climb, 4–9 m high.

## Deliverables
- Node group `GN_Plant_Ladder`: inputs `Seed`, `Height (4–9)`, `Twist`, `StalkRadius`, `RungSpacing (~0.5)`, `LeafSize`, `Sway`
- Stalk + two guide vines, rung leaves/knots every ~0.5 m, a top leaf platform to step off, `growth_t` along height (>= 24 segments)
- `Anchor`, `Tip`, `Land` empties; `CLIMB_idx` trigger boxes covering the climbable column; optional `COL_` for the top platform
- 6 seed screenshots `docs/shots/ladder_variants.png`

## Steps
1. Reuse utilities from T06.
2. Ensure there is clearance of >= 0.8 m in front of the stalk for the player.
3. Verify `growth_t`, empties and triggers; export test via T02 scripts.

## Acceptance criteria
- [x] Seeds produce distinct but equally climbable ladders
- [x] Height input maps to real metres within +/-0.2 m
- [x] Top platform gives a stable exit
- [x] Tri count <= 4k

## Requests (to/from other roles)
_none yet_

## Visual pass (user request)
- Thinner stalk (`StalkRadius` 0.10-0.16, was 0.16-0.30) and bean pods hanging on the back side: slim green pod (part .34) with three yellow beans (part ~.8, `_FlowerColor` in the shader), ~1.5 pods per metre of height, scale 1.3-1.9. Tris now 2.6-3.4k (budget 4k). Rungs, climb volume and pad are unchanged, so climbing still works (verified in play mode).
- Climb hint: `PlayerController.CurrentClimbHint` (Climb / Descend / Climbing) drives HUD text "Press [W] to climb", "Press [S] to climb down" and "[W] up [S] down [Space] jump off" (only when no other prompt is showing; independent of the Hints option). The key hint panel also lists "W / S climb beanstalks".

## Handoff
- **Files:** `Source/Blender/scripts/plants_ladder.py` (reuses helpers from `plants_bridge.py`), `plants.blend` (group `GN_Plant_Ladder`, source object `GN_Ladder_Source`, baked `Plant_Ladder_01..06`), `exports/Plant_Ladder_01..06.fbx` (not synced to Unity; T09/T11 do that), `docs/shots/ladder_variants.png`.
- **Use:** `exec(pipeline_common.py)` then `exec(plants_ladder.py); build_all()` rebuilds group, 6 variants and saves. Heights 4.5, 5.5, 6.5, 7.5, 8.2, 9.0.
- **Geometry:** grows along Blender +Z (Unity +Y), 1.9-2.1k tris. Climb face is Blender +Y (Unity +Z); leaves/blossoms sit on the back (-Y), so the front has >= 0.8 m free apart from 0.3 m rungs. Top leaf pad (r 1.0) is centred 1.9 m in front of the stalk top (near edge 0.9 m out so the climb line at 0.5 m stays clear), joined by a visual stem, top surface at z = Height. Platform top is exactly Height; leaf tips can poke ~0.2 m higher.
- **growth_t:** R = z / Height, G = part id (0 stalk, .34 vines, .67 rungs/leaves/pad, 1 blossoms), B = tint (same as bridge).
- **Empties/proxies:** `Anchor_NN` (origin), `Tip_NN` (0,0,H), `Land_NN` on the pad (0, 1.7, H), a single `CLIMB_NN_0` box (1.1 wide, z 0.05..2.6 in front, y 0..H+1.2: widened over the pad so S on the pad starts a descent), `COL_NN_0` pad collider.
- **Known issues:** Curve Radius did not drive Curve to Mesh in Blender 5.2, so the stalk taper uses the Scale input. Rung/leaf look is simple cards and bars. Not tested in Unity yet (climb volume tuning belongs to T11).

## Unity side (done)
- `Editor/LadderPrefabBuilder.cs`, menu `PlantGame > 8 Build Ladder Prefabs` -> `Prefabs/Plants/Plant_Ladder_01..06.prefab` (M_BridgeGrow, COL_ on layer Plant, CLIMB_0 trigger on layer Climbable + `ClimbableVolume` with land = Land and standOff putting the climb line 0.5 m out, `PlantInstance` 5 s).
- `PlantInstance` now enables `ClimbableVolume`s only once fully grown (sprouts are not climbable).
- Verified in play mode (temp scene, deleted): grown ladder 03, W near the base -> Climbing -> topped out onto Land (y 6.5) and stood on the pad collider.
- Not done: descent from the pad (S) and the ladder in the sandbox soil patch / a Beanstalk seed item (T11/T12).
- Test scene: `Sandbox_PlantTest` (menu `PlantGame > 9 Build Sandbox_PlantTest`) has the ladder island; automated climb + cross verified.
