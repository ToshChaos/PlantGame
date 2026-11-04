# T70 — Home indicator and iPad layout

**Owner:** A7 Sound & UI · **Status:** DOING (code in; Simulator checks partly done, device open) (see `TASKS-IOS.md`) · **Depends on:** T59, T69

Design: `docs/mobile-controls.md`.

## Why
iOS draws edge to edge. **Checked 2026-11-04 on the Simulator (iPhone 18 Pro, landscape): the Dynamic Island blocks nothing**, so no safe-area layout is needed on iPhone (an earlier note here, that it covered the Menu button, was wrong). What is left is the home-indicator gesture, iPad 4:3 and resizable iPad windows.

## Work
- Safe area: no work unless a screen shows a control under the cutout (check the other landscape direction and iPad when you are there).
- Home-indicator conflicts: set `deferSystemGesturesMode` for the bottom edge (and sides if needed) so a look drag or hotbar tap does not trigger the system swipe. Try `hideHomeButton` and keep it only if it helps.
- iPad 4:3: check every screen at 2048 x 2732 and 2064 x 2752 proportions. `TitleMenu` already notes a 4:3 tablet is about 1660 wide; check the phone-column home layout, level cards and Options.
- Resizable iPad windows: Apple's TN3192 says `UIRequiresFullScreen` (on in Player Settings) is no longer honoured on iPadOS 26. Layout reads `Screen.width` when it is built and `QualityTier.ScaleToScreen` runs only on a settings change. Rebuild or re-anchor on resize, handle very small windows sensibly, and rescale rendering.
- Touch targets stay at least 44 pt on every device.

## Acceptance
- Simulator screenshots of title, Level 1 HUD, pause and Options, on a Dynamic Island iPhone in both landscape directions and on a 13" iPad: nothing under the home indicator, nothing clipped (the cutout was already checked fine on iPhone).
- On a device, repeated look drags from the bottom edge do not leave the game.
- Resizing an iPad window during a run keeps a usable layout (Simulator at minimum, device if available).
- The Android build, with `renderOutsideSafeArea` decided again, still looks right.

## Handoff
- 2026-11-04: `deferSystemGesturesMode = BottomEdge` set in `IosBuild.Configure`. `ScreenWatcher` (Core) re-fits CanvasScalers and the rendered line count when the size changes and reloads the title scene when the phone / tablet layout flips. iPad Pro 13" Simulator, landscape (one direction): title, Level 1 HUD and pause look right, nothing clipped, no Quit. Not done: the other landscape direction, Options on iPad, a real window resize (the Simulator cannot resize iPad windows; needs a device or iPadOS 26 Simulator window handles), device check of the bottom-edge swipe, Android `renderOutsideSafeArea` re-decision.
