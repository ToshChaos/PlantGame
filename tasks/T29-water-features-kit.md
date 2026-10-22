# T29 — Water features kit: waterfall + lake

**Owner:** A4 Stonemason · **Depends on:** T10 · **Estimate:** 3h · **Day:** 2
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
The two water sources that make refilling visible and attractive. The art direction wants these to be landmarks you can spot from the next island.

## Deliverables
- `Kit_Water_Waterfall_01..02`: a cliff-edge piece on the 4 m grid that spills a sheet of water off the island edge (white-blue ribbon mesh with UVs along the flow, foam strip at the lip) and a small catch basin where the player stands to refill
- `Kit_Water_Lake_01..02`: a recessed 2x2 tile pond with a stone/moss shore and a flat water surface mesh 0.3 m below the rim; shallow (walkable), collider as a trigger on layer `Water`
- Mist/splash anchor empties `Splash_Point`, trigger volume `WATER_TRIGGER` (about 2 m reach) for the `WaterSource` interaction
- Both with `Socket_Water`-ready pivots so T13 can place them; reproducible in `Source/Blender/scripts/build_kit.py` (extend T10's script)
- Preview render `docs/shots/water_features.png`

## Steps
1. Start from the T10 terrain edge tile so shapes match.
2. Keep surfaces as simple meshes; flow and ripples come from shaders (T30).
3. Export through T02 scripts and sync.

## Acceptance criteria
- [ ] Both pieces snap to the 4 m grid and sit flush with neighbouring terrain
- [ ] Waterfall visibly falls into the void; lake reads clearly from 12 m
- [ ] `WATER_TRIGGER` and `Splash_Point` are present and correctly named
- [ ] Tri count: waterfall <= 600, lake <= 500

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
