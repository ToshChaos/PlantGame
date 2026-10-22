"""Export one or more collections to Source/Blender/exports/<name>.fbx with the pinned settings (T02).

In Blender (via MCP execute_blender_code):
    S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
    exec(open(S + 'export_fbx.py').read())
    export_collections(['Plant_Bridge_01'])          # one FBX per collection, named after it
    export_collections(['Plant_Bridge_01'], validate_profile='plant')   # validates first (default for Plant_*)

Each collection becomes one FBX. Objects in a collection are exported with their parent hierarchy.
Names ending in _NN for Anchor/Tip/Land/Reservoir/WaterMin/WaterMax/CLIMB are stripped for the export only
(and restored afterwards), because Blender object names are unique per file.
"""
import os
import bpy

_here = os.path.dirname(os.path.abspath(__file__)) if '__file__' in globals() else None
_S = _here or os.path.join(globals().get('PROJECT_ROOT', '/Users/anton.podkletnov/Projects/PlantGame'), 'Source', 'Blender', 'scripts')
exec(open(os.path.join(_S, 'pipeline_common.py')).read())
exec(open(os.path.join(_S, 'validate_export.py')).read())


def _supported(settings):
    props = set(bpy.ops.export_scene.fbx.get_rna_type().properties.keys())
    dropped = [k for k in settings if k not in props]
    return {k: v for k, v in settings.items() if k in props}, dropped


def _strip_name(name, nn):
    for pfx in STRIP_NN_PREFIXES:
        if name.startswith(pfx + '_' + nn):
            # Anchor_01 -> Anchor ; CLIMB_01_2 -> CLIMB_2
            return pfx + name[len(pfx) + 1 + len(nn):]
    return None


def export_collection(coll_name, out_dir=None, validate_profile=None, strict=True, overrides=None):
    """overrides: FBX settings replacing FBX_SETTINGS entries for this export (e.g. CHAR_FBX_OVERRIDES)."""
    coll = bpy.data.collections.get(coll_name)
    if coll is None:
        raise ValueError('collection not found: ' + coll_name)
    if validate_profile is None:
        validate_profile = 'plant' if coll_name.startswith('Plant_') else 'generic'
    if validate_profile != 'none':
        errors, warnings = validate_collection(coll_name, validate_profile)
        for w in warnings:
            print('[export] WARN ' + w)
        if errors and strict:
            raise RuntimeError('validation failed for %s:\n  - %s' % (coll_name, '\n  - '.join(errors)))

    out_dir = out_dir or exports_dir()
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, coll_name + '.fbx')

    nn = coll_name.rsplit('_', 1)[-1] if validate_profile == 'plant' else None
    renamed = []  # (obj, original name)
    prev_sel = [o.name for o in bpy.context.selected_objects]
    prev_active = bpy.context.view_layer.objects.active
    try:
        objs = list(coll.all_objects)
        if nn:
            # two passes through temp names so renames cannot collide
            for o in objs:
                new = _strip_name(o.name, nn)
                if new:
                    renamed.append((o, o.name, new))
            for o, old, new in renamed:
                o.name = '__tmp__' + old
            for o, old, new in renamed:
                o.name = new
                if o.name != new:
                    raise RuntimeError('cannot rename %s to %s (name taken by another object)' % (old, new))
        if bpy.context.object and bpy.context.object.mode != 'OBJECT':
            bpy.ops.object.mode_set(mode='OBJECT')
        bpy.ops.object.select_all(action='DESELECT')
        for o in objs:
            o.select_set(True)
        if objs:
            bpy.context.view_layer.objects.active = objs[0]
        settings, dropped = _supported(dict(FBX_SETTINGS, **(overrides or {})))
        if dropped:
            print('[export] settings not supported by this Blender (dropped): ' + ', '.join(dropped))
        bpy.ops.export_scene.fbx(filepath=path, **settings)
    finally:
        for o, old, new in renamed:
            o.name = '__tmp__' + old
        for o, old, new in renamed:
            o.name = old
        bpy.ops.object.select_all(action='DESELECT')
        for n in prev_sel:
            ob = bpy.data.objects.get(n)
            if ob:
                ob.select_set(True)
        bpy.context.view_layer.objects.active = prev_active
    print('[export] wrote ' + path)
    return path


def export_collections(names, **kw):
    return [export_collection(n, **kw) for n in names]
