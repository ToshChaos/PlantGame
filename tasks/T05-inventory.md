# T05 — Limited inventory (4 slots) + item definitions

**Owner:** A1 Gameplay · **Depends on:** T04
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
A 4-slot inventory with selection, matching the contract, plus the first item definitions.

## Deliverables
- `Inventory`, `ItemDefinition`, `ItemKind` exactly as in section 5.5
- ScriptableObjects in `ScriptableObjects/Items`: `Item_VineSeed`, `Item_BeanstalkSeed`, `Item_ShelfSeed`, `Item_DewSeed` (Water Collector), `Item_HeartSeed` (optional key item)
- Selection: Tab / Shift+Tab cycle (primary), keys 1–4 and scroll as aliases; `Changed` event for UI; `ItemPickedUp` event
- Full-inventory rule: pickup refused with a clear event (`InventoryFull`) so UI/SFX can react
- Placeholder world prefabs: glowing seed sphere in palette colours (Prefabs/Items)

## Steps
1. Implement data classes and component; unit-test add/remove/select logic (EditMode test in `Tests/`).
2. Create the item assets and temporary world prefabs.
3. Integrate with T04 sensor in sandbox; verify the 5th pickup is refused.

## Acceptance criteria
- [x] Exactly 4 slots; the 5th pickup is refused with feedback
- [x] Tab cycles all 4 slots with wraparound (1–4 and scroll aliases also work)
- [x] Dropping frees the slot; picking up fills the first empty slot
- [x] EditMode tests pass

## Requests (to/from other roles)
_none yet_

## Changes after review
- Mouse wheel / trackpad scroll no longer cycles slots (confusing on laptops). Tab / Shift+Tab and 1-4 remain. `PlayerInputReader.CycleDelta` ignores the `Scroll` action; docs/controls.md and AGENTS.md updated.

## Handoff
Files: `Scripts/Inventory/{Inventory,WorldItem,SeedTree,ItemBob}.cs`, `Editor/ItemBuilder.cs` (menu `PlantGame > 6 Build Items`), items in `ScriptableObjects/Items` (Vine, Beanstalk, Shelf, Dew seeds + Heart Seed key), placeholder prefabs `Prefabs/Items/WorldItem_*`, tests `Tests/EditMode/InventoryTests.cs` (8 pass).
Selection input (Tab / Shift+Tab / 1-4 / wheel) is handled in `PlayerInteraction`. Seed `plant` fields are empty until T11 creates PlantDefinitions. Icons are null until T19.
Extra (user request): `SeedTree` drops seeds at level start (start island, see T12/T14).
