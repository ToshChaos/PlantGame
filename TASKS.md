# TASKS.md — MVP board (3-day hackathon)

Status: `TODO` · `DOING` · `REVIEW` · `DONE` · `CUT` · `BLOCKED(reason)`. Owners update their own row.
Details, acceptance criteria and handoff notes live in `tasks/T<nn>-*.md`. Roles: see `AGENTS.md` section 4.
Estimates are focused hours for one agent (they include verification).

## Board
| ID | Task | Owner | Depends on | Est | Day | Status |
|----|------|-------|-----------|-----|-----|--------|
| [T00](tasks/T00-project-bootstrap.md) | Project bootstrap, packages, folders, git | A0 | — | 1.5h | 1 | DONE |
| [T01](tasks/T01-art-bible-and-materials.md) | Art bible, palette, shared materials, grow shader | A6 | T00 | 3h | 1 | DONE |
| [T02](tasks/T02-blender-unity-pipeline.md) | Blender → Unity export pipeline & import presets | A0+A3 | T00 | 2h | 1 | DONE |
| [T03](tasks/T03-player-controller.md) | Player controller: WASD, look, jump, climb, camera | A1 | T00 | 3h | 1 | REVIEW |
| [T04](tasks/T04-interaction-system.md) | Interaction: focus, prompt, pick up / drop / use | A1 | T03 | 2h | 1 | TODO |
| [T05](tasks/T05-inventory.md) | Limited inventory (4 slots) + item definitions | A1 | T04 | 2h | 1 | TODO |
| [T06](tasks/T06-gn-vine-bridge.md) | Geometry Nodes: Vine bridge plant | A3 | T00 | 4h | 1 | TODO |
| [T07](tasks/T07-gn-beanstalk-ladder.md) | Geometry Nodes: Beanstalk ladder plant | A3 | T06 | 3h | 1–2 | TODO |
| [T08](tasks/T08-gn-platform-plant.md) | Geometry Nodes: Shelf-fungus platform plant | A3 | T06 | 3h | 2 | TODO |
| [T09](tasks/T09-bake-and-export-variants.md) | Bake 6 variants per plant + proxies + export | A3 | T02, T06 (T07, T08, T28 as they land) | 3h | 2 | TODO |
| [T10](tasks/T10-island-kit.md) | Modular island kit (terrain, ruins, vegetation) | A4 | T02 | 6h | 1–2 | TODO |
| [T11](tasks/T11-growth-system.md) | Growth system: SoilPatch, growth, colliders, climb | A2 | T01, T06 (placeholder), T09 | 5h | 2 | TODO |
| [T12](tasks/T12-world-items-and-goal.md) | Seed pickups, soil patch prefab, Heart Tree goal | A1 | T05, T11 | 2h | 2 | TODO |
| [T13](tasks/T13-island-assembler.md) | Island assembler from kit, seeded | A5 | — (placeholders; T10, T12, T29 swapped in) | 4h | 1–2 | TODO |
| [T14](tasks/T14-world-generator.md) | World generator: island graph, gaps, solvability | A5 | T13, T11, T27 | 4h | 2–3 | TODO |
| [T15](tasks/T15-game-loop.md) | Game flow: scene wiring, run start/restart, respawn, win/fail screens | A0 | T14, T25 | 2h | 3 | TODO |
| [T16](tasks/T16-sky-and-atmosphere.md) | Background: skybox, clouds, fog, post-processing | A6 | T01 | 3h | 2 | TODO |
| [T17](tasks/T17-music-and-ambience.md) | Music + ambience + AudioManager/mixer | A7 | T00 | 2h | 2 | TODO |
| [T18](tasks/T18-sound-effects.md) | Sound effects set + event hookup | A7 | T00, T04, T11 | 3h | 2–3 | TODO |
| [T19](tasks/T19-ui-hud-menus.md) | HUD: hotbar, prompts, timer, pause, win/fail panels | A7 | T05 (soft: T25, T27 — stubbed) | 3h | 2 | TODO |
| [T20](tasks/T20-growth-vfx.md) | Growth VFX polish (cuttable) | A6 | T11 | 2h | 3 | TODO |
| [T21](tasks/T21-level-tuning.md) | Seed & difficulty tuning, pick showcase seeds | A0 | T14, T15 | 2h | 3 | TODO |
| [T22](tasks/T22-qa-and-test-seeds.md) | QA pass, regression seeds, bug list | A8 | T15 | 3h | 3 | TODO |
| [T23](tasks/T23-build-and-performance.md) | Performance pass + standalone build | A8 | T22 | 2h | 3 | TODO |
| [T24](tasks/T24-demo-prep.md) | PlantGallery showcase, README, credits, demo capture | A0 | all | 2h | 3 | TODO |
| [T25](tasks/T25-game-modes-timer-scores.md) | Game modes (Exploration/Challenge), run timer, best-time save | A1 | T12 | 3h | 2–3 | TODO |
| [T26](tasks/T26-level-select-and-seed-codes.md) | Title + level select, Random Level, seed codes (copy/enter) | A7 | T14, T25 | 3h | 3 | TODO |
| [T27](tasks/T27-watering-can.md) | Watering can: 2 charges, sprout state, refill at sources | A2 | T04, T11 | 3h | 2 | TODO |
| [T28](tasks/T28-gn-water-collector.md) | Geometry Nodes: Water Collector plant | A3 | T06 | 3h | 2 | TODO |
| [T29](tasks/T29-water-features-kit.md) | Water features kit: waterfall + lake | A4 | T10 | 3h | 2 | TODO |
| [T30](tasks/T30-water-shaders-and-vfx.md) | Water shaders + splash/drip VFX | A6 | T01, T29 | 2h | 2–3 | TODO |
| [T31](tasks/T31-player-model-third-person.md) | Player model (blocky, white/black/gold) + third-person camera | A1+A3 | T02, T03 | 2h | 1 | REVIEW |

## Parallel lanes (who can work at the same time)
- **Lane A – Player (Unity):** T03 → T31 → T04 → T05 → T12 → T25
- **Lane B – Plants (Blender):** T06 → T07 / T08 / T28 → T09 → (hands off to T11)
- **Lane C – Kit (Blender):** T10 → T29 → (real pieces replace T13's placeholders)
- **Lane D – Systems (Unity):** T11 (placeholder plants first) → T27 → T14 → T15; **T13 (A5) runs in parallel from Day 1 PM on placeholder kit**
- **Lane E – Look & sound (Unity):** T01 → T16 → T20 · T30 · T17 · T18 · T19 → T26

Critical path: T00 → T02 → T06 → T09 → T11 → T27 → T14 → T15 → T21/T22 → T23 → T24.
De-risk early: **T11 must run with a placeholder plant (a stretched cube with the grow shader) on Day 1 evening**, so the
mechanic is proven before final Blender art arrives.

## Schedule
| Slot | Goal | Tasks |
|------|------|-------|
| **Day 1 AM** | Project alive, pipeline proven, player moves | T00, T01 (start), T02, T03, T31, T06 (start), T10 (start) |
| **Day 1 PM** | Interact + inventory, first procedural plant in Unity | T04, T05, T06, T07, T10, T11 spike with placeholder, T13 (start, placeholder kit) |
| **Day 1 end — checkpoint** | Walk, pick up a seed, plant it, a placeholder bridge grows and can be crossed | |
| **Day 2 AM** | Real plants + kit land in Unity; growth system done | T08, T09, T11, T12, T10, T13, T16, T27, T28, T29, T30 |
| **Day 2 PM** | Islands assemble from seed; world generates solvable layout; modes + timer begin | T13 (swap in real kit), T14, T17, T18, T19, T25 |
| **Day 2 end — checkpoint** | Press play: seeded world, crossable with all plant kinds, music + HUD in | |
| **Day 3 AM** | Game flow, level select + seed codes, tuning, QA | T15, T26, T21, T22, T20, T18 |
| **Day 3 PM** | Build, showcase, demo | T23, T24, polish, freeze 2h before demo |

## Checkpoint gates (A0 runs these)
- **G1 (Day 1 end):** player moves/jumps, picks up and drops, plants placeholder, crosses it. If not: drop T07/T10 scope.
- **G2 (Day 2 end):** same seed gives same world twice; 3 different seeds are all beatable; audio and HUD present; the watering can works (sprouts need water, lake/waterfall refill); a timer runs and a win records a best time.
  If not: cut per `AGENTS.md` section 8.
- **G2.5 (Day 3 AM):** from the title screen: play curated Level 1 in both modes, start a Random Level, copy its seed code and re-enter it to get the identical world.
- **G3 (Day 3, T-2h):** feature freeze; only bug fixes, build, demo.

## Risks & mitigations
| Risk | Mitigation |
|------|-----------|
| Geometry Nodes don't run in Unity | Bake variants per seed in Blender (T09); Unity picks a variant by seed. Keep `plants.blend` open in the demo to show the node graph. |
| Growth of baked meshes looks cheap | Vertex-colour `growth_t` + shader reveal and tip scale-in (T01/T11); VFX in T20 hides seams. |
| Unsolvable generated worlds | T14 places the required seed before each gap and validates by simulated reachability; T22 regression-tests 20 seeds. |
| Scene merge conflicts | Only A0 edits `Main.unity`; everything else is prefabs + sandbox scenes. |
| Audio assets unavailable offline | T18 includes a python synth fallback for SFX; T17 lists CC0 sources + procedural pad fallback. |
| Seed codes break when generator changes | `SeedCode.GeneratorVersion` in the code; T22 keeps golden codes in `docs/test-seeds.md` and re-checks them after every world-gen change. |
| Water adds scope (~11h) and a second resource to balance | Day-1 spike skips water (`needsWater` off). T28 is cuttable: the generator then puts a lake/waterfall on every island. Water values are data in `WaterSettings`, tuned in T21. |
| Time | Cut line in `AGENTS.md` section 8; gates above. |
