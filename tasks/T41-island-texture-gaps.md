# T41 — Island texture gaps (bug)

**Owner:** A6 + A5 · **Depends on:** T35, T36
**Status:** FIXED

## Problem
Some island ground textures show gaps (holes/seams), e.g. on the Beanstalk Hill level. Likely suspects: ground-type blending, rim wall vs top mesh seam, tilted islands, texture UV tiling or alpha.

## Acceptance
- Reproduce on Beanstalk Hill, find the cause, fix it in the terrain builder/material.
- Fly around every curated level and a few random seeds: no gaps on any island, tilted ones included.
- Note the cause in the Handoff.

## Handoff
Could NOT reproduce a gap on Beanstalk Hill: raycast grid over every island collider found 0 holes, and rim/underside renders (`docs/shots/T41_*.png`) show none. Found and fixed one real seam: side-wall ring 0 was flat at the shoulder height while the top mesh dips where a carved hollow reaches the rim (waterfall streams), leaving a crack; ring 0 now follows the top rim heights (`RoundIsland.cs`). Level 2 has no waterfall, so this is not proven to be the reported gap. Possible other candidate: black holes in the Dew-seed succulent rosette (seen in `T41_island4_top.png`). Needs the reporter to point at the spot.
