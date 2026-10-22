# T21 — Seed & difficulty tuning, pick curated levels

**Owner:** A0 Director · **Depends on:** T14, T15 · **Estimate:** 2h · **Day:** 3
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Turn the generator into good levels: tune gaps, seed supply, timers and choose the curated levels.

## Deliverables
- Create `LevelDefinition` assets: **Level 1 'Meadow Steps'** (3–4 islands, Bridge only, generous time), Level 2 'Sky Stairs' (Bridge + Ladder), Level 3 'Heartbloom' (all kinds, 6–7 islands) — only Level 1 is required
- Pick seeds by playing: record the seed, par time and challenge time (par x 2 recommended) in `docs/test-seeds.md`
- Tune: water budget (can 2 charges, collector +1/15 s cap 2, how many water islands), challenge time limits including collector waits, plant grow times, gap ranges, decoy rate, island size, jump forgiveness
- Pick 3 'showcase seeds' with the most distinct plant variants for the demo

## Steps
1. Play each candidate seed in Exploration and Challenge; time yourself; choose the seeds that feel good.
2. Feed fixes back as Requests to A5/A2/A3.
3. Freeze the curated list; later generator changes must not alter these (regression through T22).

## Acceptance criteria
- [ ] Level 1 is completable by a new player in 3–6 minutes
- [ ] Each curated level has seed, island count, allowed kinds, time limit, par time
- [ ] 3 showcase seeds recorded

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
