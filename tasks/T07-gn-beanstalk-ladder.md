# T07 — Geometry Nodes: Beanstalk ladder plant

**Owner:** A3 Plant Smith · **Depends on:** T06 · **Estimate:** 3h · **Day:** 1–2
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A climbable beanstalk generator: a thick twisting stalk with rungs/leaf-steps the player can climb, 4–9 m high.

## Deliverables
- Node group `GN_Plant_Ladder`: inputs `Seed`, `Height (4–9)`, `Twist`, `StalkRadius`, `RungSpacing (~0.5)`, `LeafSize`, `Sway`
- Stalk + two guide vines, rung leaves/knots every ~0.5 m, a top leaf platform to step off, `growth_t` along height (>= 24 segments)
- `Anchor`, `Tip`, `Land` empties; `CLIMB_idx` trigger boxes covering the climbable column; optional `COL_` for the top platform
- 6 seed screenshots `docs/shots/ladder_variants.png`

## Steps
1. Reuse utilities from T06.
2. Ensure there is clearance of >= 0.8 m in front of the stalk for the player.
3. Verify `growth_t`, empties and triggers; export test via T02 scripts.

## Acceptance criteria
- [ ] Seeds produce distinct but equally climbable ladders
- [ ] Height input maps to real metres within +/-0.2 m
- [ ] Top platform gives a stable exit
- [ ] Tri count <= 4k

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
