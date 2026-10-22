# Blender -> Unity pipeline (T02)

Blender **5.2.2 LTS**, Unity 6000.6.5f1. Sources stay in `Source/Blender/`; never put `.blend` in `Game/Assets/`.

## Export in 3 commands (Blender, via MCP `execute_blender_code`)
```python
S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
exec(open(S + 'export_fbx.py').read())          # also loads pipeline_common.py and validate_export.py
export_collections(['Plant_Bridge_01'])          # validates, then writes Source/Blender/exports/Plant_Bridge_01.fbx
```
Then copy into Unity from the sandbox shell: `python3 Source/Blender/scripts/sync_to_unity.py` (add `--dry` to preview).
Plants go to `Game/Assets/Art/Models/Plants/`, kit pieces to `.../Kit/`, `Test_*` to `.../Test/`.
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
| `mesh_smooth_type` / modifiers | `FACE` / applied |
| `add_leaf_bones`, `bake_anim` | `False` |
Settings the running Blender does not know are dropped with a printed note, so upgrades do not crash the export.

## Naming
- Meshes and collections `Type_Name_Variant`, e.g. `Plant_Bridge_03`, `Kit_Terrain_Flat_01`. Plant variants: `Plant_<Bridge|Ladder|Platform|Collector>_<01..06>`; one collection per variant, same name as its main mesh.
- Blender object names are unique per file, so helper objects carry the variant number: `Anchor_03`, `Tip_03`, `Land_03`, `CLIMB_03_<idx>`, `Reservoir_03`, `WaterMin_03`, `WaterMax_03`. The exporter strips the number for the export only (restored afterwards), so Unity sees the contract names `Anchor`, `Tip`, `Land`, `CLIMB_<idx>`, ...
- `COL_<NN>_<idx>` and `INTERACT_<NN>` keep their number (contract 5.3). Empties must be parented to the main mesh; the mesh pivot sits on `Anchor`.

## Validation (`validate_export.py`)
Checks: collection/mesh names, `growth_t` colour attribute (0..1 range), tri budget (<= 4000 per variant), `Anchor`/`Tip`, pivot on Anchor, `COL_*`, `CLIMB_*` for ladders, collector extras. `export_collections` stops on errors; call `print_report(name, 'plant')` to see them. Profile `'generic'` (kit pieces, tests) only checks scale.

### Parented hierarchies (characters, T31)
`bake_space_transform=True` breaks child objects (meshes under empties land rotated and offset in Unity). Character
collections export with `pipeline_common.CHAR_FBX_OVERRIDES` (`bake_space_transform=False`, `axis_forward='-Z'`) and
`ModelImportSettings` turns on `bakeAxisConversion` under `Art/Models/Characters/`. Measured with `Char_Player_01`: geometry
faces Unity +Z, left at -X, 1:1 scale; the FBX root empty keeps a ~90° X rotation, so code that rotates joints should work
in model space (see `PlayerAnimator`). Plants/kit pieces with nested parts should use the same overrides.

## Unity import (`Assets/Editor/ModelImportSettings.cs`)
Applied automatically on first import of any FBX under `Assets/Art/Models`: scale 1 with `useFileScale`, no material import, mesh compression off, Read/Write on for `Plants/` and `Test/`, hierarchy preserved (empties kept), no animation/cameras/lights. Menu `PlantGame > Tools > Reapply Model Import Settings` re-applies after a preset change. `growth_t` is read with `mesh.colors[i].r`.

## Round-trip test
1. Blender: `exec(open(S+'t02_make_test_object.py').read()); make_test_asym()` then `export_collection('Test_Asym', validate_profile='generic')`.
2. Shell: `python3 Source/Blender/scripts/sync_to_unity.py`.
3. Unity: menu `PlantGame > Tools > Verify Pipeline Round-trip` and read the Console report: height = 1.0 m, `growth_t` range 0..1, `Anchor` found, arm side.

## Status of the open questions
- **Verified in Blender (T02):** the exporter writes `Test_Asym.fbx`; the `_NN` stripping works and names are restored; validation passes on a plant skeleton.
- **Verified in Unity 6000.6.5f1 (Test_Asym round trip):** scale 1:1, no X mirror (arm +X, nub +Z) with `axis_forward='Z'`, `growth_t` readable from vertex colour R and drives the grow shader. Menu `PlantGame/Tools/Verify Pipeline Round-trip` re-runs the check.
- FBX is the default format; glTF only if vertex colours misbehave.
