# T62 — App lifecycle, orientation and screen behaviour on Android

**Owner:** A0 Director · **Status:** DOING (post-MVP, see `TODO.md`) · **Depends on:** —

Design: `docs/mobile-controls.md`. Not scheduled for the hackathon weekend.

## Work
- Landscape-only (`LandscapeLeft/Right` auto-rotate), sleep timeout never while playing, target frame rate 60 (30 fallback option), `Screen.safeArea`.
- `OnApplicationPause` / focus loss: auto-pause the run (Exploration elapsed clock and Challenge countdown both stop), mute music, resume to the pause menu, not mid-air input.
- Back button handling, no accidental quit; save the best times and last seed on pause (`SaveService`), since Android may kill the process.
- Low-battery / thermal: option to cap at 30 fps. Handle audio focus (phone calls).

## Acceptance
- Home button during a Challenge run then returning shows the pause menu with the same remaining time; killing the app does not lose best times.

## Handoff
Done: `Core/MobileLifecycle.cs` (mobile only): landscape left / right auto-rotation, screen never sleeps, 60 fps target, auto-pause of the run on app pause plus `PlayerPrefs.Save()`. Player settings landscape only (`AndroidBuild.Configure`).
Not done: Back button behaviour on a real device, 30 fps option, audio focus, `Screen.safeArea` layout (T59). Not verified on a device.

Update 2026-10-29: 30 fps battery-saver toggle (`GameSettings.Fps30`, touch Options right column). Android Back arrives as Escape (pause toggle). Audio focus left to Unity's default handling.
