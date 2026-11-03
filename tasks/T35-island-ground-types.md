# T35 — Island ground types and grass texture

**Owner:** A6 Tech Artist + A5 · **Depends on:** T01, T13
**Status:** DONE

## Goal
Islands should not all look alike: some have less grass (mud, rock), and the grass looks better.

## Done
- `World/Ground.cs`: `GroundKind { Grass, Mud, Rock }`, `Ground.Assign(layoutSeed, count)` (start island grass, at most half the islands bare, some bare island in worlds of 4+), `Top`, `Vegetation`, `Boulders`. `WorldGenerator` uses it for the island top material and passes vegetation scale and boulder count to `Scenery.Decorate` (mud: 30 % trees/bushes/flowers + 2 boulder piles, rock: 10 % + 5).
- Textures (`TextureBuilder`, menu 11): `T_Terrain` is now 512 px with rows of leaning blades and patchy colour; new `T_Mud` (dry cracked plates, wet patches, pebbles) and `T_Pebbles` (flagstones with moss). Materials `M_TerrainMud`, `M_TerrainRock` (`MaterialBuilder`, menu 2; the kit picks them up with menu 14). Autumn only recolours the grass.

## Known issues / ideas
- Tiling of the mud cracks is visible from above; a blend between ground types on one island (vertex colours) would need a custom shader.
- Rock ground could use real rock props from the Kenney / Quaternius packs.
