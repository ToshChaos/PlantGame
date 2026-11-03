# T33 — Seed visuals: distinct look per seed (world pickup, hotbar icon, seed trees)

**Owner:** A1 Gameplay (items, HUD) + A5 Cartographer (generated seed sources) · **Depends on:** T05, T12, T14
**Status:** DONE

> Requested by the user on day 3: every seed should be recognisable by shape and colour, in the world and in the hotbar.

## Goal
| Seed (item id) | Looks like | Colour |
|---|---|---|
| Vine Seed (`vine_seed`, bridge) | a piece of **bamboo** (jointed stick) | bright green |
| Beanstalk Seed (`beanstalk_seed`, ladder) | a **bean** (kidney shape) | purple |
| Shelf Seed (`shelf_seed`, platform) | a **puffball mushroom** (round cap on a short stalk) | white |
| Dew Seed (`dew_seed`, collector) | a fat-leaved **succulent** (rosette, looks like a flower) | teal |
| Parachute Leaf, Heart Seed | unchanged (yellow leaf, gold sphere) | |

The same shape is used for the 3D pickup (`ItemBuilder`) and a procedural hotbar sprite (`UiSprites`). The generated seed sources
(`WorldGenerator.SeedSource`, `Scenery` patches) grow the matching plant and carry the seed shape as fruit:
bamboo grove (was a leafy tree) for the vine seed, bean poles with purple beans, puffball cluster for the shelf seed, succulent rosettes for the dew seed.

## Deliverables
- `Scripts/World/SeedMeshes.cs`: procedural meshes (bamboo, bean, puffball, succulent).
- `Editor/ItemBuilder.cs`: new colours, pickup prefabs from those meshes (`Art/Models/Items/Seed_*.asset`).
- `Scripts/UI/UiSprites.cs` + `PlayerHud`: hotbar icons per item id.
- `Scripts/World/Scenery.cs`, `WorldGenerator.SeedSource`: seed sources adjusted.

## Acceptance criteria
- [x] Each seed pickup has its shape and colour in the world
- [x] Hotbar icon matches the colour and reads as the shape
- [x] Seed sources on generated islands match their seed; drops still work
- [x] Console clean

## Handoff
- Shapes: `SeedMeshes` (bamboo, bean, puffball, succulent). Rebuild pickups with `PlantGame > 6 Build Items`; meshes land in `Art/Models/Items/Seed_*.asset`. Colours in `Palette` (`SeedBamboo`, `SeedBean`, `SeedSucculent`; puffball = cloud white).
- Hotbar: `UiSprites.ForItem(id)` (Bamboo, Bean, Puffball, Succulent), tinted by the pickup material colour. Previews: `docs/shots/seed-pickups.png`, `seed-icons.png`, `seed-sources.png`.
- Seed sources: vine seed = bamboo grove (replaces the leafy tree), beanstalk = poles with hanging purple beans, shelf = log with puffballs, dew = teal succulent rosettes; fruit reuses the pickup mesh.
- Checked by rendering the pickups and sources in the editor; not played through a full level.
