# Third-party asset sources

Rule (AGENTS.md rule 8): every file we import gets a row in `Game/Assets/ThirdParty/LICENSES.md`.
Raw downloads live in the git-ignored `Source/ThirdParty-raw/<pack>/`; only the models the game places are copied into
`Game/Assets/Art/Models/<Pack>/` (the 2026-10-26 cleanup removed 174 unused ones; copy a model back from the raw pack if
`Scenery.cs` starts asking for it).

## What the game uses
The Kenney Nature Kit and Fantasy Town Kit (CC0, https://kenney.nl) were used until 2026-11-13 and are deleted from `Game/Assets`:
`OrganicProps` builds the palms, flowers, grass and loose rocks in code, the Quaternius bush stands in for the flat bushes and the
ruins kit for the town pieces. The three Quaternius `Rock_Medium` models and their texture went the same way. `Scenery.cs` still
uses the old model names in its pick lists (`Kit.Has` answers for them); the raw packs are in `Source/ThirdParty-raw/`.

| Pack | Licence | In `Game/Assets` | Used for |
|------|---------|------------------|----------|
| Quaternius Stylized Nature MegaKit (Standard) | CC0 | `Art/Models/Quaternius/Nature` (19 + textures) | all broadleaf / pine / willow trees (recoloured for autumn), bushes, ferns, mushrooms |
| FreeStylized Ruins Modular Kit 01 | royalty free, do not redistribute raw files | `Art/Models/Ruins` (17 + 3 atlases) | the gate goal's arch, ruin walls, rubble, sky debris |

## Re-importing (editor menus)
1. Copy the FBX (and textures) from `Source/ThirdParty-raw/` into the pack's folder under `Game/Assets/Art/Models/`.
2. Run the pack's builder: `PlantGame.Editor.QuaterniusPropBuilder.Build()` (alpha-clipped, two-sided leaf and flower cards)
   or `PlantGame.Editor.RuinsPropBuilder.Build()` (one URP Lit material per atlas, normal maps).
3. `PlantGame > Scenes > World Kit` so `Resources/WorldKit` lists the model: it lists every model under `Art/Models/Quaternius`
   and `Art/Models/Ruins` and every material under `Art/Materials`, and all of it is loaded at startup, so keep only what a level
   places in those folders. `Scenery.cs` places a model by file name through `Kit.Prop(name)` and falls back to procedural
   primitives when it is missing.

## Where the packs came from
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
