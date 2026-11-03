# T16 — Background: skybox, clouds, fog, post-processing

**Owner:** A6 Tech Artist · **Depends on:** T01
**Status:** DONE

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
- **Files:** `Art/Shaders/OvergrowthSky.shader` (gradient + sun), `CloudLayer.shader` (scrolling noise, radial fade, fog-tinted), `Scripts/World/Atmosphere.cs`, `CloudDrift.cs`, `Editor/MainSceneBuilder.cs` (`PlantGame > 11 Build Atmosphere`), materials `M_Sky M_CloudSea M_CloudSeaHigh M_CloudCard`, `T_Cloud.png`, `Art/VFX/PostFX_Overgrowth.asset`, **`Prefabs/Environment/Atmosphere.prefab`**.
- Prefab = skybox, ambient trilight, linear fog, sun angle, two camera-following cloud-sea layers (no edge), 22 drifting cloud cards, 9 far backdrop islands (no colliders), global Volume (bloom, ACES, colour adjust, cool shadows / warm highlights, light vignette). `GameSettings.Bloom` + pause-menu "Glow" toggle disable bloom.
- Player camera now uses the skybox and has post-processing on. Screenshot `docs/shots/main_start.png`.
- **Known:** frame cost not profiled (T23); far islands are plain round islands until the T10 kit exists; one sky preset (no dusk).
- Art pass from the user's reference image: trees, bushes, flowers, white ruins and a portal arch on every island, floating rocks / islets, golden butterflies, 40 cloud cards, warmer light. See `docs/art-bible.md` (Reference mood).
