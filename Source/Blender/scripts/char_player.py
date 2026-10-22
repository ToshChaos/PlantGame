"""T31: the one MVP player model, a blocky "Roblox-style" character in white / black / gold.

In Blender (via MCP execute_blender_code):
    S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
    exec(open(S + 'char_player.py').read()); build_all()          # rebuilds, saves characters.blend, exports the FBX

Exported with CHAR_FBX_OVERRIDES (hierarchy-safe; see pipeline_common.py).
Layout (Blender Z up, metres): feet at the origin, ~1.8 m tall, facing Blender +Y (= Unity +Z after export).
The character's left is -X. Every part is a separate mesh with ONE colour, parented under a joint empty so Unity can
swing it without a rig:
    Char_Player_01 (root empty)
      Torso_W, Belt_G, Emblem_G, Collar_K
      Joint_Head   (neck)      -> Head_W, Eye_L_K, Eye_R_K, Band_G, Sprout_G
      Joint_ArmL/R (shoulder)  -> Arm_<s>_W, Cuff_<s>_G, Glove_<s>_K
      Joint_LegL/R (hip)       -> Leg_<s>_K, Shoe_<s>_W
Mesh name suffix = material in Unity: _W M_PlayerWhite, _K M_PlayerBlack, _G M_PlayerGold (PlayerPrefabBuilder).
"""
import os
import bpy
import bmesh

_here = os.path.dirname(os.path.abspath(__file__)) if '__file__' in globals() else None
_S = _here or os.path.join(globals().get('PROJECT_ROOT', '/Users/anton.podkletnov/Projects/PlantGame'), 'Source', 'Blender', 'scripts')
exec(open(os.path.join(_S, 'export_fbx.py')).read())

NAME = 'Char_Player_01'
COLOURS = {   # preview only; Unity assigns its own materials by suffix
    'W': (0.93, 0.94, 0.92, 1.0),
    'K': (0.035, 0.037, 0.045, 1.0),
    'G': (0.83, 0.62, 0.22, 1.0),
}

# body proportions (metres)
HIP_Z = 0.80
SHOULDER_Z = 1.30
NECK_Z = 1.34
LEG_X = 0.16
ARM_X = 0.45


def _material(code):
    name = 'MP_' + code
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = COLOURS[code]
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    if bsdf:
        bsdf.inputs['Base Color'].default_value = COLOURS[code]
        bsdf.inputs['Metallic'].default_value = 0.8 if code == 'G' else 0.0
        bsdf.inputs['Roughness'].default_value = 0.35 if code == 'G' else 0.6
    return m


def _box_mesh(name, size, centre, bevel):
    """Box of `size` (x, y, z) centred at `centre`, given in the parent's local space."""
    bm = bmesh.new()
    r = bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=r['verts'])
    bmesh.ops.translate(bm, vec=centre, verts=r['verts'])
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=2, profile=0.5, affect='EDGES')
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return me


def _part(coll, name, parent, size, centre, bevel=0.03):
    """Mesh object with its origin at the parent joint; centre is relative to that joint."""
    code = name.rsplit('_', 1)[-1]
    me = _box_mesh(name, size, centre, bevel)
    me.materials.append(_material(code))
    ob = bpy.data.objects.new(name, me)
    coll.objects.link(ob)
    ob.parent = parent
    return ob


def _empty(coll, name, parent, loc):
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_type = 'PLAIN_AXES'
    ob.empty_display_size = 0.15
    coll.objects.link(ob)
    ob.parent = parent
    ob.location = loc
    return ob


def clear():
    coll = bpy.data.collections.get(NAME)
    if coll:
        for o in list(coll.all_objects):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(coll)
    for me in [m for m in bpy.data.meshes if m.users == 0]:
        bpy.data.meshes.remove(me)


def build_player():
    clear()
    coll = bpy.data.collections.new(NAME)
    bpy.context.scene.collection.children.link(coll)
    root = _empty(coll, NAME, None, (0, 0, 0))

    # torso (relative to the root = feet)
    tz = (HIP_Z + SHOULDER_Z + 0.06) / 2
    th = SHOULDER_Z + 0.06 - HIP_Z
    _part(coll, 'Torso_W', root, (0.66, 0.34, th), (0, 0, tz), 0.04)
    _part(coll, 'Belt_G', root, (0.68, 0.36, 0.08), (0, 0, HIP_Z + 0.07), 0.015)
    _part(coll, 'Emblem_G', root, (0.14, 0.03, 0.14), (0, 0.175, HIP_Z + 0.32), 0.01)     # chest leaf badge (front = +Y)
    _part(coll, 'Collar_K', root, (0.30, 0.24, 0.05), (0, 0, SHOULDER_Z + 0.07), 0.015)

    # head
    head = _empty(coll, 'Joint_Head', root, (0, 0, NECK_Z))
    _part(coll, 'Head_W', head, (0.42, 0.40, 0.40), (0, 0, 0.24), 0.06)
    for s, x in (('L', -0.09), ('R', 0.09)):
        _part(coll, 'Eye_%s_K' % s, head, (0.065, 0.03, 0.10), (x, 0.195, 0.27), 0.012)
    _part(coll, 'Band_G', head, (0.44, 0.42, 0.05), (0, 0, 0.38), 0.015)
    _part(coll, 'Sprout_G', head, (0.04, 0.04, 0.12), (0, 0, 0.48), 0.008)                  # little gold sprout on top
    _part(coll, 'Leaf_L_G', head, (0.12, 0.03, 0.05), (-0.07, 0, 0.55), 0.01)
    _part(coll, 'Leaf_R_G', head, (0.12, 0.03, 0.05), (0.07, 0, 0.57), 0.01)

    # arms (joint at the shoulder, hanging down)
    for s, sign in (('L', -1), ('R', 1)):
        j = _empty(coll, 'Joint_Arm' + s, root, (sign * ARM_X, 0, SHOULDER_Z))
        _part(coll, 'Arm_%s_W' % s, j, (0.22, 0.26, 0.44), (0, 0, -0.18), 0.035)
        _part(coll, 'Cuff_%s_G' % s, j, (0.24, 0.28, 0.05), (0, 0, -0.42), 0.012)
        _part(coll, 'Glove_%s_K' % s, j, (0.21, 0.25, 0.14), (0, 0, -0.52), 0.035)

    # legs (joint at the hip)
    for s, sign in (('L', -1), ('R', 1)):
        j = _empty(coll, 'Joint_Leg' + s, root, (sign * LEG_X, 0, HIP_Z))
        _part(coll, 'Leg_%s_K' % s, j, (0.30, 0.32, HIP_Z - 0.10), (0, 0, -(HIP_Z - 0.10) / 2), 0.03)
        _part(coll, 'Shoe_%s_W' % s, j, (0.32, 0.40, 0.12), (0, 0.04, -HIP_Z + 0.06), 0.03)
    return coll


def stats():
    coll = bpy.data.collections[NAME]
    tris = 0
    dg = bpy.context.evaluated_depsgraph_get()
    zmax = 0.0
    for o in coll.all_objects:
        if o.type != 'MESH':
            continue
        me = o.evaluated_get(dg).to_mesh()
        me.calc_loop_triangles()
        tris += len(me.loop_triangles)
        for v in me.vertices:
            zmax = max(zmax, (o.matrix_world @ v.co).z)
        o.evaluated_get(dg).to_mesh_clear()
    return {'objects': len(coll.all_objects), 'tris': tris, 'height': round(zmax, 3)}


def build_all(save=True, export=True):
    build_player()
    bpy.context.view_layer.update()
    out = stats()
    if save:
        path = os.path.join(project_root(), 'Source', 'Blender', 'characters.blend')
        bpy.ops.wm.save_as_mainfile(filepath=path)
        out['blend'] = path
    if export:
        out['fbx'] = export_collection(NAME, validate_profile='none', overrides=CHAR_FBX_OVERRIDES)
    return out
