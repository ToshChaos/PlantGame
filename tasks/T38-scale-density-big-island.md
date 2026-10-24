# T38 — Bigger trees, more rocks and trees, one huge island

**Owner:** A5 · **Depends on:** T13, T14, T36
**Status:** DONE

## Goal
- **All imported trees are 2x bigger** (Quaternius oaks, conifers, Kenney trees). The T36 big trees on wet islands stay a step above the new normal size (re-tune so they remain visibly larger). Check trunk colliders, soil clearance, seed-tree spacing and ladder/bridge landing spots after the scale change.
- **More rocks and more trees** on every ground type (bare islands get more boulders, not just fewer trees).
- **At least one level has an even bigger island** (curated level; e.g. a hub or the goal island, radius well above today's max). Check the layout spacing rule (`MinIslandGap`), the validator and the performance of the larger terrain mesh.

## Acceptance
- Trees read as 2x in a screenshot next to the 1.8 m player; nothing blocks soil, items, goal or Spawn.
- Validate Seeds still 0 invalid.
- A curated level visibly has the biggest island; screenshot in `docs/shots/`.

## Handoff
Normal trees now 4.8-8.8 m (was 2.4-4.4), big wet-island trees 12-15 m with wider clearances (`Scenery.Decorate`). Boulders on every island (grass 3, mud 4, rock 9, plus a few by area), vegetation on mud/rock raised to 0.45/0.2 (`Ground`). `LevelPlan.bigIsland` + `LevelLayout`: level_5 island 3 has radius ~20 m (about 1.9x). Validate Seeds: 0 invalid of 200. Screenshot `docs/shots/T38_level5_overview.png`. Not checked: soil/ladder clearance next to the new big trees, frame time.
