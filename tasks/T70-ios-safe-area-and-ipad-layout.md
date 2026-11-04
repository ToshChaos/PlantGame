# T70 — Safe area, home indicator and iPad layout

**Owner:** A7 Sound & UI · **Status:** TODO (see `TASKS-IOS.md`) · **Depends on:** T59, T69

Design: `docs/mobile-controls.md`.

## Why
iOS always draws edge to edge, under the Dynamic Island / notch and the home indicator. No game code reads `Screen.safeArea`; Android only avoids the problem through `renderOutsideSafeArea = false` (`AndroidBuild.Configure`). So T59's safe-area item is still open, although the task is marked DONE. This work helps Android too.

## Work
- Safe-area layout for the HUD, touch controls (Jump, Pause, hotbar), title screens and pause / end panels. Backgrounds still fill the screen; only controls and text move in. Update when the area changes (landscape left <-> right swaps the side of the cutout).
- Home-indicator conflicts: set `deferSystemGesturesMode` for the bottom edge (and sides if needed) so a look drag or hotbar tap does not trigger the system swipe. Try `hideHomeButton` and keep it only if it helps.
- iPad 4:3: check every screen at 2048 x 2732 and 2064 x 2752 proportions. `TitleMenu` already notes a 4:3 tablet is about 1660 wide; check the phone-column home layout, level cards and Options.
- Resizable iPad windows: Apple's TN3192 says `UIRequiresFullScreen` (on in Player Settings) is no longer honoured on iPadOS 26. Layout reads `Screen.width` when it is built and `QualityTier.ScaleToScreen` runs only on a settings change. Rebuild or re-anchor on resize, handle very small windows sensibly, and rescale rendering.
- Touch targets stay at least 44 pt on every device.

## Acceptance
- Simulator screenshots of title, Level 1 HUD, pause and Options, on a Dynamic Island iPhone in both landscape directions and on a 13" iPad: no control or text under the cutout or the home indicator, nothing clipped.
- On a device, repeated look drags from the bottom edge do not leave the game.
- Resizing an iPad window during a run keeps a usable layout (Simulator at minimum, device if available).
- The Android build, with `renderOutsideSafeArea` decided again, still looks right.

## Handoff
