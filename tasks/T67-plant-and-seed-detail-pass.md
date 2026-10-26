# T67 — Plant and seed detail pass (final art pass before shipping)

**Owner:** A3 + A6 · **Depends on:** T49, T48, T33, T56
**Status:** DONE

## What changed
- **Triangle budget** per plant variant raised 4k -> 10k (`pipeline_common.MAX_TRIS`, AGENTS.md v2.8). Variants now: bridge ~2.6k, ladder ~5-6k, platform ~4k, collector ~2.1k.
- **Vine bridge:** blade-shaped drooping leaves (were diamonds); about half as many leaves overall and few, small ones mid-span (walking area). The sprout state (growth 0.08) is two small vines heading in the bridge direction: they carry `spr = 1`, `growth_t.R <= 0.075`; the rest of the bridge is remapped to R 0.085..1 so nothing else shows while sprouting.
- **Beanstalk:** thick knobbly stalk with swollen root, long drooping leaves, curling tendrils; pods are closed except one open pod showing three yellow beans.
- **Mushroom platform:** now a mushroom patch: every cap has its own tapered stalk from the ground under it (foot bulb, collar ring), no central stem or arms; cap positions / colliders unchanged. Rolled rim lip, 32-segment caps. Variants with two walkable caps get one small decorative mushroom (`padi = -1`, no collider), so there are always three.
- **Water Collector:** 24-segment ribbed bowl, nodes on the stem, cupped petals and wavy leaves with midribs, smooth shaded bowl + stem.
- **Textures:** `PlantGrow` has `_BarkTex / _LeafTex / _CapTex` (triplanar, no UVs needed; `T_PlantBark/Leaf/Cap` from `TextureBuilder`); menu `PlantGame > 2b Assign Plant Textures` sets them on every PlantGrow material.
- **Seed pickups** (`SeedMeshes`): UVs, welded normals, wrinkle / pit displacement, more segments; bamboo with sprouting twigs, bean with hilum and seam, puffball with warts and gill skirt, succulent with five leaf rings, coconut with husk ridges; per-seed textures `T_SeedBamboo/Bean/Puff/Succ/Coconut`.
- **Parachute is a leaf:** the glide visual (`LeafMesh.CreateHeld`) is one big serrated leaf held overhead by its curved stem with a midrib; the leaf plant on seed sources (`Scenery.LeafPlant`) grows three big leaves on curved stems instead of canopy domes. Still called / used as the parachute.

## Play test (2026-10-26, level 4 Exploration)
- Bridge: sprout shows two small vines, grows, grown bridge looks right (`t67_play_*`). Beanstalk grows, closed pods visible. Glide leaf held in the right hand, stem beside the body, blade over the head; first version was too high and edge-on to the chase camera, lowered to 2.45 m and levelled; last tweak (less fold, no separate midrib) compiled but was not re-captured.
- Not play-tested: walking a grown mushroom patch (colliders unchanged from the verified Blender jump check), a full beanstalk climb.

## Open
- The platform stalks are cone + foot sphere: if they look too plain, add bark detail via the new triplanar texture strength (`_TexStrength`).
