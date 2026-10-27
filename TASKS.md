# TASKS.md — MVP board (complete)

The hackathon MVP is finished: every task below is `DONE`, `FIXED` (bugs) or `CUT`. Post-MVP work (mobile, bare trees,
palm detail, level balance) lives in **`TODO.md`**. Details, acceptance criteria and handoff notes are in
`tasks/T<nn>-*.md`; roles in `AGENTS.md` section 4.

## Board
| ID | Task | Owner | Depends on | Status |
|----|------|-------|-----------|--------|
| [T00](tasks/T00-project-bootstrap.md) | Project bootstrap, packages, folders, git | A0 | — | DONE |
| [T01](tasks/T01-art-bible-and-materials.md) | Art bible, palette, shared materials, grow shader | A6 | T00 | DONE |
| [T02](tasks/T02-blender-unity-pipeline.md) | Blender → Unity export pipeline & import presets | A0+A3 | T00 | DONE |
| [T03](tasks/T03-player-controller.md) | Player controller: WASD, look, jump, climb, camera | A1 | T00 | DONE |
| [T04](tasks/T04-interaction-system.md) | Interaction: focus, prompt, pick up / drop / use | A1 | T03 | DONE |
| [T05](tasks/T05-inventory.md) | Limited inventory (4 slots) + item definitions | A1 | T04 | DONE |
| [T06](tasks/T06-gn-vine-bridge.md) | Geometry Nodes: Vine bridge plant | A3 | T00 | DONE |
| [T07](tasks/T07-gn-beanstalk-ladder.md) | Geometry Nodes: Beanstalk ladder plant | A3 | T06 | DONE |
| [T08](tasks/T08-gn-platform-plant.md) | Geometry Nodes: Shelf-fungus platform plant | A3 | T06 | DONE |
| [T09](tasks/T09-bake-and-export-variants.md) | Bake 6 variants per plant + proxies + export | A3 | T02, T06 (T07, T08, T28 as they land) | DONE |
| [T10](tasks/T10-island-kit.md) | Modular island kit (terrain, ruins, vegetation) | A4 | T02 | CUT |
| [T11](tasks/T11-growth-system.md) | Growth system: SoilPatch, growth, colliders, climb | A2 | T01, T06 (placeholder), T09 | DONE |
| [T12](tasks/T12-world-items-and-goal.md) | Seed pickups, soil patch prefab, Heart Tree goal | A1 | T05, T11 | DONE |
| [T13](tasks/T13-island-assembler.md) | Island assembler from kit, seeded | A5 | — (placeholders; T10, T12, T29 swapped in) | DONE |
| [T14](tasks/T14-world-generator.md) | World generator: island graph, gaps, solvability | A5 | T13, T11, T27 | DONE |
| [T15](tasks/T15-game-loop.md) | Game flow: scene wiring, run start/restart, respawn, win/fail screens | A0 | T14, T25 | DONE |
| [T16](tasks/T16-sky-and-atmosphere.md) | Background: skybox, clouds, fog, post-processing | A6 | T01 | DONE |
| [T17](tasks/T17-music-and-ambience.md) | Music + ambience + AudioManager/mixer | A7 | T00 | DONE |
| [T18](tasks/T18-sound-effects.md) | Sound effects set + event hookup | A7 | T00, T04, T11 | DONE |
| [T19](tasks/T19-ui-hud-menus.md) | HUD: hotbar, prompts, timer, pause, win/fail panels | A7 | T05 (soft: T25, T27 — stubbed) | DONE |
| [T20](tasks/T20-growth-vfx.md) | Growth VFX polish (cuttable) | A6 | T11 | DONE |
| [T22](tasks/T22-qa-and-test-seeds.md) | QA pass, regression seeds, bug list | A8 | T15 | DONE |
| [T23](tasks/T23-build-and-performance.md) | Performance pass + standalone build | A8 | T22 | DONE |
| [T24](tasks/T24-demo-prep.md) | README, credits (gallery + demo capture cut: demo is showcased manually) | A0 | all | DONE |
| [T25](tasks/T25-game-modes-timer-scores.md) | Run timer, best-time save (Challenge mode removed later) | A1 | T12 | DONE |
| [T26](tasks/T26-level-select-and-seed-codes.md) | Title + level select, Random Level, seed codes (copy/enter) | A7 | T14, T25 | DONE |
| [T27](tasks/T27-watering-can.md) | Watering can: 2 charges, sprout state, refill at sources | A2 | T04, T11 | DONE |
| [T28](tasks/T28-gn-water-collector.md) | Geometry Nodes: Water Collector plant | A3 | T06 | DONE |
| [T29](tasks/T29-water-features-kit.md) | Water features kit: waterfall + lake | A4 | T10 | DONE |
| [T30](tasks/T30-water-shaders-and-vfx.md) | Water shaders + splash/drip VFX | A6 | T01, T29 | DONE |
| [T31](tasks/T31-player-model-third-person.md) | Player model (blocky, white/black/gold) + third-person camera | A1+A3 | T02, T03 | DONE |
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
| [T67](tasks/T67-plant-and-seed-detail-pass.md) | Plant and seed detail pass: richer bridge / beanstalk / mushroom patch / collector models, plant textures, detailed seeds, parachute as a big leaf | A3+A6 | T49, T56 | DONE |

## Final state (2026-10-26)
- Levels: ten curated (`WorldKitBuilder`, menu 14): 1 First Steps, 2 Beanstalk Hill (autumn), 3 Water from the Sky (rain, collector only), 4 Skyfall (beanstalk then a leaf glide), 5 Stepping Stones (hand plan, one huge tree on island 3), 6 There and Back (sunrise, round trip with the Heart Seed), 7 Snowed In (snow), 8 Windy Summit (spiral, gusts, snow-capped goal island), 9 Into the Mist (fog), 10 Storm Season (rain + gusts). Themes Sunny / Rain / Autumn / Snow / Fog / Sunset / Sunrise with their own sky and weather; goals flag / fruit / gate / campfire. Generated levels use `SeedCode.GeneratorVersion` 3; seed codes are five dash-joined words (old `LEAF-XXXX-XXXX-XX` codes still decode).
- Snow levels: every pond and waterfall is frozen (no refill), soil patches are frozen too; a Heat Seed (E on soil, E near water) thaws one. Exactly two Heat Seeds on the start island; the source regrows them.
- Audio: surface footsteps with bridge creaks (T47); ten gameplay tunes (T53), a menu tune, win music; a level-start jingle; altitude-scaled wind on windy levels.
- Plants: vine bridge, beanstalk ladder (no top platform), mushroom-patch platform, Water Collector; textured grow shader (T49, T67); parachute is a big leaf pickup.
- Title screen: Play, Select level, Generate level, Input seed, Options (volume, reset best times, hints) over a generated showcase world; Select level also lists the Interaction test (one of every item; formerly the Movement course).
- World look: imported CC0 / royalty-free models (Kenney, Quaternius, FreeStylized Ruins; `Game/Assets/ThirdParty/LICENSES.md`), only the ones the game places. T10 (Blender island kit) is CUT; islands are procedural (grass, mud or rock ground; snow on snow levels).
- Cleanup (2026-10-26): unused models, materials, dead code and the T02 test fixture removed; raw Blender exports and the built `.app` are git-ignored (see `docs/task-log.md`).
- Not covered by the MVP (by-hand playthrough, built-player smoke test, listening pass, balance): see `TODO.md`.
