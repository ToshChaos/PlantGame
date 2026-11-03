# T19 — HUD: hotbar, prompts, timer, pause, win/fail panels

**Owner:** A7 Sound & UI · **Depends on:** T05 (soft: T25 timer, T27 watering can — stub until they land)
**Status:** DONE

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

## Already in place (sandbox HUD, `Scripts/UI/PlayerHud.cs`)
- Text colours: orange (`Palette.TextOrange` #FF9A3C) for prompts, warnings and menu buttons; purple (`Palette.TextPurple` #B58CFF) for labels. Keep this scheme in the full HUD.
- Water: empty can shows pulsing orange drops, "Water EMPTY" and "Refill at a lake or waterfall [E]"; trying to water with an empty can flashes the warning (`GameEvents.CanEmptyTried`).
- Dry sprout in focus: "Press [F] to water" (key read from the bound action).
- Key hints panel (bottom right: Q pick up / drop, E use / plant / refill, F water, Tab next slot), **on by default**, toggled by `Hints: On/Off` in the pause menu (`Core/GameSettings.Hints`, PlayerPrefs). T26 options screen should reuse `GameSettings`.

## Timer and end panel (sandbox HUD, done)
- Top-centre timer (count up in Exploration, countdown in Challenge, red under 10 s) read from `RunSession`. On `RunFinished` / `GameFailed` an end panel appears after ~1.8 s / 0.5 s ("Goal reached!" + time + `New best!` or best time; "Time's up"), freezes time, frees the cursor, buttons Play again / Quit. Needs: level-select button, copy seed code, medals.

## Ladder hints (sandbox HUD, done)
- Contextual climb prompts from `PlayerController.CurrentClimbHint` (see T07). W / S / Space are written in the strings (the controls spec fixes them); switch to bound-key lookup if T26 adds rebinding.

## Start menu, goal hint, best time (sandbox HUD, done)
- `PlayerHud` opens a **start menu** when the scene loads (time frozen, cursor free): title, control scheme (key / action columns), the saved **best time** for the current `RunSession.SaveKey` + mode (or "No best time yet") and a **Start** button. Start unfreezes, locks the cursor and calls `RunSession.StartRun` so the clock begins at 0. Restart / Play again skip the menu (`_skipMenu`). Esc is ignored while the menu is open.
- **Goal hint:** top-centre objective line "reach the flag on the farthest island and press [E] - N m away" (live distance to the `GoalFlag`) plus "Grow vines, beanstalks and mushrooms to cross the gaps".
- Best time was already saved by `RunSession.Win` -> `SaveService.TrySubmitTime`; now it is also shown on the menu (the end panel showed it already). T26 replaces this with the real title / level-select; keep the controls block.
- Screenshot: docs/shots/start_menu.png.

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_

- (T15 pass) Added Menu / Retry buttons, Exploration / Challenge toggle on the start menu, Glow toggle, UI click + hover sounds. Remaining for T26: title scene and level cards.
