# TODO.md — post-MVP backlog

The hackathon MVP is finished: every task on `TASKS.md` is `DONE` (or `CUT`). This file holds the work planned for
after the MVP. Nothing here has been started. Details, acceptance criteria and handoff notes stay in `tasks/T<nn>-*.md`;
roles are in `AGENTS.md` section 4. When a task here starts, set its status in the task file and in the table below.

Status: `TODO` · `DOING` · `REVIEW` · `DONE` · `CUT`.

## Mobile (Android beta)
Design and control mapping: `docs/mobile-controls.md`. Desktop controls (`AGENTS.md` 5.8) stay the contract until T57
adds a touch column.

| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T57](tasks/T57-mobile-input-layer.md) | Touch input layer (stick, look drag, buttons) behind `PlayerInputReader` | A1 | — | REVIEW |
| [T58](tasks/T58-touch-auto-focus-context-button.md) | Auto-focus interaction and one context button | A1 | T57 | REVIEW |
| [T59](tasks/T59-mobile-touch-hud-layout.md) | Touch HUD layout, safe area, scaling, left-handed mode | A7 | T57 | TODO |
| [T60](tasks/T60-touch-hotbar-and-pause.md) | Tappable hotbar, pause / Back, touch-friendly menus, soft-keyboard seed entry | A7 | T59 | REVIEW |
| [T61](tasks/T61-touch-gameplay-tuning.md) | Camera, climbing, jump and glide tuning for touch | A1 | T57, T58 | REVIEW |
| [T62](tasks/T62-mobile-pause-focus-lifecycle.md) | App lifecycle, orientation, auto-pause, frame rate | A0 | — | DOING |
| [T63](tasks/T63-mobile-performance-quality-tier.md) | Quality tier, decoration thinning, texture compression, profiling | A6 | T23 | TODO |
| [T64](tasks/T64-android-build-and-beta.md) | Android build, signing, beta track, test matrix | A8 | T62, T63 | DOING |

## Content and polish
| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T21](tasks/T21-level-tuning.md) | Balance pass on the ten curated levels and the generator (gaps, seed supply) | A0 | T14, T15 | TODO |
| [T65](tasks/T65-leafless-trees.md) | Bare trees: snow-level broadleaf trees leafless except two yellow leaves, rare leafless variant elsewhere | A5+A6 | T36, T39, T45, T56 | TODO |
| [T66](tasks/T66-palm-detail.md) | More polygons in the palm trunks and fronds | A5+A3 | T56 | TODO |

## Checks the MVP did not cover
- A full by-hand playthrough of all ten levels in both modes (`docs/qa-checklist.md`); the automated pass only loads each level.
- Built-player smoke test (T23): title -> level 1 -> win -> back, random level, pasted seed code, best times kept across launches; a profile in the player, not the editor.
- Listening pass on the music, ambience, footsteps and bridge sounds (T43, T47, T53).
- Balance of the snow levels (Heat Seed supply) (T45); part of T21.
- Known editor quirk B1 (`docs/bugs.md`): `PlayFromTitle` resets the editor start scene after a domain reload.

## Ideas (not planned yet)
- A real Parachute Bloom plant (T32 shipped the parachute as a leaf pickup).
- Retrieve plant: Use on a grown plant gives the seed back.
- Glow-shard collectibles, a day/night cycle, gamepad support.
