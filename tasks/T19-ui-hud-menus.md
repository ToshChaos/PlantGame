# T19 — HUD: hotbar, prompts, timer, pause, win/fail panels

**Owner:** A7 Sound & UI · **Depends on:** T05 (soft: T25 timer, T27 watering can — stub until they land) · **Estimate:** 3h · **Day:** 2
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Readable, on-style UI. Use the UI skill to detect the framework; default to UI Toolkit or uGUI, whichever the project already uses.

## Deliverables
- **Stubs first (do not wait for T25 / T27):** bind only to the `GameEvents` contract (`CanChanged`, `RunStarted`, `RunFinished`,
  `GameWon`, `GameFailed`, ...) and the `RunSession` / `WateringCan` API names from `AGENTS.md` 5.5. Until those exist, drive the
  widgets from a `HudDebugFeed` (inspector buttons / keys in the sandbox that raise the events with fake values: charges 2→1→0,
  a ticking timer, win/fail). When T25 / T27 land, delete the feed; no HUD code should change.
- Watering can widget beside the hotbar (2 droplet pips, full/empty, flash on refill, shake on dry F) bound to `CanChanged`; hotbar with 4 slots (icons, selected highlight, empty slots), full-inventory shake
- Interaction prompt ('Q Pick up Vine Seed', 'E Plant Vine Seed', 'E Refill watering can', 'F Water sprout'; key letters come from the bound actions) bound to `FocusChanged`
- Run HUD: elapsed time (Exploration: small/optional), countdown (Challenge: large, turns amber < 30 s, red < 10 s), current mode label
- Pause menu: Resume / Restart / Volume sliders / Main menu
- Win panel: time, best time, 'New best!' badge, seed code with Copy button, Retry / Next / Menu. Fail panel: Retry / Menu
- Style: palette, rounded, translucent white glass panels, soft glow cyan accents; readable at 1080p and 720p
- Prefabs under `Prefabs/UI`; no dependencies on scenes

## Steps
1. Mock all panels in a UI sandbox first.
2. Bind to events/properties exactly as in contracts; avoid reaching into other systems.
3. Test with keyboard and mouse; controller navigation is stretch.

## Acceptance criteria
- [ ] All panels display correct data from `RunSession`/`Inventory`
- [ ] Countdown formatting mm:ss.ms, never negative
- [ ] Copy button puts the seed code on the clipboard
- [ ] UI never blocks gameplay input when closed

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
