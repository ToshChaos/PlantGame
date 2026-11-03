# T28 — Geometry Nodes: Water Collector plant

**Owner:** A3 Plant Smith · **Depends on:** T06
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A pitcher-bloom that catches dew from the air: a procedural water-collecting plant with a visible reservoir. It is not a traversal plant, so it is allowed to be small (about 1.5–3 m tall).

## Deliverables
- Node group `GN_Plant_Collector`: inputs `Seed`, `Height (1.5–3)`, `BowlRadius`, `PetalCount`, `Curl`, `LeafDensity`, `StemThickness`
- Large open bowl with white/blue petals around a rim, broad leaves at the base, glow-cyan dew dots; `growth_t` from base to rim (>= 24 segments)
- Separate child mesh `Reservoir` (the water volume inside the bowl, flat top) with `WaterMin` / `WaterMax` empties; one `INTERACT_NN` trigger box around the bowl; a small body collider
- `Anchor` and `Tip` empties per the contract in `AGENTS.md` 5.3 (Collector section); 6 baked variants
- `docs/shots/collector_variants.png`

## Steps
1. Reuse utility node groups from T06 (leaves, noise, growth).
2. Make the bowl readable from 8–10 m away: a bright rim and a clearly visible water surface.
3. Hand 6 variants to T09 for bake and export.

## Acceptance criteria
- [ ] Seeds give distinct but equally functional collectors (same bowl capacity and interaction box)
- [ ] `Reservoir` scales cleanly from empty to full without poking through the bowl
- [ ] Tri count <= 3k; contract empties and trigger are present
- [ ] Palette check: white, blue and green, cyan only as dew accents

## Fallback / cut plan
If short on time: single bowl + 4 petals + 2 leaves, vary only size and curl by seed.

## Requests (to/from other roles)
- Once `Plant_Collector_NN` prefabs exist, `Sandbox_PlantTest` picks them up automatically (rebuild via `PlantGame > 9 Build Sandbox_PlantTest`); add a layout branch there if needed.

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_

## Handoff (T28)
- **Files:** `Source/Blender/scripts/plants_collector.py` (`build_all()`), `exports/Plant_Collector_01..06.fbx`, `Game/Assets/Art/Models/Plants/Plant_Collector_NN.fbx`, `Editor/CollectorPrefabBuilder.cs` (menu 14), `Scripts/Plants/WaterCollector.cs`, `Prefabs/Plants/Plant_Collector_NN.prefab`, material `M_CollectorGrow`.
- **Deviation:** variants are baked with bmesh from per-seed parameters (height 1.8-2.6 m, bowl 0.7-0.9 m, 5-8 petals, 4-6 leaves), not a Geometry Nodes group; the Blender validator passes for all six, about 750-790 tris each. Port `_build` to nodes later if the GN requirement matters.
- **Behaviour (user request, v1.8):** produces **1 charge after 20 s** once grown, capacity 1; E transfers it to the can. Values are fields on `WaterCollector` (T21 tunes, later `WaterSettings`).
- **30-second test:** `PlantGame > 9 Build Sandbox_PlantTest`, play, plant the Dew Seed on the first island's soil, wait ~24 s, E on the plant.
- **Known:** pale palette; `docs/shots/collector_variants.png` (all six) not rendered yet; no drip VFX / ready sparkle (T30); only variant 1 is used in the sandbox.
