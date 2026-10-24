# T45 — Snow, frozen water and the Heat Seed (extra / stretch)

**Owner:** A5 + A2 + A6 + A1 · **Depends on:** T14, T27, T33, T35
**Status:** DONE

## Goal
A new obstacle layer on top of the water/plant loop. Cuttable: not part of the MVP.

- **Snow ground:** a snow texture/ground type for some islands (extends T35 ground types; snow can cover all or part of an island, e.g. a patch around soil or water). Snow theme or per-island flag, seeded; the start island is never snowy.
- **Frozen soil:** a soil patch on snow-covered ground is **inactive** until unfrozen: no prompt, `CanUseItem` false, "frozen" feedback (`UseRejected`), visibly iced over.
- **Frozen water:** a lake, waterfall or Water Collector in snow/ice cannot be used for refill (F) until unfrozen; shows an ice surface and a "frozen" feedback.
- **Heat Seed:** new seed item (bright yellow / orange glowing fruit; world prefab, hotbar icon, seed-tree variant like T33). Plant/use it (E) near snow to **melt snow in an area** (radius ~4-6 m, animated melt of the texture/ice, particles + glow light); frozen soil and water inside the area become active. Decide in implementation: consumed on use (recommended) vs grows into a heat plant.
- **World gen (T14):** a snowy island always has a Heat Seed reachable first (on the island before, or the seed tree there), and the validator treats frozen soil/water as unusable until melted, so every level stays beatable. Bump `SeedCode.GeneratorVersion` if output changes.
- Audio: crunchy snow footsteps, melt hiss, ice crack.

## Acceptance
- Melt state is per area and deterministic from the seed (not persisted across runs).
- Validate Seeds 0 invalid; one curated level (or a Sandbox) demonstrates snow, frozen pond, Heat Seed melt.
- AGENTS.md contracts (items, 5.2b water rules, ground kinds, change log) updated.

## Handoff
Done (with T50): snow theme (white ground, snowfall, pale sky); frozen ponds (`WaterSource.frozen`, ice material, no refill and no refill hint while frozen); Heat Seed item (`heat_seed`, orange sphere pickup, hotbar disc) on a seed patch on the start island of snow levels, F next to frozen water with it selected melts that pond (seed used up) with a hint; up to two ponds are frozen per snow level. NOT done: frozen soil patches, melting snow in an area (radius, animation), snow footsteps, melt audio, validator awareness of frozen water (the start island always has liquid water and Heat Seeds regrow, so levels stay beatable), Heat Seed in the seed-tree variants, AGENTS.md contract text for the item.
