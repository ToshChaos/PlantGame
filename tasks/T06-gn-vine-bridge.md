# T06 — Geometry Nodes: Vine bridge plant

**Owner:** A3 Plant Smith · **Depends on:** T00 (T02 for export)
**Status:** DONE

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
- [x] Changing `Seed` changes the look; deck stays walkable (continuous, width >= 1.2 m)
- [x] `growth_t` runs 0→1 monotonically along the length
- [x] Tri count <= 4k per variant
- [x] Anchor/Tip/COL proxies generated and named per contract
- [x] Visual check done with screenshots; palette is white/green/blue

## Notes
- Use Blender MCP `execute_blender_code` to build node trees by script; take viewport screenshots often.
- Keep to simple nodes (Curve to Mesh, Instance on Points, noise) — no simulation zones needed.

## Fallback / cut plan
If the full braided look takes too long, ship deck + two vines + leaf cards and move on; polish in T21.

## Requests (to/from other roles)
_none yet_

## Visual pass (user request)
- More arched: `Arc` = 12-16 % of the length (about 1.0-2.2 m peak, <= ~27 deg slope; ends stay level), `Droop` capped at 0.25. Tighter, thicker braided strands, woven slats across the deck every ~0.4 m, and 5-petal flowers on both handrails: white petals with a yellow centre and yellow petals with a white centre (part ids 1.0 white, ~0.8 yellow). Tri count 2.6-3.9k (budget 4k: do not raise `LeafDensity` above 1.6).
- Shader `PlantGrow`: new `_FlowerColor` (yellow, part ids 0.72-0.88). Screens: docs/shots/bridge_variants.png, bridge_arched.png.

## Handoff
- **Files:** `Source/Blender/scripts/plants_bridge.py`, `Source/Blender/plants.blend` (node group `GN_Plant_Bridge`, object `GN_Bridge_Source` with the live modifier, baked `Plant_Bridge_01..06`), `docs/shots/bridge_variants.png`, `exports/Plant_Bridge_01.fbx` (not synced to Unity; T09 does that).
- **Use:** `exec(open(S+'plants_bridge.py').read()); build_all()` rebuilds group, 6 variants and saves. `bake_variant(n)` bakes one; `variant_params(n)` holds the per-variant inputs (Seed, Length 8.5-13.5, Arc, Width 1.3-1.9, Thickness, Curl, LeafDensity, Droop).
- **Geometry:** grows along Blender +Y from the origin, arc in Z, wobble in X. 1.8-2.0k tris, one material slot (`M_Bridge_Preview`). Deck is a closed slab (top at the path), 2 twisting strands + a handrail per side, leaf cards and small blossom cones.
- **growth_t** (one colour attribute): R = y/Length (monotonic 0..1), **G = part id** (0 deck, 0.34 vines, 0.67 leaves, 1.0 blossoms), B = random tint. T01/T11: map G to the palette in `M_PlantGrow`; reveal by R.
- **Empties/proxies:** `Anchor_NN` at the origin, `Tip_NN` at the deck end, `Land_NN` 0.8 m past it, `COL_NN_i` boxes (~1.4 m each, follow slope/yaw). Validation passes for all 6.
- **Known issues/notes:** Blender 5.2 cannot write modifier inputs from Python, so variants set the group's interface defaults before creating the modifier. Not done: separate nested utility groups (`GN_Util_*`), woven-plank deck detail, blossoms are tiny. Blender EEVEE shows odd grey patches where COL boxes render solid (now `hide_render`).
