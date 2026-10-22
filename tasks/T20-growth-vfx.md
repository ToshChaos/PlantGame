# T20 — Growth VFX polish (cuttable)

**Owner:** A6 Tech Artist · **Depends on:** T11 · **Estimate:** 2h · **Day:** 3
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Make growing magical: pollen/sparkles along the growth front, ground ripple at the seed, little burst when done.

## Deliverables
- Particle prefabs: `VFX_SeedPlant`, `VFX_GrowthFront` (follows `_Growth` position along the path), `VFX_Grown` (blossom burst), pickup sparkle, win confetti-petals
- Hook-ups through `GameEvents` or `PlantInstance` callbacks
- Palette-correct colours (cyan, white, soft green)

## Steps
1. Add to `PlantInstance` via a small optional component so T11 stays clean.

## Acceptance criteria
- [ ] VFX visible but never obscure the path; < 0.5 ms cost
- [ ] Works for all three plant kinds

## Fallback / cut plan
First feature to cut.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
