# T23 — Performance pass + standalone build

**Owner:** A8 Tester/Release · **Depends on:** T22
**Status:** REVIEW

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A shippable build that runs smoothly.

## Deliverables
- Profile typical and worst-case (8 islands) scenes; target 60 fps on the dev machine; fix top offenders (batching, shadow distance, particle counts, collider counts)
- Standalone build (macOS/Windows per demo machine) in `Builds/` (git-ignored) with a one-line run note in README
- Build smoke test: title → level 1 → win → back; random level; paste seed code; save file persists best time

## Steps
1. Use Unity Profiler/Frame Debugger via editor tools where possible.
2. Strip unused assets; check texture/audio import settings.

## Acceptance criteria
- [ ] Build launches and is playable start to finish
- [ ] Frame time stable; no console errors in the built player log
- [ ] Best times persist between launches

## Requests (to/from other roles)
_none yet_

## Handoff
`PlantGame > Build Standalone (macOS)` (`Editor/StandaloneBuild.cs`) builds Title + Main to `Builds/Overgrown-mac/Overgrown.app` (git-ignored): 153 MB, 0 errors, report in `Game/Logs/build.txt`. Run: `open Builds/Overgrown-mac/Overgrown.app`. Launched once: starts, player log (`~/Library/Logs/Overgrown/Overgrown/Player.log`) has no errors or missing-script warnings. `PlantGame > Smoke Test Levels` now also logs average ms per frame, draw calls, set-pass calls, triangles and shadow casters: all ten levels 1.6-4.2 ms per frame in the editor, 800-1750 draw calls, up to 645k triangles (heaviest: levels 4, 5, 10), nothing needed fixing. Not done: a by-hand run of title, level 1, win, back, random level, pasted seed code and best-time persistence across launches in the built player; a player-side profile (editor numbers are not player numbers). Fixed on the way: three MonoBehaviours (`IslandTerrain`, `LoopEmitter`, `RuinSounds`) lived in files with other names, so the baked Atmosphere prefab had 19 missing-script references; each now has its own file and the prefab was rebuilt. The editor console's shader warnings (about 700) all come from the `com.unity.ai.inference` package (`Hidden/Sentis/*`, `ConvGeneric`), none from project shaders; that package is a dependency of the Unity AI Assistant that the editor tooling uses, so it stays.
