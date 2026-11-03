# T20 — Growth VFX polish (cuttable)

**Owner:** A6 Tech Artist · **Depends on:** T11
**Status:** DONE

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
- **Files:** `Scripts/Plants/GrowthVfx.cs` (in the Main `Systems` object). Runtime-built particle systems with one generated soft-dot material (no prefabs, so `VFX_*` are names of the spawned objects, not assets).
- Effects: seed sprout burst, watering droplets, growth-front emitter following the plant path (anchor -> tip, arched for bridges), grown burst at the tip + base, pickup sparkle, win petals. Palette cyan / white / green.
- **Verified:** compiles, runs through win/fail with a clean console; visuals not yet inspected by screenshot. Cost not profiled.
