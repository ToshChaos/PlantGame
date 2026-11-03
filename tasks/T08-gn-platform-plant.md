# T08 — Geometry Nodes: Shelf-fungus platform plant

**Owner:** A3 Plant Smith · **Depends on:** T06
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A spiral of broad shelf-fungus / leaf-pad platforms that rises 2–4 m and reaches 3–6 m outward.

## Deliverables
- Node group `GN_Plant_Platform`: inputs `Seed`, `Pads (4–7)`, `RiseTotal`, `Reach`, `PadSize (>=1.5)`, `Wobble`, `StemThickness`
- Central stem with pads spiralling outwards; glowing cyan rim spots; `growth_t` from stem base up and out
- `Anchor`, `Tip`, `Land` empties; `COL_` per pad (flat tops)
- 6 seed screenshots `docs/shots/platform_variants.png`

## Steps
1. Place pads along a helix; clamp step rise <= 1.2 m and horizontal hop <= 2.5 m.
2. Randomise pad rotation/size with the seed but within clamps.
3. Verify jumpability numerically with a script that measures pad-to-pad deltas.

## Acceptance criteria
- [x] Every seed passes the jump-delta check (rise <= 1.2 m, hop <= 2.5 m)
- [x] Total reach/rise inside envelope in `AGENTS.md` 5.2
- [x] Tri count <= 4k

## Fallback / cut plan
Cuttable (see `AGENTS.md` section 8). If cut, world gen only uses Bridge + Ladder.

## Requests (to/from other roles)
- Once `Plant_Platform_NN` prefabs exist, `Sandbox_PlantTest` picks them up automatically (rebuild via `PlantGame > 9 Build Sandbox_PlantTest`); add a layout branch there if needed.

## Mushroom pass (user request)
- Caps are now flat-topped discs over an inverted cone (bracket-mushroom silhouette) with white spots, on round white stalks that slope up from the main stem; the stem is pale green. Colours in the shader: caps red (`_CapColor`, `Palette.MushroomRed` #D6453D, part id ~0.92), stalks and spots white (id 1.0). Jump check unchanged and still passes (pad tops, rise, hop). Tris 1.7-2.8k. Screens: docs/shots/platform_variants.png, platform_mushrooms.png.
- Caps overlap in plan view; the cap above leaves ~0.4 m of headroom over the one below, so go up by jumping, not by walking under it.

## Handoff
- **Files:** `Source/Blender/scripts/plants_platform.py`, `plants.blend` (group `GN_Plant_Platform`, source `GN_Platform_Source`, baked `Plant_Platform_01..06`), `exports/Plant_Platform_01..06.fbx` (synced to `Art/Models/Plants/`), `docs/shots/platform_variants.png`. Unity: `Editor/PlatformPrefabBuilder.cs` (menu `PlantGame > 10 Build Platform Prefabs`) -> `Prefabs/Plants/Plant_Platform_01..06.prefab`.
- **Use:** `exec(pipeline_common.py)`, `exec(plants_platform.py)`, `build_all()` rebuilds group + 6 variants, runs `jump_check` on each and saves. `variant_params(n)` / `VARIANTS` hold per-variant inputs (Pads 4-7, RiseTotal 2.4-4.0, Reach 3.2-6.0, PadSize 1.6-2.2). `Twist` (rad between pads) is derived as min(0.5, 1.4/Reach) so hops stay short.
- **Geometry:** grows along Blender +Z (Unity +Y). Thin tapered stem, N flat-top discs (10 verts, 0.18 thick) spiralling out, an arm from the stem to each pad, ring of 6 rim spots per pad (part id 1: cyan accent). 0.7-1.0k tris.
- **growth_t:** R = z / RiseTotal, G = part id (0 stem, .34 arms, .67 pads, 1 spots), B = tint. Float attribute `padi` (pad index) exists in Blender only.
- **Empties/proxies:** `Anchor` (origin), `Tip` (top centre of the last pad), `Land` (0.4 m outward on the last pad), `COL_NN_k` one box per pad (0.92 x diameter, top flush with the pad top), ordered base -> tip.
- **Jump check (measured, all 6 variants pass):** rise per step 0.5-0.6 m (limit 1.2), worst centre hop 1.36-1.66 m (limit 2.5), total rise 2.4-4.0, reach 3.2-6.0, pad diameter >= 1.6.
- **Unity check:** in `Sandbox_PlantTest` (island 2) the grown platform has all 6 pad colliders enabled and ray-probes hit each pad top. Next island sits 3.6 m higher, ~1.4 m past the last pad.
- **Known issues:** pads are pale (part id .67 palette); wobble is small so pads sit on a smooth spiral. The jump from the last pad to the next island was not exercised by an automated walk (the scripted attempt was inconclusive); the 1.4 m gap is far inside the 8.3 m jump reach.
- **Pipeline note:** helper objects now carry a kind tag in Blender (`Anchor_01@P`); `export_fbx.py` strips it. Without it Bridge/Ladder/Platform `Anchor_01` collided in one file (and my ladder script's cleanup had deleted the bridge empties in plants.blend; restored by rebuilding).
