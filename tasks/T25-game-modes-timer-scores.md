# T25 — Game modes (Exploration/Challenge), run timer, best-time save

**Owner:** A1 Gameplay · **Depends on:** T12
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Two ways to play and a persistent record of how fast you were.

## Deliverables
- `GameMode`, `RunConfig`, `RunSession`, `SaveService` exactly as in `AGENTS.md` 5.5
- **Exploration:** no fail state; timer counts up silently (HUD optional); falling respawns without penalty
- **Challenge:** countdown from `timeLimit`; at 0 → `GameFailed`/`RunSession.Fail()`
- Timer pauses with the pause menu; ignores loading; uses unscaled time deliberately; precise to ms
- On win: elapsed time submitted to `SaveService.TrySubmitTime(key, mode, seconds)`; returns new-best flag; raises `RunFinished(seconds, newBest)`
- Save file: `persistentDataPath/save.json` with version, best times per key+mode, last seed code, audio settings; tolerant to corrupt/missing file
- Keys: curated `levelId`, or the `SeedCode` string for random/code levels (so highscores follow the seed!)
- EditMode tests for SaveService (new best, worse time, corrupt file) and RunSession state machine

## Steps
1. Implement RunSession state machine: Idle → Running → (Won | Failed) → Idle.
2. Implement SaveService with atomic write (temp file + move).
3. Expose properties for T19 HUD; document in Handoff.
4. Test with the hand-built level from T12.

## Acceptance criteria
- [ ] Challenge fails exactly when the countdown reaches zero; Exploration never fails
- [ ] Best time is saved and survives restart; slower times do not overwrite
- [ ] Pausing stops the clock
- [ ] Corrupt save file does not crash the game (backed up and reset)

## Requests (to/from other roles)
_none yet_

## Already in place (sandbox, `Scripts/Core`)
- `RunSession` (Idle / Running / Won / Failed, `Elapsed`, `Remaining`, `StartRun`, `Win`, `Fail`, `Restart`; unscaled clock that stops while `timeScale` is 0; Challenge fails at 0), `SaveService` (JSON `save.json` in persistentDataPath, atomic write, tolerant to a bad file, `TrySubmitTime` / `BestTime` / `SetLastSeedCode`), `GameEvents.GameWon / RunFinished / GameFailed / RunStarted` raised. `Sandbox_PlantTest` has a `RunSession` (Exploration, levelId `sandbox_plants`) that starts itself.
- Verified in play mode: timer runs, E on the flag wins, best time saved (`New best!`). Still to do here: EditMode tests for SaveService / RunSession, audio settings in the save, Challenge UI polish, wiring `RunConfig` from the menu (T26), `GameFailed` retry flow.

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_

## Handoff (day 3)
- Per level and mode best times (`SaveService`, key = levelId or seed code), reset-all in Options and the in-game start menu. Still open: EditMode tests for SaveService / RunSession, audio settings in the save file.
