# T25 — Game modes (Exploration/Challenge), run timer, best-time save

**Owner:** A1 Gameplay · **Depends on:** T12 · **Estimate:** 3h · **Day:** 2–3
**Status:** TODO

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

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
