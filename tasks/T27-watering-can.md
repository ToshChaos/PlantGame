# T27 — Watering can: 2 charges, sprout state, refill at sources

**Owner:** A2 Botanist · **Depends on:** T04, T11
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
Seeds now only sprout; the player's watering can makes them grow. Two charges, refilled at lakes, waterfalls and Water Collector plants. Rules: `AGENTS.md` 5.2b.

## Deliverables
- `WateringCan` on the Player prefab (`MaxCharges = 2`, `Charges`, `TryConsume()`, `Refill(n)`), raising `CanChanged`, `Watered`, `CanRefilled`, `CanEmptyTried`
- `IWaterable` on `PlantInstance`: Sprout state (`_Growth` ~0.08, no colliders) until `Water()`; `PlantDefinition.needsWater` toggle (false for Collector)
- `WaterSource` component (Lake / Waterfall, layer `Water`, unlimited, `IInteractable`: prompt 'Refill watering can' (key F), hidden when the can is full)
- `WaterCollector` component: starts collecting when its plant is grown, +1 stored charge per 15 s, cap 2, drives the `Reservoir` fill level, `IInteractable` hand-over of stored charges, raises `CollectorReady`
- `ScriptableObjects/World/WaterSettings` (max charges, collector rate and capacity, water range) read by all of the above
- Hook F (Water) into the T04 sensor; held visual: small watering-can model in the player's hand (placeholder primitive is fine), pour animation/particle anchor
- Sandbox: sprout, lake, waterfall (primitive meshes OK) and a collector on `Sandbox_Plants.unity`

## Steps
1. Turn on `needsWater` for the three traversal plants and verify the full loop: plant, sprout, water, grow, cross.
2. Implement sources and collector; test the timing with `Time.timeScale` (see the play-mode recipe in `AGENTS.md`).
3. Keep the Day-1 spike behaviour available via a debug toggle `WaterSettings.requireWater`.
4. Document the public API in the Handoff for T19 (UI), T18 (SFX), T14 (validator).

## Acceptance criteria
- [ ] A planted seed stays a sprout until watered; one F press costs exactly one charge
- [ ] Can holds at most 2; refill at a lake or waterfall is instant and unlimited; at full the prompt is hidden
- [ ] F with 0 charges costs nothing and raises `CanEmptyTried`; F on a grown plant costs nothing
- [ ] Collector: grows without water, then +1 charge per 15 s up to 2; E transfers only what the can can hold
- [ ] Water state survives respawn; resets on a new run

## Notes
- Collector plant art comes from T28; use a placeholder pitcher until then.

## Fallback / cut plan
If behind: drop the held-can model and the pour animation (keep the particle).

## Requests (to/from other roles)
_none yet_

## Already in place (sandbox HUD)
- Empty-can indicator, "Press [F] to water" sign and refill hint exist in `PlayerHud` (see T19 notes); T27 should keep firing `GameEvents.CanEmptyTried` / `CanChanged` so they keep working.

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_

## Progress (water pass, day 3)
- Can **starts empty** (user request); first island always has water (or a collector in collector-only levels). Droplet HUD, can model in hand (`WateringCanVisual`: shown while it holds water, water disc when full, tilts when watering).
- `WaterCollector` implemented (1 charge / 20 s, capacity 1, see T28); `WaterSource` has one interact point per pond / stream. Still open: `WaterSettings` ScriptableObject, `requireWater` debug toggle, `IWaterable` on a real SoilPatch (T11), respawn persistence of charges.
