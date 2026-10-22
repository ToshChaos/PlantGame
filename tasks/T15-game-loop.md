# T15 — Game flow: scene wiring, run start/restart, respawn, win/fail screens

**Owner:** A0 Director · **Depends on:** T14, T25 · **Estimate:** 2h · **Day:** 3
**Status:** TODO

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
_Files created · prefab/scene names · public API · 30-second test · known issues_
