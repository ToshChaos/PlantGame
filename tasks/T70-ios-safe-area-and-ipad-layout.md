# T70 — Home indicator and iPad layout

**Owner:** A7 Sound & UI · **Status:** DOING (see `TASKS-IOS.md`) · **Depends on:** T59, T69

Design: `docs/mobile-controls.md`.

## Left to do
- Simulator: title, Level 1 HUD and pause in the other landscape direction; Options and level cards on a 13" iPad (2064 x 2752 proportions). Nothing under the home indicator, nothing clipped; touch targets at least 44 pt.
- Device: repeated look drags from the bottom edge do not leave the game.
- Resizing an iPad window during a run keeps a usable layout (the Simulator cannot resize iPad windows here, so this needs a device or a Simulator with window handles).
- Android: decide `renderOutsideSafeArea` again and check that build still looks right.

## Handoff
- `deferSystemGesturesMode = BottomEdge` is set in `IosBuild.Configure`; `ScreenWatcher` (Core) re-fits CanvasScalers and the rendered line count when the size changes, and reloads the title scene when the phone / tablet layout flips.
- Checked on the Simulator: iPhone 18 Pro landscape, the Dynamic Island blocks nothing, so iPhone needs no safe-area layout. iPad Pro 13" in one direction: title, Level 1 HUD and pause fine.
