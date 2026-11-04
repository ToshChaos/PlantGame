# TASKS-ANDROID.md — Android beta backlog

Open work only. The touch layer (T57-T61) is done; design and action map are in `docs/mobile-controls.md` (v3: no stick; tap the ground to walk,
tap the circle / diamond markers to act, double-tap a hotbar slot to drop, Jump and Pause buttons only).
Task details live in `tasks/T<nn>-*.md`; statuses: `TODO` · `DOING` · `REVIEW`. When a task is done, remove its row.

| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T62](tasks/T62-mobile-pause-focus-lifecycle.md) | App lifecycle, orientation, auto-pause, frame rate | A0 | — | DOING |
| [T63](tasks/T63-mobile-performance-quality-tier.md) | Texture compression and device profiling (tiers, first-run preset and world pass are in) | A6 | T23 | DOING |
| [T64](tasks/T64-android-build-and-beta.md) | Android build, signing, beta track, test matrix | A8 | T62, T63 | DOING |

## Still open
- First run of the Medium-stripped APK (50.7 MB, built 2026-11-04) on a device: not run yet, checklist in T64.
- Device check of tap-to-walk, marker taps (sprout watering, pickups, goal), beanstalk tap-to-climb, jump-to-glide and the smoother camera follow.
- Left-handed mirror (from T59) is not built.

## Needs a person
Developer account, keystore, hosting the privacy policy, contact email, store graphics, device testing and profiling (T63), internal test track upload. Steps: `docs/release-android.md`.

## Standing decision
Supported devices: phones released after 2020 (they ship with Android 10 or later). Min SDK stays 26: a superset, so nothing is dropped. A 2020-era phone is the low end of T63's test matrix.
