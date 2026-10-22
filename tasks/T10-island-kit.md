# T10 — Modular island kit (terrain, ruins, vegetation)

**Owner:** A4 Stonemason · **Depends on:** T02 · **Estimate:** 6h · **Day:** 1–2
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A cohesive set of modular pieces, assembled by code into varied floating islands. Use Geometry Nodes where it pays off (rock underside, grass tufts, scatter).

## Deliverables
- `Source/Blender/kit.blend` with a collection per category; pieces named `Kit_<Category>_<Name>_<NN>` on a 4 m grid, pivot bottom-centre
- **Terrain:** flat, edge, corner, inner-corner, slope, 3 underside rock chunks (hanging stalactite-style) — grass top in green, white-stone cliffs, blue crystal veins
- **Ruins:** arch, pillar (2 heights), wall, broken wall, stairs, altar, ring portal — white stone with moss
- **Vegetation:** 3 tree types, bush, flower patch, grass tuft, glowing mushrooms, hanging vines
- **Props:** rock x3, crystal cluster (cyan glow), `Prop_SoilPatch` (dark soil + small sprout ring), `Prop_SeedPedestal`, `Prop_HeartTree` (goal, large)
- Simple convex colliders for Terrain; `COL_` boxes for Ruins; no collision for Veg
- A kit overview render `docs/shots/kit_overview.png` and a 'sample island' hand-built in Blender for style check
- Tri budget: terrain tile <= 500, ruin <= 800, trees <= 1.5k, whole sample island <= 30k

## Steps
1. Block out Terrain pieces first (needed by T13); export and hand off early (Day 1 evening).
2. Add Ruins, then Props (SoilPatch and HeartTree are required by T12), then Vegetation.
3. Use vertex colours for palette; keep UV-free if possible.
4. Assemble a sample island in Blender and check the style against the art bible (T01).
5. Write `Source/Blender/scripts/build_kit.py` (`build_all()`) so the whole kit is reproducible, plus a Unity menu `PlantGame > Build Kit Prefabs`. Export through T02 scripts; verify the sockets (`Socket_*` empties) exist on the sample island prefab template.

## Acceptance criteria
- [ ] All required pieces exist with the exact naming and grid
- [ ] Terrain tiles snap without gaps or overlaps (4 m grid)
- [ ] A sample island looks coherent: white/green/blue, floating, readable silhouette
- [ ] Exports import in Unity with colliders and pivots correct
- [ ] `Prop_SoilPatch` and `Prop_HeartTree` delivered by end of Day 1 or Day 2 AM

## Notes
- Prioritise Terrain + SoilPatch + HeartTree + 2 tree types + 3 ruins; everything else is variety.
- Scatter with Geometry Nodes only for grass/flowers (instances), kit pieces are plain meshes.

## Fallback / cut plan
Cut order: ruin variety, extra tree types, hanging vines.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
