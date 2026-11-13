# Asset optimisation scratchpad (islands, scatter, sky; not the plants)

Working notes for the next session that looks at the cost of the non-plant visual assets. Started 2026-11-13.
The plants had their own pass on 2026-11-12 (`docs/task-log.md`, "Plant assets checked and rebaked").
What was done in the first pass is in `docs/task-log.md`, "Island and scatter assets: optimisation pass"; this file holds what is
left, how to measure, and the traps.

## How to measure (5 minutes)

1. Enter play mode, load a level (level 10 is the largest: 8 islands, rain):
   `GameBootstrap.PendingRun = level.ToConfig(); PlayerHud.SkipMenuOnce(); SceneManager.LoadScene("Main");`
   and set `Application.runInBackground = true`.
2. `PlantGame.Editor.SceneCensus.Run()` (menu PlantGame > Test > Scene Census) writes `Game/Logs/census_<tier>_<level>.txt`:
   triangles, renderers and shadow casters per island part, per mesh and per material. It counts every enabled renderer in the
   scene, not what the camera sees, so compare census with census, not with the Stats window.
3. For Medium / Low set `GameSettings.Quality = 1` (or 0) and load the level again: `WorldOptimizer` only runs after a load.
   **Set it back to 2 before leaving play mode** (it is saved in PlayerPrefs, and it flips `m_CurrentQuality` in
   `ProjectSettings/QualitySettings.asset`: do not commit that).
4. Close-ups: render a temporary `Camera` into a RenderTexture and write a PNG to `docs/shots/` (git-ignored).
5. Static checks need no play mode: `ModelImporter` / `TextureImporter` settings and `Mesh` attributes through `Unity_RunCommand`.

## Where level 10 stands (editor, whole scene, after the first pass)

| | High | Medium |
|---|---|---|
| Triangles | 1,035k (was 1,209k) | 777k (was 1,004k) |
| Renderers | 1,591 | 946 |
| Shadow casters | 1,090 | 302 |
| Meshes loaded, editor count | 43 MB (was 61 MB) | 70 MB (was 97 MB) |
| Draw calls at the spawn view (Stats) | about 1,900 | 724 |

Biggest groups left on High: Scenery (trees, bushes, flowers) 187k, SideDecor 119k, seed trees and seed patches 302k together,
Atmosphere 150k, WaterPlants 47k, ShinyBits 43k, Overgrowth 43k, the eight island meshes 54k.

## Not done yet, biggest first

Each item says what it would save and what to check before doing it.

1. **Unity primitive spheres on High: 220 x 768 = 169k triangles** (berries, petals, dew, flower centres; `Scenery.Prim`).
   Medium / Low already swap in a 144-triangle sphere (`WorldOptimizer`). High was left alone on purpose in T63. A middle sphere
   (about 300 triangles) for anything under 0.3 m would save about 100k. Check petals and berries close up.
2. **Decorative seeds on seed trees / patches on High: 152k triangles** (`Seed_BeanstalkSeed` 42 x 1,480, `Seed_ShelfSeed`
   18 x 3,312, `Seed_VineSeed` 18 x 1,648). Medium / Low use `SeedMeshes.Lite` (a third). High could use Lite for the fruit that
   hangs out of reach and keep the full mesh on pickups.
3. **Tree bark.** A tree is 3-7k triangles and the bark is most of it (`Q_Bark_NormalTree`: 93k over 26 trees). The models have
   9 generated mesh LODs; `Scenery.SimplifyTree` takes LOD 1 for the leaf cards and LOD 0 for the bark. Try bark LOD 1 or 2 on
   Medium / Low (`OrganicProps.Lean`). Deeper leaf LODs lose the cards (bare trees), so keep the leaves at 1.
   The four backdrop islet trees (`CommonTree_1`, `CommonTree_3`, `Pine_3` in the census) are the full source meshes by design
   (the atmosphere bake stores the asset, not a copy).
4. **Tree mesh copies.** `SimplifyTree` and `LeaflessTree` copy the whole vertex buffer of each source mesh (up to 15 sources,
   5-10k vertices each), and the sources stay read/write for it. A build-time bake (an editor builder that writes the simplified
   and bare meshes as assets, sources read/write off) would drop the runtime copies and the CPU copies: several MB.
5. **Island side rocks (`SideDecor`): 86 renderers and 86 shadow casters on Medium**, none merged. `WorldOptimizer` keeps anything
   under `Scenery` / `SideDecor` that is 2.5 m or bigger apart so `CameraOcclusionFade` can fade it, and a side rock scaled to
   2 m tall is up to 2.8 m wide. The camera is never below the rim in normal play: merging SideDecor whatever its size (or raising
   its limit) would cut about 80 draws and 80 casters per level. Check the fade when the player hangs on a beanstalk next to a wall.
6. **Floating marble chips: 82 renderers, each with its own `FloatBob`.** Never merged because they move. One merged mesh per
   island with a single bob (or instancing) would cut about 10 draws an island. They are 320 triangles each now; Low could use 80.
7. **The player is 57 renderers** (57 draws plus 57 shadow draws). The parts hang on joints, so only parts on the same joint with
   the same material can merge; `char_player.py` could join them on export. Needs a look at `PlayerOutfitApplier` (it maps mesh
   name suffixes to materials).
8. **Sky backdrop (`Atmosphere` prefab): about 170 renderers, 150k triangles**, nothing merged. Far islands are 7.5-8.7k triangles
   each for something hundreds of metres away (41k for five); islets 55k; 16 sky rocks at 1,280. A coarser far-island mesh and
   detail-2 sky rocks would halve it. Baked by `MainSceneBuilder.BuildAtmosphere`; the meshes live in `Atmosphere_Generated.asset`.
9. **Normal maps without tangents.** `M_RockLoose`, `M_Marble` and `M_Stone` carry a normal map, but the meshes they are drawn
   on (`DebrisMeshes.Rock` / `Tile`, about 250 renderers a level) have no tangents, so the map is sampled and does nothing (or
   lights slightly wrong). Either drop the maps from those three materials (one texture sample less, 0.4 MB of textures on a
   phone) or call `RecalculateTangents` in `DebrisMeshes` (16 bytes a vertex). Compare a rock close up both ways first.
   `IslandTerrain`, `TreeParts` and `SoilPatchMesh` do compute tangents.
10. **Cattails and lily pads.** After the first pass a pond is still about 4.8k triangles of cattail heads (60 heads x 80) and
    1.4k of pads. Heads are 4 cm wide: 6 sides and 3 bands (36 triangles) would do. The round pads were a user request: keep 24 segments.
11. **Low tier only:** marble chips already drop to 80 triangles on Medium / Low; the small loose rocks (320) could go to 80 on Low.

## Textures

- Android still uses ETC2 (`PlayerSettings.Android.textureCompressionFormats` = ETC). RGBA textures are 8 bits a pixel there
  and the game has six of them at 1024 (leaf, flower and fern atlases, `T_LeafGrey`). ASTC 6x6 (as on iOS) is 3.6 bits a pixel:
  under half the memory and a smaller APK. Listed as open since 2026-11-04; needs a device run (T64), which is why it was not
  changed here.
- Oversized for what they cover: `Flowers.png` (flowers on one bush model), `Mushrooms.png` (one small prop), `Leaves.png`
  (ferns) are 1024; 512 would do. `T_LeafGrey.png` (1024, RGBA) tints the procedural crowns.
- The app icons (`T_AppIcon*`) are not referenced by any scene or material: they do not load at runtime.
- Total for everything under `Art` on Android: about 45 MB in the editor's count, icons included. Texture memory is not the problem.

## Unreferenced now, delete if nobody wants them back

Nothing in a scene, prefab or `Resources/WorldKit` references these any more, so they are not in a build, but they are still in the repo:

- `Art/Models/Kenney` (32 models, the town atlas), `Art/Materials/Kenney` (11 materials), `Editor/KenneyPropBuilder.cs`.
- `Art/Models/Quaternius/Nature/Rock_Medium_1..3.fbx`, `Textures/Rocks_Diffuse.png`, `Art/Materials/Quaternius/Q_Rocks.mat`.
- Code that only served them: `PalmLook.Apply` / `Retarget` (the bark and frond textures are still used by `OrganicProps.Palm`),
  the `pillar-stone` / `wall-broken` fallbacks in `Scenery.Pillar` / `Slabs`, `QualityTierBuilder.Batchable`'s Kenney branch.
- Update `ThirdParty/LICENSES.md` and `docs/asset-sources.md` when they go (the raw packs stay in `Source/ThirdParty-raw/`).

The scenery code still names these models in its pick lists (`"flower_redA"`, `"Rock_Medium_1"`, `"plant_bush"`, ...). **Keep the
names**: `Kit.Has` answers true for them and the lists feed `rnd.Next`, so removing a name shifts every random draw after it and
changes the dressing of every existing level.

## Traps

- **`Overgrowth.Batch` meshes carry one winding.** Everything emitted through `Overgrowth.Emit` must use a two-sided material
  (`ColourMat`, `StoneMat`, `LeafMat`, `BladeMat` all set `_Cull` 0). A new batch material with back-face culling shows half its triangles.
- **Tier-dependent detail must be gated on `Application.isPlaying`** (`OrganicProps.Lean`), or the editor builders bake whatever
  tier the editor happens to be set to and stop being deterministic.
- **Quaternius models import without tangents** (`QuaterniusPropBuilder`). Give one of their materials a normal map and the
  tangents have to come back (`importTangents`), on all 22 models.
- **`MainSceneBuilder.BuildAtmosphere` rewrites `Atmosphere_Generated.asset` even when it reports "prefab unchanged"**: the
  stored `M_Stone#n` materials pick up the matte settings of commit 465955c, which was never re-baked. Not committed here
  (unrelated to this pass); the next rebuild will show that diff.
- Anything `WorldKit` lists is loaded with it at startup: check `WorldKitBuilder.Shipped` before adding a folder of models or materials.
- Editor mesh memory numbers include a CPU copy of every mesh; use them to compare, not as device numbers. No device was measured.
