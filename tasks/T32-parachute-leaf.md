# T32 — Parachute leaf: a plant that grows glide leaves (one-time use)

**Owner:** A1 Gameplay (glide state) + A2 Botanist (plant/leaf item) + A3 Plant Smith (Blender variant) · **Depends on:** T05, T11, T27
**Status:** DONE

> Read `AGENTS.md` first. Requested by the user on day 1. Interpretation below; confirm with the user if it is wrong.

## Goal
A certain plant (working name **Parachute Bloom**, seed item `Item_ParachuteSeed`) grows large leaves. The player picks a leaf (Q), carries it in an inventory slot,
and uses it **mid-jump** to glide down slowly. **Single use:** the leaf is consumed when the glide ends (landing, or cancelled).

## Design decisions (proposed)
- Plant grows like the others (sprout -> water -> grown, `PlantKind.Glider` added to the enum; seed code `kindsMask` is 4 bits, so Glider takes the last bit and Collector shares / is re-mapped: decide in T14/T26).
- A grown plant offers a few **Leaf** pickups (`WorldItem`, kind `Misc`, `Item_ParachuteLeaf`), e.g. 2 per plant, spawned at `Socket` empties on the mesh; they do not regrow.
- **Use:** while Airborne (not Grounded/Climbing) with a Leaf selected, press **E** (alternative: hold Space while falling) -> state `Gliding`: fall speed capped at ~2.5 m/s, air control 8 m/s horizontal, leaf model above the head. Ends on landing, on Space/E again (cancel) or when the leaf runs out of glide time (~6 s). The leaf is removed from the inventory on **activation**, so it can never be reused.
- Does not break solvability: the world generator treats glide as a bonus only (never required), the validator ignores it.

## Deliverables
- `PlayerController`: new state `Gliding` (+ `GameEvents.GlideStarted/GlideEnded`), tunables `glideFallSpeed`, `glideAirSpeed`, `glideMaxSeconds`.
- `Item_ParachuteLeaf` + `Item_ParachuteSeed` (ScriptableObjects), world prefabs, `PlantKind.Glider`, `Plant_Glider_01..06` Blender variants (Geometry Nodes, leaves as separate `Leaf_<idx>` empties).
- Leaf use hook in `PlayerInteraction.Use()` (selected item `kind == Misc` and `IGlideItem`), HUD hint "Glide" (T19), SFX hook (T18).
- Docs: `docs/controls.md` row, `AGENTS.md` change log (v1.6) once agreed.

## Acceptance criteria
- [ ] Leaf can be picked up from a grown Parachute Bloom and sits in a slot
- [ ] E in mid-air with a leaf slows the fall to the glide speed; model shows the leaf
- [ ] The leaf is gone after use (also when the glide is cancelled); a second E does nothing
- [ ] E on the ground with a leaf does not consume it
- [ ] Jump + glide with the 4.8 m jump never makes a level unwinnable or skips a required plant

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_

## Handoff (day 3)
- Shipped as a leaf plant + glide, no Blender plant: item `parachute_leaf`, `PlayerController.TryStartGlide` (E in mid-air, fall cap 2.5 m/s, ends on landing / Space), `PlantKind.Glider` gap type for curated sequences (Skyfall: beanstalk up, then a 19 m gap down about 9 m). The leaf plant (`Scenery.LeafPlant`: tall stalk, big leaves, glow) stands on the island you glide from.
- Open: a real Parachute Bloom plant grown from a seed, and a glide VFX / sound.
