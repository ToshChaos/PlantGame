# T04 — Interaction: focus, prompt, pick up / drop / use

**Owner:** A1 Gameplay · **Depends on:** T03 · **Estimate:** 2h · **Day:** 1
**Status:** TODO

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
The player can target things with a forward ray/sphere, see a prompt, pick up / drop (Q), use the selected item or a water source (E), and water sprouts (F), resolved as in `AGENTS.md` 5.8.

## Deliverables
- `Scripts/Core/InteractionSensor.cs` (camera-forward SphereCast, range 3 m, layer `Interactable`)
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
- [ ] Prompt appears when looking at an item within 3 m and disappears when not
- [ ] Q picks up the focused item; with nothing to pick up (or a full inventory) Q drops the selected item in a sensible spot (never inside terrain)
- [ ] E on a valid target triggers `UseItem` exactly once per press; E at a water source refills; F waters
- [ ] Nothing throws when inventory is empty or full

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
