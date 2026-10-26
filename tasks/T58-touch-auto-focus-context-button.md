# T58 — Touch interaction: auto-focus and the context button

**Owner:** A1 Gameplay · **Status:** REVIEW (post-MVP, see `TODO.md`) · **Depends on:** T57

Design: `docs/mobile-controls.md`. Not scheduled for the hackathon weekend.

## Goal
Replace look-at-crosshair focus with a rule that works without precise aiming, and drive one context button from it.

## Work
- New focus mode in the interaction system used when the touch scheme is active: score interactables, `IUsableTarget` soil patches and `IWaterable` sprouts within 3 m by distance and angle to the player's facing (cone ~70 degrees), prefer the camera-centre target on ties, hysteresis against flicker. Desktop keeps the raycast focus.
- Context button: icon + label from the focus and the selected item (Pick up, Plant, Raise flag, Thaw, Glide, Grab, Drop). Hidden when it would do nothing. Long-press = drop the selected item.
- Water button shows only when it has a use (sprout in reach, source or collector in reach, or dry can message); reuse the `CanEmptyTried` dry feedback.
- Same focus object feeds `PlayerHud` prompts so the text agrees with the button. `GameEvents.FocusChanged` unchanged.
- Soil patch decoys and several soils per gap mean the nearest patch must be the one used; the highlight ring/outline on the focused target is visible (reuse the focus highlight if one exists).

## Acceptance
- Level 1 is completable on touch: pick up a seed, plant it on the nearest soil, water it, cross, with no mis-targets when two soils or two items are within 3 m (test on a layout with a decoy soil).
- The button label never disagrees with what pressing it does.

## Handoff
Files: `Player/TouchInput.cs` (per-frame state), `Player/TouchControls.cs` (polled EnhancedTouch layer: floating left stick = walk / run at the edge, right-side drag = look, buttons Jump / Context / Water / Next item / Pause), `PlayerInputReader` merges touch into Move / Look / Jump / Run / Pause / Cycle / ContextPressed, `InteractionSensor.SenseByFacing` (cone auto-focus, 70 deg, hysteresis), `PlayerInteraction.Context / ContextLabel / WaterUseful`, `GameSettings.TouchControls / TouchLookSensitivity / TouchInvertY`.
Test: menu `PlantGame > Touch Controls (editor test)` then play (mouse acts as a finger via TouchSimulation); phones show it automatically. Verified in editor: simulated stick drag walked the player. Not verified: real device, look drag feel, context button in a full level, hotbar tap (T60), tuning (T61).
Known: no long-press drop; items cycle with the Next item button.
