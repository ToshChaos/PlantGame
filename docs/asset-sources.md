# Free asset sources (visual polish research)

Researched 2026-10-24. Goal: replace procedural/placeholder art for **trees, seeds, islands, ruins, background objects**.
Rule (AGENTS.md rule 8): every file we import gets a row in `Game/Assets/ThirdParty/LICENSES.md`.

**Status (2026-10-24): Kenney Nature Kit + Fantasy Town Kit imported (CC0); everything else below is still a lead.** Blockers met: CraftPix downloads need a sign-in (a person must do it), the agent shell has no network; Chrome works for OpenGameArt/Kenney.

~~Old status~~: nothing imported yet. From the agent session the shell has no network, the in-app browser refuses
opengameart.org / kenney.nl, and craftpix.net answers scripted downloads with 403 (Cloudflare). Download the zips by hand
(or allow the sites in the browser pane) into `Source/ThirdParty-raw/<pack>/` (not under `Game/Assets`), then ask Claude to import.
"Verified" = read on the page this session; "from search" = taken from a search snippet, re-check the licence on the page before importing.

## Licence cheat sheet
| Licence | Credit needed | Notes |
|---|---|---|
| CC0 (Kenney, Quaternius) | no (credit is nice) | safest, use first |
| CraftPix free licence | no | royalty free in unlimited projects, commercial OK; **do not redistribute the raw files** (keep zips out of public repos, import only what we use). Verified on the pack pages. |
| Unity Asset Store EULA (Standard) | no | usable in shipped games, not redistributable as assets. The git repo is private/hackathon, but do not publish the raw package contents in a public repo. Needs a Unity login to "Add to My Assets", then Package Manager > My Assets > Import. |
| Fab free items | per listing (Fab Standard or CC-BY) | check each listing; CC-BY needs a credit line |
| CC-BY (various on OpenGameArt) | **yes** | add author to LICENSES.md and the credits screen (T24) |

## 1. CraftPix freebies (https://craftpix.net/freebies/) — verified
Mostly 2D; the **3D Game Assets** category (search `3d low poly`) has FBX + a tiny PNG atlas (64x64 palette, easy to recolour to our white/green/blue). Free ones, download link is `/download/<id>/` on the pack page:

| Use | Pack | Page | Download id |
|---|---|---|---|
| Trees | Free Tree 3D Low Poly Pack (21 objects) | /freebies/free-tree-3d-low-poly-pack/ | 12184 |
| Trees | Free Tropical Palm Tree (20) | /freebies/free-tropical-palm-tree-3d-low-poly-models/ | 13426 |
| Trees | Free Winter Tree | /freebies/free-winter-tree-3d-low-poly-models/ | 12787 |
| Bushes / background | Free Bush 3D Low Poly Models | /freebies/free-bush-3d-low-poly-models/ | 12931 |
| Islands / ground detail | Free Stone 3D Low Poly Models (40 rocks) | /freebies/free-stone-3d-low-poly-models/ | 12505 |
| Ruins | Free Medieval Props (28: walls, fences, gates, etc.) | /freebies/free-medieval-props-3d-low-poly-pack/ | 14680 |
| Ruins | Free Environment Props (28: roads, fences, gates, lamps) | /freebies/free-environment-props-3d-low-poly-models/ | 17864 |
| Ruins | Free Medieval Houses; Free Elf House | /freebies/free-medieval-houses-3d-low-poly-pack/, /free-elf-house-3d-low-poly-models/ | 19392, 23696 |

2D, useful for the **sky / title backdrop**: "Free Sky Ruins 2D Platformer Tileset", "Free World Tree Roots 2D Platformer Tileset", "Free Beach 2D Game Backgrounds", "Island Game - Free GUI" (could skin menus).
No floating-island 3D pack on CraftPix; the island shapes stay ours (Blender kit, T10).

## 2. OpenGameArt (https://opengameart.org/) — from search, licence re-check needed
- **Kenney Nature Kit** — CC0, 330+ objects (trees, rocks, terrain pieces, waterfalls, plants), FBX/OBJ/glTF, ~10.5 MB. https://opengameart.org/content/nature-kit (also kenney.nl/assets/nature-kit). Best single source for trees, bushes, flowers, rocks, cliff/terrain pieces, background clutter.
- **Kenney Fantasy Town Kit 2.0** — CC0, 160+ objects, FBX/OBJ/glTF, ~3.9 MB. https://opengameart.org/content/fantasy-town-kit . Walls, arches, stairs, pillars for **ruins** (we weather/tint them).
- **LowPoly Nature Pack** (Quaternius) — CC0, blends + OBJ + FBX. https://opengameart.org/content/lowpoly-nature-pack
- **Big LowPoly Environment Pack** (zisongbr) — CC0, trees, rocks, mountains. https://opengameart.org/content/big-lowpoly-environment-pack
- Other OGA hits to open and check (licence unknown): Low Poly Forest Pack, Low Poly Tree, Lowpoly Trees, Simple Low Poly Trees, Palm tree v2.
- Not found: any floating-island or ready ruins pack on OGA; use Kenney Fantasy Town + Nature cliffs, or the Unity items below.

## 3. freegameassets.com — verified (directory site, no files of its own except two model packs)
Home page lists its own **Pixel Fantasy 3D Models (155)** and **Painterly Fantasy 3D Models (31)** (licence not read yet; check before use), plus a link directory: Poly Pizza (1523 3D models, mostly CC0/CC-BY), Kenney (513), Poly Haven (CC0 textures/HDRIs for the sky), ambientCG (CC0 textures), Blend Swap, Itch.io, Fab, Unity Asset Store, Mixamo, Freesound (SFX), Incompetech / FreePD (music).
Most useful leads: **Poly Pizza** (search "tree", "ruins", "floating island"; each model shows its licence, CC-BY needs credit) and **Poly Haven HDRIs** for the skybox.

## 4. Unity Asset Store / Fab (added on request) — from search, not verified
Store pages found (prices unverified, many are paid; filter "Free" in the store):
- Islands Stylized Low Poly: https://assetstore.unity.com/packages/3d/environments/islands-stylized-low-poly-206458
- Low Poly Floating Islands: https://assetstore.unity.com/packages/3d/environments/low-poly-floating-islands-224511
- Stylized Lowpoly Islands Pack: https://assetstore.unity.com/packages/3d/environments/landscapes/stylized-lowpoly-islands-pack-187882
- Low Poly Style Environment: https://assetstore.unity.com/packages/3d/environments/low-poly-style-environment-72471
- Fab: TinyNature Lite (free) https://www.fab.com/listings/6595e3fe-d515-423d-a6b5-e38946e9a7e2
Search found no confirmed free floating-island pack. Needs: a Unity login to add free items. Then they appear in Package Manager > My Assets. Note URP: store materials are often Built-in, we convert them (Edit > Rendering > Materials > Convert) or swap to our `M_*` palette materials.

## Recommended plan
1. **Trees / background**: Kenney Nature Kit (CC0) + CraftPix Free Tree / Bush packs. Recolour to the art-bible palette via our vertex-colour / palette materials.
2. **Rocks / island undersides / cliffs**: Kenney Nature Kit cliffs + CraftPix Free Stone (40 rocks).
3. **Ruins**: Kenney Fantasy Town Kit (walls, arches, pillars, stairs) + CraftPix Medieval Props; moss tint via shader.
4. **Seeds**: no good asset found. Keep ours (Blender), optionally use Kenney nature pods or CraftPix stones as seed bases.
5. **Sky**: Poly Haven CC0 HDRI or our own gradient sky (T16).
6. After each import: row in `LICENSES.md`, FBX settings per `docs/pipeline.md`, then swap the kit pieces through the builders, not by hand.

## Open
- Hand-download the zips above (Kenney Nature Kit, Fantasy Town Kit, CraftPix Tree/Bush/Stone/Medieval Props) into `Source/ThirdParty-raw/`.
- Read the licence of freegameassets' own Pixel/Painterly 3D packs.

## Imported so far
- Kenney Nature Kit 2.1 (CC0) -> `Game/Assets/Art/Models/Kenney/Nature` (170 models) and Fantasy Town Kit 2.0 (CC0) -> `.../Town` (26 models). Raw zips unpacked in `Source/ThirdParty-raw/` (git-ignored).
- `PlantGame > 17 Build Kenney Props` remaps materials to URP (`Art/Materials/Kenney`, leaf/bark tinted to the art-bible greens), then `PlantGame > 14 Build World Kit` fills `WorldKit.props`.
- `Scenery.cs` uses them through `Kit.Prop(name)` (trees, bushes, rocks, flowers, ruin pillars, rubble) and falls back to the old primitives if a model is missing. Shots: `docs/shots/kenney_*.png`.

## More leads found by Google (not downloaded)
- Quaternius Ultimate Stylized Nature Pack (60+) and Stylized Tree Pack (https://quaternius.com, CC0). The MegaKit (Standard, 68 of 116 models) is imported, see below.
- FreeStylized "Ruins Modular Kit 01" (read 2026-10-24): https://freestylized.com/asset_pack/ruins_01/ . Modular kit (arch, wall and block pieces in the preview), 2K baked textures, FBX + Unity 2021.3 + UE 5.4 + Blender. Licence: "Royalty Free: free for all commercial and non-commercial purposes" (no credit stated). The Free Download button leads to a **Patreon post** (https://www.patreon.com/posts/free-ruins-01-119705391), which normally needs a Patreon login (free), so a person has to download it. The same-titled "Stylized Ruins - Modular Assets" by Alex Nevarez is on ArtStation (free, $0 Extended Commercial licence, FBX + Unity package) and TurboSquid (free, licence not read).
- Poly Pizza "Floating Island" by cg_world: https://poly.pizza/GZKvAswLh6 (check licence; Poly Pizza lists CC0 and CC-BY).
- OpenGameArt "CC0 ASSETS 3D LOW POLY" collection: https://opengameart.org/content/cc0-assets-3d-low-poly
- BlendSwap floating island .blend files (CC licences vary), itch.io 3D + low-poly free assets tag.

## Quaternius Stylized Nature MegaKit (imported 2026-10-24)
- Downloaded via Chrome from https://quaternius.itch.io/stylized-nature-megakit (free route "No thanks, just take me to the downloads", Standard zip, 99 MB, CC0). Raw: `Source/ThirdParty-raw/quaternius-stylized-nature/` (git-ignored).
- 29 models copied to `Game/Assets/Art/Models/Quaternius/Nature` + textures. `PlantGame > 18 Build Quaternius Props` makes URP Lit materials (leaf/flower cards are alpha-clipped and two-sided), then `PlantGame > 14 Build World Kit`.
- `Scenery.cs` prefers them for trees (oak/blossom/birch = CommonTree, pine/cypress = Pine, willow = TwistedTree), bushes, ferns and rocks. Autumn levels keep the Kenney `_fall` trees. Shots `docs/shots/quaternius_*.png`.
- The Pro version (all 116 models) is paid; not used.

## FreeStylized Ruins Modular Kit 01 (imported 2026-10-24)
- Zip `Ruins1_(AssetPack4).zip` (382 MB, downloaded by the user from the Patreon post) -> `Source/ThirdParty-raw/freestylized-ruins01/` (git-ignored). Only the FBX folder and the BaseColor + Normal_GL textures of the three atlases are copied to `Game/Assets/Art/Models/Ruins/{A1,A2,A3,Textures}`. The Blender, Unity and Unreal variants in the zip are not used.
- 21 pieces: `AP4_A1/A2_StoneWall*` (walls 1-3 m tall), `AP4_A2_Stone1-8` (rubble), `AP4_A3_Door1` (a complete gothic arch, 4 x 5.1 m), `DoorStone1/2` (arch fragments), `LongStone1/2` (slabs), `StoneRing1`, `Gothic1`.
- `PlantGame > 19 Build Ruins Props` (normal-map import type, 1024 px base colour, one URP Lit material per atlas), then `14 Build World Kit`. Contact sheet `docs/shots/ruins_sheet.png`.
- In game: `Door1` is the portal arch frame (the old cube pillars stay as invisible colliders, glowing disc and steps unchanged); `StoneWall*` replace the white pillar/rubble clusters; stones, slabs and arch fragments are also used as sky debris. Shots `docs/shots/ruins_arch.png`, `ruins_cluster.png`.

## Sky debris and depth (same session)
- `Scenery.SkyDebris`: about half the jagged rocks and 3/4 of the broken pillar drums now use imported rocks (Quaternius `Rock_Medium_*`, Kenney rocks / stones) and ruin chunks; the 10 unreachable islets (colliders removed) get a Quaternius bush or fern, imported rocks, now and then a wall piece (every 3rd) or a mini gothic arch (every 5th).
- `Scenery.KeepDebrisClear` (called by `WorldGenerator` after decorating): moves the baked debris at least 18 m (islets 28 m) off every island edge and off the corridor between linked islands; a piece that cannot be moved clear is hidden. Measured on level 4: closest debris 20 m from an island edge, none hidden.
- Depth: `OvergrowthSky` has a new `_DeepColor` (looking down fades to deep blue), the low cloud sea is darker and bluer, and five translucent dark-blue `M_Abyss` sheets (y -80 to -190, follow the camera) sit under the clouds. Rebuilt with `PlantGame > 11 Build Atmosphere`. Shots `docs/shots/depth_down.png`, `depth_overview.png`.
