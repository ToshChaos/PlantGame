# T72 — iOS performance and memory

**Owner:** A6 Tech Artist · **Status:** TODO (see `TASKS-IOS.md`) · **Depends on:** T63, T69

Shares tiers and budgets with T63. The Android profile numbers do not carry over; the Simulator says nothing about performance, so this needs a real device.

## Work
- **First-run preset:** `QualityTier.DetectPreset` gives Low under 5 GB or under 6 cores. That rule is tuned for Android: a 4 GB iPhone has a strong GPU and would start on Low. Add an iOS rule from what profiling shows, from `SystemInfo` or the device model.
- **Rendered lines:** check `ScaleToScreen` at 810 / 650 lines on iPhone and iPad; iPads hit the 0.5 clamp, so look for softness.
- **Memory:** iOS kills an app over its limit without any callback. Measure peak footprint with Xcode's memory gauge on the largest curated level and after several level loads in a row (no growth). Set the minimum device from the result, with the `increased-memory-limit` entitlement only if it is really needed.
- **Textures:** confirm the iPhone overrides from `QualityTierBuilder.CapPhoneTextures` and ASTC are in use. T63's open ASTC work, if it lands, covers both platforms.
- **Thermal:** 15 minutes at the default preset and at 60 fps; note throttling. The 30 fps default stays unless the numbers justify a change.
- **Download size:** record IPA size and installed size (the Android APK is about 62 MB, the Mac build 164 MB).

## Acceptance
- Level 1 and the largest curated level hold 30 fps at the first-run preset on the weakest supported device, without a memory kill.
- Numbers (device, preset, fps, peak memory, size) recorded in `docs/task-log.md`; minimum device written into `docs/release-ios.md`.

## Handoff
