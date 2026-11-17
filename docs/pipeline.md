# Blender -> Unity pipeline (T02)

Blender **5.2.2 LTS**, Unity 6000.6.9f1. Sources stay in `Source/Blender/`; never put `.blend` in `Game/Assets/`.

## Export in 3 commands (Blender, via MCP `execute_blender_code`)
```python
S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
exec(open(S + 'export_fbx.py').read())          # also loads pipeline_common.py and validate_export.py
export_collections(['Plant_Bridge_01'])          # validates, then writes Source/Blender/exports/Plant_Bridge_01.fbx
```
Then copy into Unity from the sandbox shell: `python3 Source/Blender/scripts/sync_to_unity.py` (add `--dry` to preview).
Plants go to `Game/Assets/Art/Models/Plants/`, the player to `.../Characters/`. `Source/Blender/exports/` is git-ignored: the copies under `Game/Assets/Art/Models/` are what the repo keeps.
Each Blender call is a fresh Python namespace, so re-run the `exec` lines in every call.

## Pinned FBX settings (`pipeline_common.FBX_SETTINGS`)
| Setting | Value |
|---------|-------|
| `axis_forward` / `axis_up` | `Z` / `Y` (measured in Unity: `-Z` rotated models 180° about Y) |
| `apply_scale_options` | `FBX_SCALE_UNITS` (1 Blender unit = 1 m) |
| `bake_space_transform` | `True` |
| `global_scale` | `1.0` |
| `use_selection` | `True` (the exporter selects the collection) |
| `object_types` | `EMPTY`, `MESH` |
| `colors_type` | `LINEAR` (growth_t is data, no sRGB curve), `prioritize_active_color=False` |
| `mesh_smooth_type` / modifiers | `FACE` / applied (Unity reads the exported normals, so smooth faces and sharp edges set in Blender carry over) |
| `add_leaf_bones`, `bake_anim` | `False` |
Settings the running Blender does not know are dropped with a printed note, so upgrades do not crash the export.

## Rebuilding the plants
Every plant kind is rebuilt from its script (`build_all()` bakes six variants into `plants.blend` and saves it); all four kinds live in the one file.
```python
S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
for f in ('plants_bridge.py', 'plants_ladder.py', 'plants_platform.py', 'plants_collector.py'):
    ns = {'__name__': f}; exec(open(S + f).read(), ns); ns['build_all']()
exec(open(S + 'export_fbx.py').read())
export_collections(sorted(c.name for c in bpy.data.collections if c.name.startswith('Plant_')))
```
Then `sync_to_unity.py` and the four `PlantGame > Plants > ... Prefabs` menus. Rules the bakes follow:
- **Cards are two-sided.** `PlantGrow` culls back faces, so a leaf made of one layer of faces is invisible from behind. Leaf blades are joined with a flipped copy (`two_sided` in `plants_bridge.py`).
- **Shading:** `shade_by_angle` (smooth, sharp above 80 degrees). A tube needs five or more sides to shade round.
- **Beanstalk:** the steps are on Blender -Y (the side the player climbs, Unity -Z); foliage goes to the flanks and stays out of the climb column.
- **Collector:** the `Reservoir` must stay inside the basin's outer wall at every fill; `WaterCollector.ApplyFill` widens it as it rises.
- **Vertex colour `growth_t`:** R growth, G part id, B sway weight, A flower opening (contract 5.3). A part that should bend in the breeze stores `swayw` with its root at the origin before it is instanced (`store_sway` in `plants_bridge.py`); anything without `swayw` stays still. Leave A at 1 unless the part is a flower with its centre in the UV maps.
- **Bridge silhouettes:** `STYLES` gives each variant a leaf shape (`blade`, `broad`, `willow`, `clover`) and a flower shape (`FLOWERS`: `classic`, `daisy`, `cup`, `star`); `bake_variant` rebuilds the node group for its style, so a new style is a new entry in those tables. White and yellow flowers share their points on a rail, so each reads as one two-tone flower.
- **Mesh space:** Unity's mesh space is Blender (x, z, y) for the plants (measured from the bounds); data stored for the shader must use that order.

## Naming
- Meshes and collections `Type_Name_Variant`, e.g. `Plant_Bridge_03`, `Kit_Terrain_Flat_01`. Plant variants: `Plant_<Bridge|Ladder|Platform|Collector>_<01..06>`; one collection per variant, same name as its main mesh.
- Blender object names are unique per file, so helper objects carry the variant number: `Anchor_03`, `Tip_03`, `Land_03`, `CLIMB_03_<idx>`, `Reservoir_03`, `WaterMin_03`, `WaterMax_03`. The exporter strips the number for the export only (restored afterwards), so Unity sees the contract names `Anchor`, `Tip`, `Land`, `CLIMB_<idx>`, ...
- `COL_<NN>_<idx>` and `INTERACT_<NN>` keep their number (contract 5.3). Empties must be parented to the main mesh; the mesh pivot sits on `Anchor`.

## Validation (`validate_export.py`)
Checks: collection/mesh names, `growth_t` colour attribute (0..1 range), tri budget (<= 10000 per variant, `pipeline_common.MAX_TRIS`), `Anchor`/`Tip`, pivot on Anchor, `COL_*`, `CLIMB_*` for ladders, collector extras. `export_collections` stops on errors; call `print_report(name, 'plant')` to see them. Profile `'generic'` (kit pieces, tests) only checks scale.

### Parented hierarchies (characters, T31)
`bake_space_transform=True` breaks child objects (meshes under empties land rotated and offset in Unity). Character
collections export with `pipeline_common.CHAR_FBX_OVERRIDES` (`bake_space_transform=False`, `axis_forward='-Z'`) and
`ModelImportSettings` turns on `bakeAxisConversion` under `Art/Models/Characters/`. Measured with `Char_Player_01`: geometry
faces Unity +Z, left at -X, 1:1 scale; the FBX root empty keeps a ~90° X rotation, so code that rotates joints should work
in model space (see `PlayerAnimator`). Plants/kit pieces with nested parts should use the same overrides.

## Unity import (`Assets/Editor/ModelImportSettings.cs`)
Applied automatically on first import of any FBX under `Assets/Art/Models`: scale 1 with `useFileScale`, no material import, mesh compression off, Read/Write off, no tangents on `Plants/`, hierarchy preserved (empties kept), no animation/cameras/lights. Menu `PlantGame > Tools > Reapply Model Import Settings` re-applies after a preset change. `growth_t` is read with `mesh.colors[i].r`.

## Round-trip test (T02, done)
The axis / scale settings above were verified once with an asymmetric test object (`Test_Asym`). The fixture
(`t02_make_test_object.py`, `Test_Asym.fbx`, `Editor/PipelineRoundTripTest.cs`) was removed in the 2026-10-26 cleanup;
restore it from git history (commit before that date) if the export settings ever change.

## Status of the open questions
- **Verified in Blender (T02):** the exporter writes `Test_Asym.fbx`; the `_NN` stripping works and names are restored; validation passes on a plant skeleton.
- **Verified in Unity 6000.6.5f1 (Test_Asym round trip):** scale 1:1, no X mirror (arm +X, nub +Z) with `axis_forward='Z'`, `growth_t` readable from vertex colour R and drives the grow shader. 
- FBX is the default format; glTF only if vertex colours misbehave.
