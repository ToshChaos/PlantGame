"""Validate a collection before export (T02). Checks names, tri budget, growth_t, Anchor/Tip/COL_*.

    exec(open(S + 'validate_export.py').read())
    errors, warnings = validate_collection('Plant_Bridge_01', 'plant')   # profile: 'plant' | 'generic'
    print_report('Plant_Bridge_01', 'plant')

'plant' profile follows AGENTS.md 5.3. Blender-side names carry the variant number (Anchor_01); the exporter strips it.
Accepted for 'Anchor' etc.: either 'Anchor_<NN>' or plain 'Anchor' (if only one variant is in the file).
"""
import os
import re
import bpy

if 'PLANT_NAME_RE' not in globals():
    _S = os.path.dirname(os.path.abspath(__file__)) if '__file__' in globals() else None
    exec(open(os.path.join(_S, 'pipeline_common.py')).read())


def _find(coll, base, nn):
    for o in coll.all_objects:
        if o.name in (base, '%s_%s' % (base, nn)):
            return o
    return None


def _tri_count(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    try:
        me.calc_loop_triangles()
        return len(me.loop_triangles)
    finally:
        ev.to_mesh_clear()


def _has_growth_t(obj):
    me = obj.data
    a = me.color_attributes.get('growth_t') or me.attributes.get('growth_t')
    return a is not None


def _growth_t_range(obj):
    a = obj.data.color_attributes.get('growth_t')
    if a is None:
        return None
    vals = [d.color[0] for d in a.data]
    return (min(vals), max(vals)) if vals else None


def validate_collection(coll_name, profile='generic'):
    errors, warnings = [], []
    coll = bpy.data.collections.get(coll_name)
    if coll is None:
        return ['collection not found: ' + coll_name], warnings
    objs = list(coll.all_objects)
    meshes = [o for o in objs if o.type == 'MESH']
    if not objs:
        errors.append('collection is empty')

    if profile == 'plant':
        m = PLANT_NAME_RE.match(coll_name)
        if not m:
            errors.append('collection name must be Plant_<Bridge|Ladder|Platform|Collector>_<01..06>, got ' + coll_name)
            return errors, warnings
        kind, nn = m.group(1), m.group(2)
        main = bpy.data.objects.get(coll_name)
        if main is None or main.type != 'MESH' or main.name not in coll.all_objects:
            errors.append('missing main mesh object named %s in the collection' % coll_name)
        else:
            if not _has_growth_t(main):
                errors.append('main mesh has no color attribute "growth_t" (vertex colour R = 0 base -> 1 tip)')
            else:
                rng = _growth_t_range(main)
                if rng and (rng[1] - rng[0]) < 0.5:
                    warnings.append('growth_t range is only %.2f..%.2f; expected ~0..1' % rng)
            tris = _tri_count(main)
            if tris > MAX_TRIS:
                errors.append('%d tris > budget %d' % (tris, MAX_TRIS))
            if len(main.material_slots) > 1:
                warnings.append('more than one material slot (contract: one)')
        anchor = _find(coll, 'Anchor', nn)
        tip = _find(coll, 'Tip', nn)
        if anchor is None:
            errors.append('missing empty Anchor_%s' % nn)
        if tip is None:
            errors.append('missing empty Tip_%s' % nn)
        if main is not None and anchor is not None:
            d = (main.matrix_world.translation - anchor.matrix_world.translation).length
            if d > 1e-3:
                errors.append('mesh pivot is %.3f m from Anchor (pivot must be at the Anchor)' % d)
        for e in (anchor, tip):
            if e is not None and e.type != 'EMPTY':
                errors.append('%s must be an Empty' % e.name)
            if e is not None and main is not None and e.parent is not main:
                warnings.append('%s is not parented to %s' % (e.name, coll_name))
        cols = [o for o in objs if o.name.startswith('COL_%s_' % nn)]
        if kind != 'Collector' and not cols:
            errors.append('no collision proxies named COL_%s_<idx>' % nn)
        stray = [o.name for o in objs if o.name.startswith('COL_') and not o.name.startswith('COL_%s_' % nn)]
        if stray:
            errors.append('collision proxies with the wrong variant number: ' + ', '.join(stray))
        if kind == 'Ladder' and not [o for o in objs if o.name.startswith('CLIMB_%s_' % nn)]:
            errors.append('ladder needs CLIMB_%s_<idx> trigger boxes' % nn)
        if kind == 'Collector':
            for base in ('Reservoir', 'WaterMin', 'WaterMax'):
                if _find(coll, base, nn) is None:
                    errors.append('collector needs %s_%s' % (base, nn))
            if not [o for o in objs if o.name == 'INTERACT_' + nn]:
                errors.append('collector needs INTERACT_%s' % nn)
        # tri budget is per variant: sum of visible render meshes (proxies excluded)
        total = sum(_tri_count(o) for o in meshes if not o.name.startswith(('COL_', 'CLIMB_', 'INTERACT_')))
        if total > MAX_TRIS:
            errors.append('variant total %d tris > budget %d' % (total, MAX_TRIS))
    else:
        for o in meshes:
            if o.scale[:] != (1.0, 1.0, 1.0):
                warnings.append('%s has non-unit scale %s (apply scale)' % (o.name, tuple(round(v, 4) for v in o.scale)))
        if not meshes:
            warnings.append('no mesh objects')
    return errors, warnings


def print_report(coll_name, profile='generic'):
    errors, warnings = validate_collection(coll_name, profile)
    print('== %s [%s]: %d error(s), %d warning(s)' % (coll_name, profile, len(errors), len(warnings)))
    for e in errors:
        print('  ERROR ' + e)
    for w in warnings:
        print('  warn  ' + w)
    return not errors
