# T14 — World generator: island graph, gaps, solvability

**Owner:** A5 Cartographer · **Depends on:** T13, T11, T27, T29
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Place islands in the sky and connect them so the level is always beatable using the right plants — deterministic from the seed.

## Deliverables
- **Start island has 2–3 `SeedTree`s (user request)** that drop the first seeds at level start; the validator counts them as the starting seed supply (the player must never need a seed that is not on the Start island or reachable before the first gap).
- `WorldGenerator.Generate(seed)` per contract; takes `RunConfig` (islandCount, allowedKinds) via `Generate(RunConfig)` overload
- Island graph: Start → ... → Goal path with 0–2 optional side islands (reward/seed stash); layout via seeded random walk in 3D that respects gap envelopes
- Each link gets a **plant kind** from `allowedKinds` and positions that fit section 5.2 (bridge gap 8–14 m, ladder rise 4–9 m, platform 3–6 m / 2–4 m); soil socket on source island, land socket on target
- **Seed supply:** for every link, a matching seed pickup is placed on the source island or an earlier island; inventory limit respected (never need more than 4 carried at once); optional decoy seeds
- **Water plan:** Start island has a water source; mark ~40% of other islands as water islands; add Dew Seed supply where the 'max 2 water-needing links between access points' rule in `AGENTS.md` 5.2b would break; values from `WaterSettings`
- **Solvability check (with water):** simulated traversal (graph + envelopes) asserting each link is crossable; regenerate with salt++ if validation fails
- Lighting/skybox anchor, world bounds, kill plane height, per-island `Spawn` for respawn (checkpoint = last visited island)
- Difficulty curve from islandCount (4 easy → 8 hard), decoys and ladder/platform mix
- `WorldGenerator.Validate()` editor menu running N seeds with a report; difficulty parameters in `ScriptableObjects/World/GeneratorSettings`

## Steps
1. Hard-code a 3-island line first (bridge, ladder), verify with the real player.
2. Add graph randomisation and the validator.
3. Run 200 seeds in batch; log failures and fix the generator, never the validator.
4. Output the seed supply plan as a debug string (what seed sits where) for T22.

## Acceptance criteria
- [ ] 200/200 seeds validate (including the water budget: charges, refills, collector wait, at most one backtrack); 20 random seeds actually completed or auto-solved in Play mode via a debug `AutoSolve` (teleport + plant)
- [ ] Same RunConfig → same world (hash of all island transforms)
- [ ] No gap is crossable without a plant; every gap has a seed of the right kind available beforehand
- [ ] Generation under 1 s for 8 islands

## Notes
- Use `System.Random` only; keep generator version in `SeedCode.GeneratorVersion` and bump on changes.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_

## Requests / notes (day 3)
- Add `WaterMode { Sources, CollectorOnly }` to `LevelDefinition` / `RunConfig`. **CollectorOnly** levels have no `WaterSource` at all: the Start island gets a soil patch + Dew Seed so the Water Collector (1 charge per 20 s, cap 1) is the only water; the validator must count the 20 s waits and the walk back to the first island. **Sources** levels (Level 1 style): water on the Start island and on some others, at most 2 water-needing links between water islands, features are ponds or streams with one interact point. `SandboxPlantTestBuilder` is the hand-built reference.

## Handoff (day 3)
- `Scripts/World`: `LevelPlanner` (hand plan for level_5, `Generate` chain with optional fixed `sequence`, Glider gaps, round trip), `LevelLayout`, `WorldValidator` (menu 15: 200 seeds), `WorldGenerator`. Side islands, alternative soil spots and overlap-free layout: see T34. Not done: Dew-seed supply for Sources levels, `AutoSolve` playthrough of 20 seeds, difficulty ScriptableObject.
