# TASKS.md — board after the MVP

The MVP (T00-T56, finished Sunday 2026-10-25) is finished: see **`TASKS-MVP.md`**. This file holds everything since: the T67 detail pass, the bare-tree and palm tasks, plus the open work.
The Android beta tasks (T57-T64) are on `TASKS-ANDROID.md`. Details, acceptance criteria and handoff notes stay in `tasks/T<nn>-*.md`;
roles are in `AGENTS.md` section 4. When a task starts, set its status in the task file and in the table below.

Status: `TODO` · `DOING` · `REVIEW` · `DONE` · `FIXED` · `CUT`.

## Board
| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T21](tasks/T21-level-tuning.md) | Balance pass on the ten curated levels and the generator (gaps, seed supply) | A0 | T14, T15 | TODO |
| [T65](tasks/T65-leafless-trees.md) | Bare trees: snow-level broadleaf trees leafless except two yellow leaves, rare leafless variant elsewhere | A5+A6 | T36, T39, T45, T56 | DONE |
| [T66](tasks/T66-palm-detail.md) | More polygons in the palm trunks and fronds | A5+A3 | T56 | DONE |
| [T67](tasks/T67-plant-and-seed-detail-pass.md) | Plant and seed detail pass: richer bridge / beanstalk / mushroom patch / collector models, plant textures, detailed seeds, parachute as a big leaf | A3+A6 | T49, T56 | DONE |

## Checks the MVP did not cover
- A full by-hand playthrough of all ten levels (`docs/qa-checklist.md`); the automated pass only loads each level.
- Built-player smoke test (T23): title -> level 1 -> win -> back, random level, pasted seed code, best times kept across launches; a profile in the player, not the editor.
- Listening pass on the music, ambience, footsteps and bridge sounds (T43, T47, T53).
- Balance of the snow levels (Heat Seed supply) (T45); part of T21.
- Known editor quirk B1 (`docs/bugs.md`): `PlayFromTitle` resets the editor start scene after a domain reload.

## Ideas (not planned yet)
- A real Parachute Bloom plant (T32 shipped the parachute as a leaf pickup).
- Retrieve plant: Use on a grown plant gives the seed back.
- Glow-shard collectibles, a day/night cycle, gamepad support.
