# TASKS-ANDROID.md — Android beta backlog

Android board, split out of `TODO.md`. Controls design and action map: `docs/mobile-controls.md` (v3: no stick; tap the ground to walk,
tap the circle / diamond markers to act, double-tap a hotbar slot to drop, Jump and Pause buttons only).
Task details live in `tasks/T<nn>-*.md`; statuses: `TODO` · `DOING` · `REVIEW` · `DONE` · `CUT`.


| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T57](tasks/T57-mobile-input-layer.md) | Touch input layer (look drag, tap-to-walk, tap markers, Jump / Pause) behind `PlayerInputReader` | A1 | — | REVIEW |
| [T58](tasks/T58-touch-auto-focus-context-button.md) | Auto-focus interaction; tap the circle / diamond to act (context button removed) | A1 | T57 | REVIEW |
| [T59](tasks/T59-mobile-touch-hud-layout.md) | Touch HUD layout, safe area, scaling, left-handed mode | A7 | T57 | TODO |
| [T60](tasks/T60-touch-hotbar-and-pause.md) | Tappable hotbar, pause / Back, touch-friendly menus, soft-keyboard seed entry | A7 | T59 | REVIEW |
| [T61](tasks/T61-touch-gameplay-tuning.md) | Camera, climbing, jump and glide tuning for touch | A1 | T57, T58 | REVIEW |
| [T62](tasks/T62-mobile-pause-focus-lifecycle.md) | App lifecycle, orientation, auto-pause, frame rate | A0 | — | DOING |
| [T63](tasks/T63-mobile-performance-quality-tier.md) | Quality tier, decoration thinning, texture compression, profiling | A6 | T23 | TODO |
| [T64](tasks/T64-android-build-and-beta.md) | Android build, signing, beta track, test matrix | A8 | T62, T63 | DOING |


## Still open after the v3 control rework
- Device check of tap-to-walk, marker taps (sprout watering, pickups, goal), beanstalk tap-to-climb, jump-to-glide and the smoother camera follow.
- T59 touch HUD layout: safe area, scaling, left-handed mode (left-handed mirror is not done).
- T62 / T64: lifecycle, build, signing, beta track; T63 quality tier and profiling.
- Pause menu spacing and hints were adjusted for tap controls; recheck on small phones.
