# TASKS.md — board after the MVP

The MVP (T00-T31) is finished: see **`TASKS-MVP.md`**. This file holds everything since: finished polish and content tasks, plus the open work.
The Android beta tasks (T57-T64) are on `TASKS-ANDROID.md`. Details, acceptance criteria and handoff notes stay in `tasks/T<nn>-*.md`;
roles are in `AGENTS.md` section 4. When a task starts, set its status in the task file and in the table below.

Status: `TODO` · `DOING` · `REVIEW` · `DONE` · `FIXED` · `CUT`.

## Board
| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T21](tasks/T21-level-tuning.md) | Balance pass on the ten curated levels and the generator (gaps, seed supply) | A0 | T14, T15 | TODO |
| [T32](tasks/T32-parachute-leaf.md) | Parachute leaf: plant grows leaves, one-time glide mid-jump | A1+A2+A3 | T05, T11, T27 | DONE |
| [T33](tasks/T33-seed-visuals.md) | Seed visuals: bamboo / bean / puffball / succulent in world, hotbar and seed trees | A1+A5 | T05, T12, T14 | DONE |
| [T34](tasks/T34-level-variety.md) | Level variety: alternative soil spots per gap, optional side islands, islands that never overlap | A5 | T14 | DONE |
| [T35](tasks/T35-island-ground-types.md) | Island ground types (grass / mud / rock) and a better grass texture | A6+A5 | T01, T13 | DONE |
| [T36](tasks/T36-world-look-and-spacing.md) | Coloured textured trees, bigger trees on wet islands, parachute canopy leaves, waterfall look, tall island rims, tilted islands, wider gaps + stretched bridges, seed-tree ambience, silent title | A5+A6+A7 | T14, T35 | DONE |
| [T37](tasks/T37-win-music.md) | Win-condition music (and optional fail sting) | A7 | T17, T19 | DONE |
| [T38](tasks/T38-scale-density-big-island.md) | Imported trees 2x bigger, more rocks and trees, one even bigger island | A5 | T13, T14, T36 | DONE |
| [T39](tasks/T39-overgrown-feel.md) | Overgrown feel: leaf litter, moss, vines, ferns, reclaimed ruins | A5+A6 | T35, T36, T38 | DONE |
| [T40](tasks/T40-plant-and-flower-textures.md) | Better vine-bridge flowers and flower texture, bamboo stems of different heights | A3+A6 | T06, T09, T33 | DONE |
| [T41](tasks/T41-island-texture-gaps.md) | Bug: gaps in island textures (e.g. Beanstalk Hill) | A6+A5 | T35, T36 | FIXED |
| [T42](tasks/T42-vague-goal-hints-and-goal-cue.md) | Vague goal hints, goal-island visual cue, remove goal distance indicator | A7+A1 | T15, T19 | DONE |
| [T43](tasks/T43-thematic-ambience-and-ruin-sounds.md) | Ambience matching its source, mysterious sounds near ruins | A7 | T17, T18, T36 | DONE |
| [T44](tasks/T44-taller-beanstalk-no-top-platform.md) | Taller beanstalk, no platform at the top | A3+A2+A5 | T07, T09, T11, T14 | CUT (merged into T51) |
| [T45](tasks/T45-snow-and-heat-seed.md) | Snow ground, frozen soil and water, Heat Seed that melts snow | A5+A2+A6+A1 | T14, T27, T33, T35 | DONE |
| [T46](tasks/T46-windy-summit-level.md) | Windy summit level: spiral ascent, altitude wind gusts, snowy top, wind ambience, wind on some random levels | A5+A1+A7 | T14, T45, T43 | DONE |
| [T47](tasks/T47-footsteps-and-bridge-sounds.md) | Footsteps per island surface, vine-bridge creak and rustle sounds | A7+A1 | T18, T35 | DONE |
| [T48](tasks/T48-concave-mushroom-platforms.md) | Mushroom platform pads concave (dish-shaped) instead of flat | A3+A2 | T08, T09, T11 | DONE |
| [T49](tasks/T49-realistic-textured-plants.md) | All grown plants more realistic and textured (bark, veins, mushroom detail, grow shader texture slot) | A3+A6 | T06-T09, T28, T32, T40, T48 | DONE |
| [T50](tasks/T50-ten-levels-snow-wind-fog.md) | Ten curated levels: add Snow, Windy summit and Dense Fog levels; snow / wind / fog options in world generation | A5+A6+A1 | T45, T46 | DONE |
| [T51](tasks/T51-beanstalk-top-platform-fix.md) | Bug: beanstalk still has a top platform (T44 was only specified, never built) | A3+A2 | T44 | DONE |
| [T53](tasks/T53-more-8bit-music.md) | More 8-bit music: menu tune, one shared tune for level 1 + generated levels, a different themed tune for each other curated level | A7 | T17, T37, T43 | DONE |
| [T54](tasks/T54-island-dressing.md) | Island dressing: more grass, decals, shrubs, sticks, marble chips, gems, shiny debris, vine-heavy ruins, pond plants, murky ponds | A5+A6 | T39, T35, T36 | DONE |
| [T55](tasks/T55-overgrown-rename-and-play-fixes.md) | Rename to Overgrown, camera see-through fade, soil patches, Heat Seed on E, bridge spacing, decoy soils, test scene cleanup | A0+A1+A5 | T14, T26, T45, T54 | DONE |
| [T56](tasks/T56-palms-and-parachute.md) | Palm trees (Kenney CC0 models, generated bark / frond textures, one coconut pickup each), all trees vary in size, rare purple grass; glide visual is now a real parachute (green/yellow dome + cords) | A5+A1 | T36, T32 | DONE |
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
