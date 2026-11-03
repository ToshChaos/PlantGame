# T42 — Vague goal hints, goal-island cue, no distance indicator

**Owner:** A7 + A1 · **Depends on:** T15, T19
**Status:** DONE

## Goal
- **Level goal hints become vague**: e.g. "Open the gate", "Reach the flag", "Find the fruit", "Light the campfire" per goal kind, with no directions or coordinates. Applies to the level-select description, intro and HUD.
- **Remove the goal distance indicator** from the HUD.
- **Visual cue when you are on the goal island**: e.g. a soft pulse or glow on the goal, a light beam, sparkles or a subtle HUD/vignette shift; it must not show before the player arrives.

## Acceptance
- No distance number or arrow anywhere in the HUD.
- Entering and leaving the goal island toggles the cue (check all four goal kinds: flag, fruit, gate, campfire).
- Hint strings live in one place and are easy to edit.

## Handoff
Hint strings live in `GoalFlag.Objective` (vague: Reach the flag / Find the golden fruit / Open the gate / Light the campfire / Find the Heart Seed / Bring the Heart Seed home). HUD no longer shows a distance. `GoalFlag.Update` fades in a cyan point light (pulse) when the player is within 14 m and the requirement is met. Level 6 description made vague. Compiles; cue not yet observed in play mode for all four goal kinds.
