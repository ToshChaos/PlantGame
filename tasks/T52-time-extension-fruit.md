# T52 — Time-extension fruit (Challenge mode)

**Owner:** A1 + A5 + A7 · **Depends on:** T25, T14
**Status:** DONE

## Goal
A collectible fruit that adds seconds to the Challenge countdown when walked into. Glows warm gold, chimes, shows "+N s" in the HUD and flashes the timer. Only spawns in Challenge mode (never in Exploration) and never on the start or goal island. Count: one per three islands, rounded up; each fruit is worth 30 s per island of the level divided by the count, deterministic from the seed.

## Acceptance
- `RunSession` gets `AddTime(seconds)`; `GameEvents` gets a `TimeAdded` event; the HUD timer flashes.
- Generated levels place fruits clear of lanes, soil and goals; the validator ignores them (never required).
- Not saved into best times unfairly: the best time is the elapsed time as before.

## Handoff
`World/TimeFruit.cs` (built by `WorldGenerator`, Challenge mode only, islands 1..n except the goal, own random), `RunSession.AddTime` / `BonusTime`, `GameEvents.TimeAdded`, HUD timer flashes green and shows a +15 s hint, bell via `SfxBinder`. Best time stays elapsed time. Not played through.
