"""Shared constants/helpers for the Blender -> Unity pipeline (T02). Run inside Blender.

Usage from the Blender MCP:  exec(open(PATH + 'pipeline_common.py').read())
Normally you do not call this directly; export_fbx.py and validate_export.py exec it for you.
"""
import os
import re
import bpy

# Repo root on the Mac. Overridden automatically when the .blend lives in <root>/Source/Blender.
PROJECT_ROOT = '/Users/anton.podkletnov/Projects/PlantGame'


def project_root():
    fp = bpy.data.filepath
    if fp:
        d = os.path.dirname(os.path.abspath(fp))
        # <root>/Source/Blender/x.blend
        if os.path.basename(d) == 'Blender' and os.path.basename(os.path.dirname(d)) == 'Source':
            return os.path.dirname(os.path.dirname(d))
    return PROJECT_ROOT


def exports_dir():
    d = os.path.join(project_root(), 'Source', 'Blender', 'exports')
    os.makedirs(d, exist_ok=True)
    return d


# Pinned FBX settings (AGENTS.md section 2, docs/pipeline.md). Keys not supported by the running
# Blender are dropped by export_fbx.py so a Blender upgrade does not crash the export.
FBX_SETTINGS = dict(
    axis_forward='Z',              # NOT the exporter default '-Z': measured in Unity, '-Z' imports rotated 180 deg about Y (see docs/pipeline.md)
    axis_up='Y',
    apply_scale_options='FBX_SCALE_UNITS',
    bake_space_transform=True,
    global_scale=1.0,
    use_selection=True,
    object_types={'EMPTY', 'MESH'},
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    colors_type='LINEAR',          # growth_t is data, not a colour: no sRGB conversion
    prioritize_active_color=False,
    add_leaf_bones=False,
    bake_anim=False,
    use_custom_props=False,
    path_mode='AUTO',
)

# Parented hierarchies (characters, T31): bake_space_transform is broken for child objects (meshes land rotated and
# offset in Unity). Export without it, with the exporter's default forward, and let Unity bake the axis conversion
# (ModelImportSettings turns bakeAxisConversion on under Art/Models/Characters). Measured: faces Unity +Z, left at -X.
CHAR_FBX_OVERRIDES = dict(bake_space_transform=False, axis_forward='-Z')

# Empties/children whose Blender name carries a unique "_NN" suffix; the exporter strips it so Unity sees the
# contract names (Anchor, Tip, Land, Reservoir, WaterMin, WaterMax, CLIMB_<idx>).
STRIP_NN_PREFIXES = ('Anchor', 'Tip', 'Land', 'Reservoir', 'WaterMin', 'WaterMax', 'CLIMB')

PLANT_KINDS = ('Bridge', 'Ladder', 'Platform', 'Collector')
PLANT_NAME_RE = re.compile(r'^Plant_(Bridge|Ladder|Platform|Collector)_(0[1-6])$')
MAX_TRIS = 10000     # v2.8 (T67): was 4000

# Blender object names are unique per file, but every plant kind uses Anchor_NN / COL_NN_i with the same NN. Non-mesh helper
# objects therefore carry a kind tag in Blender ("Anchor_01@L"); the exporter strips it, so Unity sees the contract names.
KIND_TAG = {'Bridge': 'B', 'Ladder': 'L', 'Platform': 'P', 'Collector': 'C'}


def strip_tag(name):
    return re.sub(r'@[A-Za-z]+$', '', name)
