# T12 — Seed pickups, soil patch prefab, Heart Tree goal

**Owner:** A1 Gameplay · **Depends on:** T05, T11 · **Estimate:** 2h · **Day:** 2
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
The playable world objects: seed pickups, planting spots and the goal.

## Deliverables
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

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
