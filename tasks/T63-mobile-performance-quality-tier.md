# T63 — Mobile quality tier and performance budget

**Owner:** A6 Tech Artist · **Status:** TODO (post-MVP, see `TASKS.md`) · **Depends on:** T23 (build baseline)

Design: `docs/mobile-controls.md`. Not scheduled for the hackathon weekend.

## Work
- Add a `Mobile` URP quality level + renderer asset: no or cheap shadows (one cascade, short distance), HDR off or low, bloom cheap or off, MSAA off / 2x, render scale 0.8 option, SRP batcher compatible shaders.
- Budgets: 30 fps minimum / 60 target on a mid-range Android (Snapdragon 7-series), under ~150k tris and ~150 draw calls on the busiest island, texture memory under ~500 MB, no GC spikes during play.
- Scale down on mobile: grass tuft count, leaf litter, decals, vines, tree and rock density (islands keep their layout; only decoration thins out, so seed codes and solvability are unaffected), plant grow shader detail (T49), water shader (T30) simple variant, transparent overdraw (`M_Keep_*`).
- Texture import overrides for Android: ASTC 6x6 (4x4 for UI), mip limits, max sizes; audio streams for music; check Read/Write models (palm FBXs) and generated runtime textures for memory.
- Profile on a real device (Unity Profiler over USB, Frame Debugger) and record numbers in `docs/task-log.md`.

## Acceptance
- Level 1 and the largest curated island hold the target frame rate on the reference device; a quality option (Low / Medium / High) exists in Options.

## Handoff
_(fill in when done)_
