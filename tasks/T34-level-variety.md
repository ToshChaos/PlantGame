# T34 — Level variety: alternative soil spots, side islands, no overlaps

**Owner:** A5 Cartographer · **Depends on:** T14
**Status:** DONE

## Goal
Levels allow more than one way to cross a gap, can have islands that are not needed to finish, and islands never overlap.

## Done
- **Alternative soil spots:** `LevelLayout.Alternatives` gives each gap 0-3 extra soil patches on the same island, a little along the rim from the preferred one (count grows with island radius; start island 2). Each aims at the preferred landing point and keeps about the same reach, so the same plant crosses. Clear of water, seeds, spawn, lanes. Named `Soil_<Kind>_<k>_alt<m>`; the preferred patch is unchanged.
- **Side islands:** `LevelPlan.mainLinks` / `goalIsland`; links after `mainLinks` lead to optional dead-end islands off a main island that has water. Random levels: 1 side island with 6-7 islands, 1-2 with 8. `RunConfig.sideIslands` / `LevelDefinition.sideIslands` add more to curated levels (levels 2 and 4 have 1). A side island has its own water and a bonus (parachute leaf or a spare seed). The validator checks them separately; the goal stays on the last main island.
- **No overlaps:** `LevelLayout.Compute` tries other headings (fanned out in 12 degree steps, up to 96) until a new island clears every earlier one except its parent: 3 m between outlines at similar height, 1.5 m within the underside's reach (8 m). Validate Seeds (menu 15): 0 invalid of 400, 1 re-roll.
- `SeedCode.GeneratorVersion` 2 (old random codes warn).

## Known issues / to check
- No real playthrough of the alternative spots or the side islands yet (T22).
- The hand-authored level_5 layout may shift where it used to overlap.
