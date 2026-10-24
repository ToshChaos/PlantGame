# T02 — Blender → Unity export pipeline & import presets

**Owner:** A0 + A3 · **Depends on:** T00
**Status:** DONE

> Read `AGENTS.md` first (contracts in section 5). Update your row in `TASKS.md` when status changes.

## Goal
One command in Blender produces files Unity imports correctly (scale, axes, vertex colours, empties), and one Unity preset applies the right settings.

## Deliverables
- `Source/Blender/scripts/export_fbx.py`: exports selected collection(s) to `Source/Blender/exports/<name>.fbx` with agreed settings
- `Source/Blender/scripts/validate_export.py`: checks names, tri budget, presence of `Anchor`/`Tip`/`COL_*`/`growth_t`
- Unity `AssetPostprocessor` or import preset: scale 1, no materials import (use project materials), vertex colour kept, mesh compression off, read/write on for plants
- Copy step: `Source/Blender/scripts/sync_to_unity.py` (copies exports into `Game/Assets/Art/Models/...`)
- `docs/pipeline.md`: the exact FBX axis/scale settings, naming rules, round-trip test

## Steps
1. Use the Blender MCP `execute_blender_code` to create a test cube with an empty `Anchor` and a vertex colour attribute `growth_t`; export it.
2. Import in Unity; verify size = 1 m, orientation, vertex colour access (`mesh.colors`), empties become child transforms.
3. Pin settings (forward -Z, up Y, apply transform) and write them down.
4. Use an **asymmetric** test object: the reference project found X is mirrored on FBX import; fix it once in the export script and document it. Decide FBX vs glTF; FBX is the default unless vertex colours misbehave.
5. Write the validate script and run it on the test object.

## Acceptance criteria
- [ ] Test cube round-trips at the correct scale and orientation
- [ ] `growth_t` is readable in Unity from the imported mesh
- [ ] `Anchor` empty exists as a child at the right position
- [ ] `docs/pipeline.md` explains how a Blender agent exports in 3 commands

## Requests (to/from other roles)
_none yet_

## Handoff
_Files created · prefab/scene names · public API · 30-second test · known issues_
Source/Blender/scripts/*, docs/pipeline.md, ModelImportSettings postprocessor, round-trip test verified (axis_forward 'Z'). Known: plants.blend/kit.blend still empty.
