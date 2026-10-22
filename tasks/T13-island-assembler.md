# T13 — Island assembler from kit, seeded

**Owner:** A5 Cartographer · **Depends on:** T10, T12, T29 · **Estimate:** 4h · **Day:** 2
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Given a seed and a size, build one good-looking island from kit pieces with all required sockets.

## Deliverables
- `Scripts/World/IslandBuilder.cs`: footprint generator (blob on grid via noise/cellular growth), terrain tile selection (flat/edge/corner/inner-corner by neighbours), underside rock chunks, height variation
- Prop scatter (Poisson-disc, seeded): ruins clusters, trees, bushes, flowers, crystals; avoid sockets and a 2 m clearance around them
- Socket placement (including `Socket_Water`: islands flagged as water islands get a Lake or Waterfall from T29; the Start island always does): `Spawn`, 1–3 `Socket_Soil` on edges facing outward, `Socket_Land` on edges, `Socket_Item` spots
- Island sizes S/M/L (3x3 → 6x6 tiles); the start and goal islands are special (spawn pad / Heart Tree + altar ring)
- Build is deterministic: `IslandBuilder.Build(seed, size, role)`
- Mesh/Collider combine optional; keep draw calls low via static batching or GPU instancing
- `Sandbox_World.unity` with a button/inspector to build 9 seeds in a grid for visual QA; screenshot in `docs/shots/`

## Steps
1. Start with flat rectangular islands; add edge/corner logic, then underside, then props.
2. Provide a debug gizmo view for sockets.
3. Test 50 seeds: no overlapping props, all sockets reachable on foot (flood-fill on tiles).

## Acceptance criteria
- [ ] Same seed → identical island (compare transform hash)
- [ ] Islands look varied across seeds and stay in-palette
- [ ] Every island has a reachable `Spawn`, >= 1 `Socket_Soil`, >= 1 `Socket_Land`
- [ ] Build time per island < 100 ms (L size)

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
