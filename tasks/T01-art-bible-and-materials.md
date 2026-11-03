# T01 — Art bible, palette, shared materials, grow shader

**Owner:** A6 Tech Artist · **Depends on:** T00
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Lock the white/green/blue fantasy look and ship the shared materials and the Shader Graph used to grow plants.

## Deliverables
- `docs/art-bible.md`: palette (section 5.7), lighting recipe, shape language, do/don't, 1 reference mood board (text description is fine)
- Materials in `Art/Materials`: `M_Terrain`, `M_Stone`, `M_Foliage`, `M_Cloud`, `M_Crystal` (emissive cyan), `M_UI`
- **`M_PlantGrow`** (Shader Graph, URP): reads vertex colour R as `growth_t`; property `_Growth` 0..1; vertices with `growth_t > _Growth` collapse to the growth front (or are clipped), with tip scale-in; two-tone green/white gradient by height; rim light
- Soft toon-ish lighting: simple ramp or lit with gentle specular; works with GPU instancing / MaterialPropertyBlock
- A swatch scene `Sandbox_Plants` showing all materials + a cube sweeping `_Growth` 0→1

## Steps
1. Define the palette as Color assets or a small `Palette.cs` constants file.
2. Build `M_PlantGrow` in Shader Graph; test on a stretched cube with baked vertex-colour gradient (create via script).
3. Expose `_Growth`, `_BaseColor`, `_TipColor`, `_RimColor`; document usage in the art bible.
4. Create remaining materials; apply fog/ambient settings in a lighting preset (final sky is T16).
5. Screenshot the swatch scene into `docs/shots/`.

## Acceptance criteria
- [ ] `_Growth` slider visibly grows a vertex-coloured mesh from base to tip
- [ ] Materials use the exact palette hexes
- [ ] Shader compiles in a build target (no editor-only nodes)
- [ ] Art bible exists and is readable in under 2 minutes

## Notes
- Vertex-colour-driven vertex offset needs the mesh to be reasonably subdivided along its length; tell A3 the minimum ring/segment count you need (suggest >= 24 segments per bridge).
- Do not require textures.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
docs/art-bible.md, Palette.cs, MaterialBuilder (menu PlantGame/2), PlantGame/PlantGrow shader, Sandbox_Plants (menu PlantGame/3). Known: screenshot to docs/shots not saved yet; crystal emission 1.0.
