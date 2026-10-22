# T06 — Geometry Nodes: Vine bridge plant

**Owner:** A3 Plant Smith · **Depends on:** T00 (T02 for export) · **Estimate:** 4h · **Day:** 1
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A Geometry Nodes asset that generates a walkable vine bridge from a seed and parameters. This is the hero 'procedural showcase' asset.

## Deliverables
- `Source/Blender/plants.blend` with a node group `GN_Plant_Bridge` driven by inputs: `Seed`, `Length (8–14)`, `Arc`, `Width (>=1.2)`, `Thickness`, `Curl`, `LeafDensity`, `Droop`
- Procedural construction: spline path (curve) with arc + noise, braided vines (2–4 strands) + flat walkable deck of woven leaves/planks, side rails of thin vines, leaves and small white/blue blossoms
- Attribute `growth_t` (0 at base → 1 at tip) written to a vertex colour; >= 24 segments along the length
- Empties `Anchor` (base), `Tip` (end), `Land`; collision proxy boxes along the deck `COL_NN_idx` generated from the same path
- Shared material slot assignments compatible with `M_PlantGrow`; palette-aware vertex colours
- Screenshots of 6 seeds side by side in `docs/shots/bridge_variants.png`

## Steps
1. Build the curve + deck first (walkable silhouette), then add strands, rails, leaves, blossoms.
2. Expose few, meaningful inputs; hide internals in nested node groups for reuse by T07/T08 (`GN_Util_Growth`, `GN_Util_Leaves`, `GN_Util_Noise`).
3. Generate `growth_t` using curve parameter; verify in Vertex Paint view via screenshot.
4. Test for 6 seeds: bridges differ visibly but all keep deck width >= 1.2 m and length in range.
5. Save scripts used (via MCP) in `Source/Blender/scripts/plants_bridge.py` so the setup is reproducible.

## Acceptance criteria
- [ ] Changing `Seed` changes the look; deck stays walkable (continuous, width >= 1.2 m)
- [ ] `growth_t` runs 0→1 monotonically along the length
- [ ] Tri count <= 4k per variant
- [ ] Anchor/Tip/COL proxies generated and named per contract
- [ ] Visual check done with screenshots; palette is white/green/blue

## Notes
- Use Blender MCP `execute_blender_code` to build node trees by script; take viewport screenshots often.
- Keep to simple nodes (Curve to Mesh, Instance on Points, noise) — no simulation zones needed.

## Fallback / cut plan
If the full braided look takes too long, ship deck + two vines + leaf cards and move on; polish in T21.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
