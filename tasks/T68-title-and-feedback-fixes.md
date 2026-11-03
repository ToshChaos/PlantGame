# T68 — Title crystals, watering animation, mushroom sprout

**Owner:** A0 · **Status:** DOING (code changed; rebuild and visual check still open) · **Depends on:** T67

## Goal
- No floating crystals on the title screen. The baked `Atmosphere` prefab still held four `Crystals*` clusters; `Scenery.SkyDebris` no longer makes them, and the Title builder now deletes any `Crystals*` piece.
- The watering animation plays on the last charge: `WateringCanVisual` hid the can the frame the charge hit 0, before the pour started.
- The white sprout mushroom of the mushroom patch is smaller (`plants_platform.py`, puffball and post). "See through" mushrooms: not reproduced yet, to look at after the rebuild.

## Still to do (needs the Unity editor open and Blender connected)
1. `PlantGame > 11 Build Atmosphere`, `17 Build Title`, `12 Build Main`; check the Title shows no crystals.
2. Re-run `plants_platform.py` `build_all()`, export and sync; look at the sprout and the grown patch for see-through caps.
3. Play: water with the last charge and watch the can tip.
