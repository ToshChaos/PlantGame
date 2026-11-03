# T44 — Taller beanstalk, no platform at the top

**Owner:** A3 + A2 + A5 · **Depends on:** T07, T09, T11, T14
**Status:** CUT (merged into T51)

## Goal
- Remove the platform/pad at the top of the Beanstalk ladder; the climber steps off directly onto the target island edge (`Land`).
- Make the beanstalk taller. Current envelope is 4-9 m vertical (AGENTS.md 5.2); raise it (e.g. up to ~14 m) and update the contract and the generator's vertical gaps and `Land` placement to match.

## Acceptance
- Ladder variants re-exported from Blender (`Source/Blender/scripts/plants_*`), no top platform mesh or collider.
- Climbing to the top steps onto `Land` without snagging; checked on Beanstalk Hill, Skyfall and a few random seeds.
- AGENTS.md 5.2 and the change log updated; Validate Seeds still 0 invalid.

## Handoff
_(empty)_
