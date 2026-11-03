# Third-party asset sources

Rule (AGENTS.md rule 8): every file we import gets a row in `Game/Assets/ThirdParty/LICENSES.md`.
Raw downloads live in the git-ignored `Source/ThirdParty-raw/<pack>/`; only the models the game places are copied into
`Game/Assets/Art/Models/<Pack>/` (the 2026-10-26 cleanup removed 174 unused ones; copy a model back from the raw pack if
`Scenery.cs` starts asking for it).

## What the game uses
| Pack | Licence | In `Game/Assets` | Used for |
|------|---------|------------------|----------|
| Kenney Nature Kit 2.1 | CC0 | `Art/Models/Kenney/Nature` (26) | palms, bushes, flowers, small rocks and gems |
| Kenney Fantasy Town Kit 2.0 | CC0 | `Art/Models/Kenney/Town` (6) | ruin pillars, low walls, stairs, rubble |
| Quaternius Stylized Nature MegaKit (Standard) | CC0 | `Art/Models/Quaternius/Nature` (22 + textures) | all broadleaf / pine / willow trees (recoloured for autumn), bushes, ferns, medium rocks |
| FreeStylized Ruins Modular Kit 01 | royalty free, do not redistribute raw files | `Art/Models/Ruins` (17 + 3 atlases) | the gate goal's arch, ruin walls, rubble, sky debris |

## Re-importing (editor menus)
1. Copy the FBX (and textures) from `Source/ThirdParty-raw/` into the pack's folder under `Game/Assets/Art/Models/`.
2. Run the pack's builder: `PlantGame > 17 Build Kenney Props` (URP materials, leaf / bark re-tinted to the art bible),
   `18 Build Quaternius Props` (alpha-clipped, two-sided leaf and flower cards) or `19 Build Ruins Props` (one URP Lit
   material per atlas, normal maps).
3. `PlantGame > 14 Build World Kit` so `Resources/WorldKit` lists the model; `Scenery.cs` places it by file name through
   `Kit.Prop(name)` and falls back to procedural primitives when a model is missing.

## Where the packs came from
- Kenney Nature Kit and Fantasy Town Kit: https://kenney.nl (also on OpenGameArt).
- Quaternius MegaKit: https://quaternius.itch.io/stylized-nature-megakit (free Standard zip; the paid Pro version is not used).
- FreeStylized Ruins 01: https://freestylized.com/asset_pack/ruins_01/ (download through the linked Patreon post, needs a
  free login; the Blender, Unity and Unreal variants in the zip are not used).

## Licence cheat sheet
| Licence | Credit needed | Notes |
|---|---|---|
| CC0 (Kenney, Quaternius) | no (credited anyway) | safest, use first |
| FreeStylized custom licence | no (credited anyway) | royalty free, commercial OK; do not redistribute the raw files |
| CraftPix free licence | no | commercial OK; do not redistribute the raw files |
| Unity Asset Store EULA | no | usable in shipped games, not redistributable as assets |
| CC-BY (OpenGameArt, Poly Pizza) | **yes** | add the author to `LICENSES.md` and the credits |

## Leads not used (researched 2026-10-24)
CraftPix free 3D packs (trees, palms, winter trees, bushes, stones, medieval props; downloads need a sign-in), Quaternius
Ultimate Stylized Nature Pack, Poly Pizza (mixed CC0 / CC-BY; "Floating Island" by cg_world), Poly Haven / ambientCG
(CC0 HDRIs and textures), Unity Asset Store floating-island packs (mostly paid, Built-in materials).
