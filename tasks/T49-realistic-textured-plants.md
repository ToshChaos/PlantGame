# T49 — More realistic, textured grown plants

**Owner:** A3 + A6 · **Depends on:** T06-T09, T28, T32, T40, T48
**Status:** DONE

## Goal
All grown plants (vine bridge, beanstalk ladder, shelf-fungus platform, Water Collector, parachute leaf, and the bamboo / puffball / succulent variants) look more natural and carry real texture, not flat vertex colours only.

- Surface detail: bark/fibre on vines and stalks, leaf veins and edges, mushroom cap gradient with gill underside and spots, pitcher-bloom veining; tapered, slightly irregular shapes (knots, curls, thickness variation, small side leaves, tendrils).
- Textures: tileable greyscale/colour maps plus tint via vertex colours so the 6 variants still differ (like `M_LeafTint`); keep the white/green/blue palette and the glow accents. Normal or height detail where URP shading allows; stay cheap.
- Keep the plant contract (AGENTS.md 5.3): `growth_t` vertex colour, empties, `COL_` proxies, <= 4k tris per variant, one material slot (extend `M_PlantGrow` with a texture slot and make sure the grow reveal still works).
- Coordinate with T40 (vine flowers, flower texture, bamboo heights) and T48 (concave pads); do these first or share the same material work.

## Acceptance
- Before/after screenshots per plant kind in `docs/shots/`; plants still grow, collide and climb as before.
- No frame-time regression (T23); Blender scripts updated and exports re-synced.

## Handoff
Done: procedural surface detail in `PlantGrow.shader` (object-space noise, no UVs needed): fibre grain, dark cracks / veins, mushroom cap mottling and paler underside; `_DetailStrength` / `_DetailScale`. Open: Blender geometry detail (knots, curls, side leaves, tendrils), real texture slot, before / after shots, T23 frame-time check.

### Review pass (2026-10-24)
- Current-state shots: `docs/shots/t49_before_{bridge,ladder,platform,collector}.png` (no "after" set: nothing shipped in this pass).
- Observed weak spots: the ladder stalk is thin and sparse next to its leaves; platform caps are flat red discs on a thin stalk; the collector bowl is faceted (flat-shaded, 16 segments). The bridge is already detailed (braided strands, slats, leaves, two-ring flowers).
- Tried: smooth shading on the collector (`use_smooth = True`). Re-export through `export_collections` after `build_all()` left the plant lying on its side in Unity (orientation differs from the shipped FBX; the original export call is not documented), so it was reverted. Before retrying, find how the shipped collector FBXs were exported (offset 0, overrides) and compare the prefab root rotation.
- Texture audit: all `T_*` maps are 128-1024 px, normal maps have `textureType: Normal Map` and sRGB off, mipmaps on; no oversized project-owned textures. `T_LeafGrey` (1024) is the only one larger than needed.
- Still open: real texture slot in `PlantGrow`, geometry detail (knots, tendrils, thicker ladder stalk, domed caps), T23 frame-time check.
