# T56 — Palm trees and a real parachute

**Owner:** A5 Cartographer + A1 Gameplay · **Status:** REVIEW

## Done
- Palms: `Scenery.TreeKinds` is 8; kind 7 uses the already imported Kenney palm models (`tree_palmTall`, `DetailedTall`, `Bend`, `tree_palm`, `DetailedShort`, `Short`; CC0, listed in LICENSES.md). Per-island weight: 0 in Snow / Fog, 0.2 in Rain / Autumn, 0.9-1.9 in Sunny / Sunset / Sunrise. 1.3x height, trunk capsule collider, no falling leaves.
- Parachute: `PlayerController.TryStartGlide` now shows `Scenery.CanopyMesh(1.7, 1.0)` (green / yellow panels, `M_FoliageLight` + `M_GlideLeaf`) 3.1 m up on six cords, replacing the flat yellow blade.
- Shots: `docs/shots/palms_island.png`, `docs/shots/parachute_glide.png`.

- Palm textures: `World/PalmLook.cs` swaps the Kenney palm bark / frond flat colours for generated textures (ringed bark, leaflet-striped fronds) and rewrites the UVs (the FBX UVs only address a palette); the six palm FBXs are now Read/Write. Shots `docs/shots/palm_tex_*.png`.
- Size variety: every tree's height is scaled by a position hash (0.72x to about 1.5x; palms 0.65x to about 1.7x) in `Scenery.Tree`, without touching the layout's random stream.
- Coconut: item `coconut` (Misc, `ItemBuilder` row, brown pickup). Each palm has at most one, lying at its foot (about 65 percent of palms); it never regrows, Q picks it up and drops it, and no target accepts it (soil patches take Seeds only). It does nothing else yet.

- Purple grass: about 3 percent of grass tufts (picked by position, so layouts are untouched) use `M_GrassBladesPurple`, the blade sprite tinted violet (`Overgrowth.PurpleBladeMat`). Shot `docs/shots/grass_purple.png`.

## Notes / open
- Frond stripes are bold close up (chevron look) and dense at the crown centre; soften `PalmLook.FrondTexture` if it reads as zebra stripes. Palm size 0.65x-1.7x and the coconut are checked in the title world only, not a real pickup with Q.
- Seed codes: palms change tree placement, so `SeedCode.GeneratorVersion` is unchanged but existing seeds look different (decoration only, layouts unchanged).
- Parachute checked as a standalone render, not yet in a real glide; the pickup leaf and leaf plant are unchanged (a real Parachute Bloom plant is still open). Kenney palm crowns have a green block on top (model quirk).
- Other free palm options if these look too plain: CraftPix "Free Tropical Palm Tree" (needs sign-in download), OpenGameArt "Palm tree v2" (licence unchecked).
