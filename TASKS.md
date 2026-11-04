# TASKS.md — board after the MVP

Open work only. The MVP (T00-T56, finished 2026-10-25) is on **`TASKS-MVP.md`**; T65-T67 are done (details in their task files).
The Android beta tasks are on `TASKS-ANDROID.md`, the iOS release tasks on `TASKS-IOS.md`. Details, acceptance criteria and handoff notes stay in `tasks/T<nn>-*.md`;
roles are in `AGENTS.md` section 4. When a task starts, set its status in the task file and in the table below; when it is done, remove its row.

Status: `TODO` · `DOING` · `REVIEW`.

## Board
| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T21](tasks/T21-level-tuning.md) | Balance pass on the ten curated levels and the generator (gaps, seed supply) | A0 | T14, T15 | TODO |
| [T68](tasks/T68-title-and-feedback-fixes.md) | Title crystals removed, watering animation on the last charge, smaller white sprout mushroom | A0 | T67 | DOING |

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
