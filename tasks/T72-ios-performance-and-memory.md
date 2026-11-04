# T72 — iOS performance and memory

**Owner:** A6 Tech Artist · **Status:** DOING (build and settings pass done; the rest needs a device) (see `TASKS-IOS.md`) · **Depends on:** T63, T69

Shares tiers and budgets with T63. The Android profile numbers do not carry over, and the Simulator says nothing about performance.

## Left to do (on a device)
- **First-run preset:** the rule is Low under 3.5 GB, else Medium at 30 fps without postprocessing. Refine it from what profiling shows.
- **Rendered lines:** check `ScaleToScreen` at 810 / 650 lines on iPhone and iPad; iPads hit the 0.5 clamp, so look for softness.
- **Memory:** iOS kills an app over its limit, usually after a warning. Measure peak footprint with Xcode's memory gauge on the largest curated level and after several level loads in a row (no growth). Set the minimum device from the result, with the `increased-memory-limit` entitlement only if it is really needed.
- **Mesh memory:** 78 of 96 FBX models are Read/Write enabled so `WorldOptimizer` can merge them, which keeps a CPU copy of each mesh. All meshes total about 46 MB at runtime in the editor's count. If the memory profile is tight, release or skip those copies after merging.
- **Thermal:** 15 minutes at the default preset and at 60 fps; note throttling. The 30 fps default stays unless the numbers justify a change.

## Acceptance
- Level 1 and the largest curated level hold 30 fps at the first-run preset on the weakest supported device, without a memory kill.
- Numbers (device, preset, fps, peak memory, size) recorded in `docs/task-log.md`; minimum device written into `docs/private/release-ios.md`.

## Handoff
- 2026-11-04 build pass, numbers in `docs/task-log.md`: unsigned device `.app` 152 MB, about 58 MB zipped (the App Store's own figure comes with a signed archive).
- `IosBuild.Configure` sets `APP_UI_EDITOR_ONLY`, Medium managed stripping, accelerometer off and Metal API validation off. `StripUnusedPackageShaders` drops the inference package's shaders from every player. Rain and snow draw fewer particles on Medium and Low (`WeatherController.Density`).
- Checked in the import settings: textures are ASTC 6x6 on iOS, the 11 largest capped at 1024; audio loads are already right (short clips ADPCM, music streamed Vorbis).
- Still in the player, kept by package code: about 5 MB of generated C++ for `Unity.InferenceEngine` (its startup hooks) and all of Newtonsoft.Json with System.Xml and System.Data (the package's `link.xml`). Removing `com.unity.ai.inference` would drop the first but may reduce the editor's AI search, so it was left in.
