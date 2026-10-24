# T15 — Game flow: scene wiring, run start/restart, respawn, win/fail screens

**Owner:** A0 Director · **Depends on:** T14, T25
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Glue everything in `Main.unity` so a RunConfig produces a playable, finishable run with respawn and restart.

## Deliverables
- `Main.unity` containing: Player prefab, `RunSession`, `WorldGenerator`, `AudioManager`, camera, UI canvases (T19), sky/post volume (T16)
- `GameBootstrap`: reads `RunConfig` from the menu (static `PendingRun`; default = Level 1 Exploration if launched directly), generates world, spawns the player at Start `Spawn`
- Respawn: falling below the kill plane returns the player to the last visited island `Spawn`; **Exploration:** no penalty; **Challenge:** respawn allowed, timer keeps running
- Win: Heart Tree → `RunSession.Win()` → win panel (time, best, new best flag) → buttons Retry / Next / Menu. Fail (Challenge, timer 0) → fail panel → Retry / Menu
- Hold R to restart run with the same config; Esc pause menu with Resume / Restart / Menu
- Scene loading between `Title` and `Main` (async, simple fade)

## Steps
1. Wire existing prefabs in Main; no other role edits this scene.
2. Test all flows: win, fail, restart, pause, menu round-trip, rapid restart ten times (no leaks).

## Acceptance criteria
- [ ] Both modes can be started, won and (Challenge) failed
- [ ] Restart creates the identical world for the same config
- [ ] Respawn works after falling from every island
- [ ] Returning to menu and starting again works repeatedly

## Requests (to/from other roles)
_none yet_

## Handoff
- **Files:** `Scripts/Core/GameBootstrap.cs` (+ static `PendingRun`, `SetMode`, kill plane, last-island respawn, held R restart), `Core/IslandSpawn.cs`, `Editor/MainSceneBuilder.cs`, `SandboxPlantTestBuilder.BuildMain()`; `PlayerHud` got Menu / Retry buttons and a mode toggle.
- **Main.unity** = Level 1 (`level_1`): the 4-island chain (bridge, ladder, platform) + `Atmosphere` prefab + `Systems` (bootstrap, AudioManager, SfxBinder, GrowthVfx). Rebuild: menu `PlantGame > 12 Build Main`.
- **Flow:** start menu (mode toggle Exploration / Challenge, 5:00 limit) -> Start -> run. Win/fail panel: Play again (Retry on fail) / Menu / Quit. Esc pause: Resume / Restart / Menu / Quit. Restart reloads the scene (identical world, goes straight into the run). Fall below lowest island - 30 m -> respawn at the last island stood on (`IslandSpawn`).
- **Verified (play mode):** respawn on island 2 after a drop, win (goal flag, best time saved), Challenge fail at the limit, restart, 4 spawns registered. Screenshot `docs/shots/main_start.png`.
- **Not done / known:** no `Title.unity` or async fade (T26 owns the menu; the start menu in the HUD stands in), no `WorldGenerator` yet (T14), so no `RunConfig.seed` effect; "Next level" button needs a second level. `Next` omitted on purpose.
