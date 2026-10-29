# T57 — Touch input layer: virtual stick, look drag, buttons feed PlayerInputReader

**Owner:** A1 Gameplay · **Status:** REVIEW (post-MVP, see `TASKS.md`) · **Depends on:** —

Design: `docs/mobile-controls.md`. Not scheduled for the hackathon weekend.

## Goal
`PlayerInputReader` stays the only place the game reads input from. Add touch sources behind it so `PlayerController`, the interaction system and the HUD need no device checks.

## Work
- Add a touch control scheme to `PlantGameInput.inputactions` (on-screen stick for Move, on-screen buttons for Jump / Use / Water / Pause, a touch-delta Look) using the Input System's On-Screen Controls (`OnScreenStick`, `OnScreenButton`) or small custom controls. No new device polling outside the reader.
- Reader gets: `ClimbAxis` from stick Y, `RunHeld` from stick magnitude past ~0.85, `PickupDropPressed` / `UsePressed` from the context button (T58 decides which), `CycleDelta` / `SlotPressed` from hotbar taps (T60). Remove the direct `Keyboard.current` read in `ClimbAxis` / `CycleDelta` behind a device check.
- Look: right-half drag, sensitivity scaled by DPI, option slider + invert Y (store in `GameSettings`).
- Active-device switching: last-used device decides whether the touch layer is shown (keyboard+mouse, touch, later gamepad). Touch must not lock the cursor; `PlayerController` cursor lock and the click-to-grab-back logic are skipped on touch.
- Multi-touch: stick and look must work at once, buttons while both are held; touches that start on a button never become look drags.

## Acceptance
- With a touch device (Unity Device Simulator, or a real Android phone) the player can walk, run, look and jump with no keyboard/mouse; desktop play is unchanged (regression check).
- No `Keyboard.current` / `Mouse.current` access left in gameplay code outside `PlayerInputReader`/device switching.
- `docs/controls.md` and AGENTS.md 5.8 get a touch column; contract change noted in section 9.

## Handoff
Files: `Player/TouchInput.cs` (per-frame state), `Player/TouchControls.cs` (polled EnhancedTouch layer: floating left stick = walk / run at the edge, right-side drag = look, buttons Jump / Context / Water / Next item / Pause), `PlayerInputReader` merges touch into Move / Look / Jump / Run / Pause / Cycle / ContextPressed, `InteractionSensor.SenseByFacing` (cone auto-focus, 70 deg, hysteresis), `PlayerInteraction.Context / ContextLabel / WaterUseful`, `GameSettings.TouchControls / TouchLookSensitivity / TouchInvertY`.
Test: menu `PlantGame > Touch Controls (editor test)` then play (mouse acts as a finger via TouchSimulation); phones show it automatically. Verified in editor: simulated stick drag walked the player. Not verified: real device, look drag feel, context button in a full level, hotbar tap (T60), tuning (T61).
Known: no long-press drop; items cycle with the Next item button.

## v3 rework (2026-10-27)
The stick, context button and water button were replaced: tap the ground to walk (turn first, then walk), tap the circle / diamond marker to act (`InteractionMarkers.Tappables`, `PlayerInteraction.HandleTap`), tap a beanstalk to climb, double-tap a hotbar slot to drop, jump again in the air to glide. Camera follow is always on for touch. See `docs/mobile-controls.md`; backlog in `TASKS-ANDROID.md`.
