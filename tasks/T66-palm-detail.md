# T66 — More polygons in the palm trunks and fronds

**Owner:** A5 Cartographer + A3 Plant Smith · **Status:** TODO (post-MVP, see `TODO.md`) · **Depends on:** T56

## Goal
The Kenney palms (T56) look blocky up close: low-poly trunks and flat-looking fronds. Give them more geometry.

## Deliverables
- Trunk: more radial and vertical segments, a gentle bend and slight taper, so the silhouette is round instead of faceted.
- Fronds: more leaflets / segments per frond with a droop curve, still one draw call per palm where possible.
- Keep the generated bark / frond textures (`World/PalmLook.cs`) working: the UVs must be rewritten for any new mesh.
- Stay inside the performance budget (T23: levels run at 1.6-4 ms per frame in the editor with 800-1750 draw calls): measure with `PlantGame > Smoke Test Levels` before and after on a palm-heavy sunny level.

## Options
- Generate the palm procedurally in code (a swept trunk plus curved frond strips), replacing the six Kenney FBXs; or
- Subdivide / rebuild the Kenney meshes in Blender (`Source/Blender/scripts/`, no `.blend` in `Game/Assets`) and re-import.

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
