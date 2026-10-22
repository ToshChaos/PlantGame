# T08 — Geometry Nodes: Shelf-fungus platform plant

**Owner:** A3 Plant Smith · **Depends on:** T06 · **Estimate:** 3h · **Day:** 2
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A spiral of broad shelf-fungus / leaf-pad platforms that rises 2–4 m and reaches 3–6 m outward.

## Deliverables
- Node group `GN_Plant_Platform`: inputs `Seed`, `Pads (4–7)`, `RiseTotal`, `Reach`, `PadSize (>=1.5)`, `Wobble`, `StemThickness`
- Central stem with pads spiralling outwards; glowing cyan rim spots; `growth_t` from stem base up and out
- `Anchor`, `Tip`, `Land` empties; `COL_` per pad (flat tops)
- 6 seed screenshots `docs/shots/platform_variants.png`

## Steps
1. Place pads along a helix; clamp step rise <= 1.2 m and horizontal hop <= 2.5 m.
2. Randomise pad rotation/size with the seed but within clamps.
3. Verify jumpability numerically with a script that measures pad-to-pad deltas.

## Acceptance criteria
- [ ] Every seed passes the jump-delta check (rise <= 1.2 m, hop <= 2.5 m)
- [ ] Total reach/rise inside envelope in `AGENTS.md` 5.2
- [ ] Tri count <= 4k

## Fallback / cut plan
Cuttable (see `AGENTS.md` section 8). If cut, world gen only uses Bridge + Ladder.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
