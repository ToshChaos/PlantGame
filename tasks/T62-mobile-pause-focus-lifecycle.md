# T62 — App lifecycle, orientation and screen behaviour on Android

**Owner:** A0 Director · **Status:** TODO · **Depends on:** —

Design: `docs/mobile-controls.md`. Not scheduled for the hackathon weekend.

## Work
- Landscape-only (`LandscapeLeft/Right` auto-rotate), sleep timeout never while playing, target frame rate 60 (30 fallback option), `Screen.safeArea`.
- `OnApplicationPause` / focus loss: auto-pause the run (Exploration elapsed clock and Challenge countdown both stop), mute music, resume to the pause menu, not mid-air input.
- Back button handling, no accidental quit; save the best times and last seed on pause (`SaveService`), since Android may kill the process.
- Low-battery / thermal: option to cap at 30 fps. Handle audio focus (phone calls).

## Acceptance
- Home button during a Challenge run then returning shows the pause menu with the same remaining time; killing the app does not lose best times.

## Handoff
_(fill in when done)_
