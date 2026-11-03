# T51 — Beanstalk still has a top platform (fix)

**Owner:** A3 + A2 · **Depends on:** T44
**Status:** DONE

## Problem
T44 (taller beanstalk, no top platform) was only written up as a task, never implemented: the exported ladder variants still end in a platform / pad at the top and the envelope is still 4-9 m.

## Goal
Do T44: no pad or platform mesh/collider at the top of `Plant_Ladder_NN`, the climber steps from the last rung onto the target island edge (`Land`); taller stalk; update AGENTS.md 5.2, `LevelLayout` (`LadderHeight`, `LadderPadReach`, `LadderPadGap`) and the validator.

## Acceptance
- Re-exported ladders (`plants_ladder.py`) with no top platform; verified in Beanstalk Hill, Skyfall and random seeds by actually climbing.
- Validate Seeds 0 invalid.

## Handoff
`plants_ladder.py`: top platform, its stem and collider removed; heights 8.5-12.3 m (variant 3 = 11 m, tris <= 3.8k); `Land` 1.9 m in front of the stalk top; thin stalk collider (pipeline wants one proxy). `LevelLayout`: LadderHeight 11, PadReach 1.2, PadGap 0, EdgeInset 1.6. Re-exported, synced, ladder prefabs rebuilt (menu 8). Verified in Beanstalk Hill: planted, grew, climbed with W and stepped onto island 2 (land 0.8 m inside the rim). Validate Seeds 0 invalid. Not retested: Skyfall and random seeds by hand.
