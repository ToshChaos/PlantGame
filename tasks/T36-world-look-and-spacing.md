# T36 — World look and spacing pass

**Owner:** A5 + A6 + A7 · **Depends on:** T14, T35
**Status:** DONE

## Done
- **Trees:** kind 6 and autumn levels use the textured Quaternius oaks with a tinted leaf material (`Art/Materials/M_LeafTint`, `T_LeafGrey`, built by `LeafTintBuilder`, menu 16): green 28 %, red / yellow / orange, purple 4 %; autumn levels skip green and purple. Pines and cypresses stay green (the flat orange Kenney cones are gone). More trees (13 per area unit instead of 9); islands with water get 1-2 big trees (about 5.6-7.2 m).
- **Parachute plant:** `Scenery.CanopyMesh` / `LeafPlant`: three large green leaves shaped like open parachute canopies (gores in two greens, scalloped edge).
- **Waterfall:** the stream over the lip is carved like a pond (same water material as the ponds), `PlantGame/WaterfallSheet` shader for the falling sheet (streaks, foam at the lip), lip foam particles, fewer droplets. Rounded rim no longer counts as a hollow for water (`IslandTerrain.Bed`).
- **Island edges:** rounded shoulder, a tall, nearly vertical rim wall, deeper underside (`6.5 + (r-8)*0.8` m). More terrain relief (`Amp` 0.9); the ground under every soil is flattened.
- **Height and tilt:** bridge ends 3.2 m lower to 1.2 m higher (the plant slopes up to 1.5 m, `SandboxTarget.bridgeRise`); `LevelLayout.TiltFor`: about 40 % of islands without water, not start or goal, lean 4-15 degrees (`MaxTiltDegrees`); the layout measures soil and landing heights from the tilted surface and the generator rotates the island at the end.
- **Spacing:** `LevelLayout.Clear`: every island keeps 8.5 m (`MinIslandGap`) from every earlier island except its parent, so no accidental jumps. Bridge gaps are 8.6-11.6 m and the plant is stretched to fit (`SandboxTarget.bridgeReach`); platform gaps 4.5 m. Validate Seeds: 0 invalid of 400 (20 re-rolls), all six curated levels lay out without problems.
- **Sound:** `Audio/SeedAmbience` (3D loop `Resources/Audio/seed_tree_ambience.wav`, made by `docs/audio/make_audio.py`) on every seed tree / patch; the title scene plays no ambience (music only, and only if Music is on).

## Known issues / ideas
- "Big trees need water" is implemented as: they only grow on islands that have water. If it should mean the player has to water them, that is not built.
- "Tilt up to 15 %" was read as up to 15 degrees; change `LevelLayout.MaxTiltDegrees` if 15 % grade (8.5 degrees) was meant.
- Not played through: tilted soils and ladders, stretched bridges, the 8.5 m rule on the hand-built level 5 (layout is clean, gameplay unchecked).
