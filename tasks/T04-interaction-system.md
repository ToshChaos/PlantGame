# T04 — Interaction: focus, prompt, pick up / drop / use

**Owner:** A1 Gameplay · **Depends on:** T03
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
The player can target things with a forward ray/sphere, see a prompt, pick up / drop (Q), use the selected item or a water source (E), and water sprouts (F), resolved as in `AGENTS.md` 5.8.

## Deliverables
- `Scripts/Core/InteractionSensor.cs` (camera-forward SphereCast, layer `Interactable`). **Third person (T31):** the camera sits
  ~4.5 m behind the player, so cast from `PlayerController.EyeTransform` but measure the 3 m range from the player (e.g. start the
  cast at the point on the camera ray nearest the player's chest, or filter hits by distance to the player).
- `IInteractable` / `IUsableTarget` implementation plumbing, `Prompt` string exposed via an event for the HUD (T19)
- `Scripts/Inventory/WorldItem.cs` (IInteractable with `Action = Pickup`: add to inventory on Q; destroys itself)
- Drop: spawn `ItemDefinition.worldPrefab` 1.2 m in front with small forward impulse, on ground layer; reject drops inside geometry
- Water (F): same sensor, target `IWaterable` within 3 m; spend a charge via `WateringCan.TryConsume()` only if `NeedsWater`; with 0 charges raise `CanEmptyTried`. Refilling is E (Use action) on a `WaterSource` / `WaterCollector` (both `IInteractable`).
- Use: if selected item exists, raycast for `IUsableTarget`; call `UseItem`; else no-op feedback event
- Highlight of focused object (outline or emissive pulse using palette glow cyan)

## Steps
1. Implement sensor and focus highlight; publish `FocusChanged(string prompt)` for UI.
2. Implement `WorldItem`; wire to Inventory (T05 provides the component; use an interface stub until then).
3. Implement drop/use handlers with `GameEvents.ItemDropped` / `ItemUsed`.
4. Test in `Sandbox_Gameplay` with 3 dummy items and a dummy `IUsableTarget`.

## Acceptance criteria
- [x] Prompt appears when looking at an item within 3 m and disappears when not
- [x] Q picks up the focused item; with nothing to pick up (or a full inventory) Q drops the selected item in a sensible spot (never inside terrain)
- [x] E on a valid target triggers `UseItem` exactly once per press; E at a water source refills; F waters
- [x] Nothing throws when inventory is empty or full

## Requests (to/from other roles)
_none yet_

## Handoff
Files: `Scripts/Core/{InteractionSensor,FocusHighlight,PlayerInteraction,SandboxTarget}.cs`, `Scripts/Inventory/WorldItem.cs`, `Scripts/Plants/{IWaterable,WateringCan(stub for T27)}.cs`. Player prefab now carries Inventory, WateringCan, InteractionSensor, PlayerInteraction.
API: `GameEvents.FocusChanged(string)`, `UseRejected`, `InventoryFull`; `PlayerInteraction.{PickupOrDrop,Drop,Use,Water}` are public for tests. Sensor: SphereCast along the camera ray + 14 deg aim assist, range 3 m from the chest, walkable geometry blocks.
Test (30 s): `PlantGame > 5 Build Sandbox_Gameplay`, play: 5 seeds on the floor (z=-4), a grey plant target at (4,-5), two seed trees at (-6/-3, 2). Q picks up, E on the target plants, F waters, Q with 4 full + looking at a 5th raises InventoryFull and drops the selected item.
Verified in play mode via direct calls (focus, pickup, 5th refused, use once per press, F costs 1 charge / 0 -> CanEmptyTried, refill). Real key presses not driven. Known: with a full inventory Q both reports InventoryFull and drops (5.8 reading). Highlight uses a property block (cyan tint + emission pulse), not yet eyeballed in a screenshot.
