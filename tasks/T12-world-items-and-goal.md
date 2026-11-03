# T12 — Seed pickups, soil patch prefab, Heart Tree goal

**Owner:** A1 Gameplay · **Depends on:** T05, T11
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
The playable world objects: seed pickups, planting spots and the goal.

## Deliverables
- **Seed trees (user request):** `Scripts/Inventory/SeedTree.cs` exists (T04/T05 pass): a tree that drops its seeds at level start. T12 makes the prefab (`Prefabs/Items/SeedTree`, Blender tree from the kit when T10 lands) and T15 calls `Drop(worldSeed)` on `RunStarted` (set `dropOnStart = false`).
- `Prefabs/Items/Pickup_<Seed>.prefab` (Vine, Beanstalk, Shelf, Dew) (use `WorldItem`; glowing, bobbing, palette-coloured per plant kind)
- `Prefabs/Island/SoilPatch.prefab` (from `Prop_SoilPatch` + `SoilPatch` component + prompt 'Plant seed')
- `Prefabs/Island/HeartTree.prefab`: trigger zone; on enter raises `GameWon` via `RunSession.Win()` (T25 provides; use `GameEvents` until then)
- Optional `SeedPedestal` that respawns a seed when the player drops back (prevents soft-locks)
- Sandbox test level: two islands, one gap, seed + soil + goal, fully hand-placed

## Steps
1. Build with placeholder meshes if kit art isn't ready; swap later.
2. Add the soft-lock safety: if the player holds no usable seed and has no seed in reach, pedestals regenerate seeds.
3. Test the complete loop in `Sandbox_Gameplay`.

## Acceptance criteria
- [ ] Hand-built mini level can be won end-to-end: pick up seed, plant at gap, cross, reach Heart Tree
- [ ] No soft-lock: dropping a seed off the island is recoverable
- [ ] Prompts show 'Pick up <name> (Q)' and 'Plant <seed> (E)'

## Requests (to/from other roles)
_none yet_

## Already in place (sandbox)
- `SeedTree` (Inventory) now also regrows: `regrowSeconds` / `maxAlive` drop a new seed every N s while fewer than `maxAlive` of its seeds lie around (0 = drop once, as before). `Sandbox_PlantTest` has a seed tree (vine) and seed patches (beanstalk, shelf) per island, built by `SandboxPlantTestBuilder.SeedSource` (placeholder primitives). World gen (T14) should place these instead of static seeds; decide whether regrow stays on in the real game (infinite seeds) or is limited per level.

## Goal flag (sandbox, done)
- `Core/GoalFlag` (IInteractable, E, prompt "Raise the flag"): only usable while the run is Running; raises the cloth and calls `RunSession.Win()`. `SandboxPlantTestBuilder.BuildGoal` places it (stone base, gold pole, cyan cloth) on the last island. T12 still owes the real Heart Tree + Heart Seed flow; reuse `GoalFlag`'s `Interact` -> `RunSession.Win()` contract.

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
