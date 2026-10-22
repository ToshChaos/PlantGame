# T18 — Sound effects set + event hookup

**Owner:** A7 Sound & UI · **Depends on:** T00, T04, T11 · **Estimate:** 3h · **Day:** 2–3
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Satisfying, soft, chimey, organic SFX wired to `GameEvents`.

## Deliverables
- SFX list: footstep (grass, stone, plant) x3 variants, jump, land, pickup, drop, inventory full, select slot, seed plant, growth loop (rustle) + 'grown' chime, water: pour, refill splash, dry-can click, collector drip + 'water ready' bell, climb rustle, Heart Tree win, fail sting, UI hover/click, timer warning (last 10 s tick)
- `Scripts/Audio/SfxBinder.cs` listens to `GameEvents` and plays through `AudioManager`; random pitch +/-5% and variation
- Footstep surface via physics material / layer or tag (`Plant`, `Ground`)
- Asset source: CC0 packs, or **synth fallback** `docs/audio/make_sfx.py` (numpy: filtered noise + sine chimes, ADSR) generating wavs into `Audio/SFX`

## Steps
1. List missing events to A1/A2 as Requests (e.g., `InventoryFull`).
2. Implement binder; test all events from the Sandbox scenes.
3. Mix levels: SFX under music, important feedback (pickup, grown) on top.

## Acceptance criteria
- [ ] Every listed event produces a sound; none are harsh or clipping
- [ ] No sound spam (rate limit footsteps/rustle)
- [ ] Credits updated / scripts committed

## Fallback / cut plan
Cut: surface-specific footsteps, timer tick.

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
