# T16 — Background: skybox, clouds, fog, post-processing

**Owner:** A6 Tech Artist · **Depends on:** T01 · **Estimate:** 3h · **Day:** 2
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
The floating-island vibe: a luminous white/blue sky, drifting clouds below and around, soft fog, gentle bloom.

## Deliverables
- Procedural gradient skybox (Shader Graph or panoramic) in palette: horizon white → sky blue → deep blue top; sun disc + soft glow; subtle stars/glints optional
- Distant parallax layers: big cloud-card billboards (render cloud sprites in Blender or generate procedurally) and a cloud sea below the islands
- Slow drifting cloud particles/planes; floating distant islands as backdrop meshes (re-use kit, low LOD, unlit fog tint)
- URP Volume: bloom (low), colour grading (slightly lifted blacks, cool shadows, warm highlights), vignette (very low), tonemapping; height/distance fog
- Directional light + ambient gradient presets; `Prefabs/Environment/Atmosphere.prefab` (drop-in for Main)
- Quality guard: mobile/low-end friendly toggle that disables bloom

## Steps
1. Build in `Sandbox_World` next to generated islands for scale.
2. Capture 3 screenshots (day/dusk-ish tones optional) in `docs/shots/`.
3. Hand the prefab to A0 for Main.

## Acceptance criteria
- [ ] Looks coherent with the art bible; islands readable against the sky
- [ ] Frame rate impact < 1 ms on the dev machine
- [ ] No visible skybox seams; cloud sea has no hard edge within play bounds

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
