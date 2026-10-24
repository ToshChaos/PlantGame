# T13 — Island assembler from kit, seeded

**Owner:** A5 Cartographer · **Depends on:** none to start (placeholders); swaps in T10 kit, T12 soil/pedestal/Heart Tree and T29 water pieces as they land
**Status:** DONE

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

## Placeholders (start before T10 / T12 / T29)
- `ScriptableObjects/World/KitCatalog` maps each piece role (Terrain flat/edge/corner/inner-corner/slope/underside, Ruin, Veg,
  Prop, SoilPatch, SeedPedestal, HeartTree, Lake, Waterfall) to a prefab. Fill it first with placeholder prefabs (Unity primitives
  scaled to the 4 m grid, pivot bottom centre, palette materials, names `Kit_<Category>_<Name>_00`), so the builder only ever
  talks to the catalog. When T10 / T12 / T29 deliver, re-point the catalog entries; the builder code does not change.
- Socket placement, determinism and the 50-seed test are done on placeholders; visual variety is judged again once real kit lands.

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

## Already in place (sandbox)
- `Editor/RoundIsland.cs` builds a rounded floating island (flat top disc, wobbled outline, tapered rocky underside, world-scale UVs, MeshCollider, layer Ground) used by `Sandbox_PlantTest`. Use it as the placeholder island shape (user request: islands should be round, not boxes); radius 8 m, soil / water / seed sockets must stay inside radius - 0.5 m.

## Layout (sandbox)
- `Sandbox_PlantTest` islands no longer sit in a line: each station has a heading (`Headings` = 0, 38, -32, 20 degrees) and the plant, soil patch and next island follow it (local forward / side offsets). T13/T14 should do the same (island graph with turns, never overlapping: centre distance >= 2 x radius + 1 m, also in plan view when heights differ).

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
