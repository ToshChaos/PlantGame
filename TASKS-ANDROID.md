# TASKS-ANDROID.md — Android beta backlog

Android board, split out of `TASKS.md`. Controls design and action map: `docs/mobile-controls.md` (v3: no stick; tap the ground to walk,
tap the circle / diamond markers to act, double-tap a hotbar slot to drop, Jump and Pause buttons only).
Task details live in `tasks/T<nn>-*.md`; statuses: `TODO` · `DOING` · `REVIEW` · `DONE` · `CUT`.


| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T57](tasks/T57-mobile-input-layer.md) | Touch input layer (look drag, tap-to-walk, tap markers, Jump / Pause) behind `PlayerInputReader` | A1 | — | DONE |
| [T58](tasks/T58-touch-auto-focus-context-button.md) | Auto-focus interaction; tap the circle / diamond to act (context button removed) | A1 | T57 | DONE |
| [T59](tasks/T59-mobile-touch-hud-layout.md) | Touch HUD layout, safe area, scaling, left-handed mode | A7 | T57 | DONE |
| [T60](tasks/T60-touch-hotbar-and-pause.md) | Tappable hotbar, pause / Back, touch-friendly menus, soft-keyboard seed entry | A7 | T59 | DONE |
| [T61](tasks/T61-touch-gameplay-tuning.md) | Camera, climbing, jump and glide tuning for touch | A1 | T57, T58 | DONE |
| [T62](tasks/T62-mobile-pause-focus-lifecycle.md) | App lifecycle, orientation, auto-pause, frame rate | A0 | — | DOING |
| [T63](tasks/T63-mobile-performance-quality-tier.md) | Quality tier, decoration thinning, texture compression, profiling | A6 | T23 | TODO |
| [T64](tasks/T64-android-build-and-beta.md) | Android build, signing, beta track, test matrix | A8 | T62, T63 | DOING |


## Still open after the v3 control rework
- Device check of tap-to-walk, marker taps (sprout watering, pickups, goal), beanstalk tap-to-climb, jump-to-glide and the smoother camera follow.
- T59 touch HUD layout: safe area, scaling, left-handed mode (left-handed mirror is not done).
- T62 / T64: lifecycle, build, signing, beta track; T63 quality tier and profiling.

## Release prep (2026-10-29)
- Done in repo: 30 fps battery-saver option (touch Options), release signing / version code from env vars (`AndroidBuild.ApplySigning`), `docs/privacy-policy.md`, `docs/store-listing.md`, `docs/release-android.md` (keystore, AAB, Play Console steps).
- Needs a person: developer account, keystore, hosting the privacy policy, contact email, store graphics, device testing and profiling (T63), internal test track upload.
