"""T31 / v3.1: the player model, an explorer-mage of the ruined magic civilisation (rounded, stylised, no boxes).

In Blender (via MCP execute_blender_code):
    S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
    exec(open(S + 'char_player.py').read()); build_all()          # rebuilds, saves characters.blend, exports the FBX

Exported with CHAR_FBX_OVERRIDES (hierarchy-safe; see pipeline_common.py).
Layout (Blender Z up, metres): feet at the origin, ~1.8 m tall body (+0.05 m hat), facing Blender +Y (= Unity +Z after export).
The character's left is -X. Every part is a separate mesh with ONE material role, parented under a joint empty so Unity can
swing it without a rig. The joint names are the animation contract (PlayerAnimator, WateringCanVisual): do not rename.
    Char_Player_01 (root empty)
      Torso, Tunic, Belt, Buckle, Strap, Satchel, Amulet, Cloak, Collar ...
      Joint_Head   (neck)      -> Head, Eyes, Hair, Hat (brim, crown, band), Gem, Sprig leaves
      Joint_ArmL/R (shoulder)  -> Sleeve, Forearm, Bracer, Cuff, Hand (+ Vine on the left arm)
      Joint_LegL/R (hip)       -> Trousers, Boot, Boot cuff
Mesh name suffix = material ROLE (reskin surface). Unity maps role -> material through a PlayerOutfit asset:
    _Skin  _Hair  _Cloth (tunic, trousers)  _Cloak (mantle, hat band)  _Gear (leather: hat, boots, belt, satchel, gloves)
    _Trim (metal / gold)  _Glow (magic: glyphs, gem, amulet)  _Leaf (plants)  _Dark (eyes)
A new look = a new outfit asset (and optionally extra parts); the geometry and the animation stay the same.
"""
import math
import os
import bpy
import bmesh
from mathutils import Vector, Quaternion, Matrix

_here = os.path.dirname(os.path.abspath(__file__)) if '__file__' in globals() else None
_S = _here or os.path.join(globals().get('PROJECT_ROOT', '/Users/anton.podkletnov/Projects/PlantGame'), 'Source', 'Blender', 'scripts')
exec(open(os.path.join(_S, 'export_fbx.py')).read())

NAME = 'Char_Player_01'
ROLES = ('Skin', 'Hair', 'Cloth', 'Cloak', 'Gear', 'Trim', 'Glow', 'Leaf', 'Dark')
COLOURS = {   # preview only; Unity assigns its own materials by role (PlayerOutfit)
    'Skin': (0.80, 0.58, 0.45, 1.0),
    'Hair': (0.24, 0.13, 0.07, 1.0),
    'Cloth': (0.93, 0.92, 0.84, 1.0),
    'Cloak': (0.17, 0.37, 0.56, 1.0),
    'Gear': (0.36, 0.22, 0.12, 1.0),
    'Trim': (0.83, 0.62, 0.22, 1.0),
    'Glow': (0.49, 0.95, 0.90, 1.0),
    'Leaf': (0.36, 0.71, 0.42, 1.0),
    'Dark': (0.04, 0.04, 0.05, 1.0),
}

# body proportions (metres)
HIP_Z = 0.82
SHOULDER_Z = 1.30
NECK_Z = 1.34
LEG_X = 0.11
ARM_X = 0.30


def _material(role):
    name = 'MP_' + role
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = COLOURS[role]
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    if bsdf:
        bsdf.inputs['Base Color'].default_value = COLOURS[role]
        bsdf.inputs['Metallic'].default_value = 0.8 if role == 'Trim' else 0.0
        bsdf.inputs['Roughness'].default_value = 0.35 if role in ('Trim', 'Glow') else 0.65
    return m


# ---- bmesh shape helpers (everything is in the parent's local space) -----------------------------------------------

def _rot_to(direction):
    return Vector((0, 0, 1)).rotation_difference(Vector(direction).normalized())


def _ellipsoid(bm, radii, centre, rot=None, segs=14, rings=9):
    """UV sphere scaled to `radii`; the poles lie on the local Z axis (pointed ends when Z is stretched)."""
    r = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    verts = r['verts']
    bmesh.ops.scale(bm, vec=radii, verts=verts)
    if rot is not None:
        bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=rot.to_matrix(), verts=verts)
    bmesh.ops.translate(bm, vec=centre, verts=verts)
    return verts


def _tube(bm, a, b, r1, r2, squash=1.0, segs=14, caps=True):
    """Cone frustum from point a (radius r1) to point b (radius r2); `squash` flattens it front to back (local Y)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    r = bmesh.ops.create_cone(bm, cap_ends=caps, cap_tris=False, segments=segs, radius1=r1, radius2=r2, depth=d.length)
    verts = r['verts']
    bmesh.ops.scale(bm, vec=(1, squash, 1), verts=verts)
    bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=_rot_to(d).to_matrix(), verts=verts)
    bmesh.ops.translate(bm, vec=(a + b) / 2, verts=verts)
    return verts


def _limb(bm, a, b, r1, r2, squash=1.0):
    """Capsule-ish limb: a tube with a sphere at each end."""
    _tube(bm, a, b, r1, r2, squash)
    _ellipsoid(bm, (r1, r1 * squash, r1), a, segs=12, rings=7)
    _ellipsoid(bm, (r2, r2 * squash, r2), b, segs=12, rings=7)


def _torus(bm, major, minor, centre, axis=(0, 0, 1), squash=1.0, segs=20, ring=8):
    verts = []
    for i in range(segs):
        u = 2 * math.pi * i / segs
        row = []
        for j in range(ring):
            v = 2 * math.pi * j / ring
            x = (major + minor * math.cos(v)) * math.cos(u)
            y = (major + minor * math.cos(v)) * math.sin(u) * squash
            row.append(bm.verts.new((x, y, minor * math.sin(v))))
        verts.append(row)
    for i in range(segs):
        for j in range(ring):
            bm.faces.new((verts[i][j], verts[(i + 1) % segs][j], verts[(i + 1) % segs][(j + 1) % ring], verts[i][(j + 1) % ring]))
    flat = [v for row in verts for v in row]
    bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=_rot_to(axis).to_matrix(), verts=flat)
    bmesh.ops.translate(bm, vec=centre, verts=flat)
    return flat


def _gem(bm, size, centre, stretch=(1, 1, 1)):
    r = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1.0)
    bmesh.ops.scale(bm, vec=(size * stretch[0], size * stretch[1], size * stretch[2]), verts=r['verts'])
    bmesh.ops.translate(bm, vec=centre, verts=r['verts'])
    return r['verts']


def _leaf(bm, base, direction, length, width):
    """Pointed leaf: a sphere stretched along its poles and flattened, starting at `base`."""
    d = Vector(direction).normalized()
    centre = Vector(base) + d * (length / 2)
    return _ellipsoid(bm, (width / 2, 0.006, length / 2), centre, rot=_rot_to(d), segs=10, rings=7)


def _smooth(me):
    for p in me.polygons:
        p.use_smooth = True


def _bake(coll, ob):
    """Applies the modifiers of a temporary object and returns the resulting mesh data."""
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg), depsgraph=dg)
    return me


# ---- scene helpers -------------------------------------------------------------------------------------------------

def _finish(coll, name, parent, me, smooth=True):
    role = name.rsplit('_', 1)[-1]
    me.name = name
    me.materials.append(_material(role))
    if smooth:
        _smooth(me)
    ob = bpy.data.objects.new(name, me)
    coll.objects.link(ob)
    ob.parent = parent
    return ob


def _part(coll, name, parent, build, smooth=True):
    """Mesh object (origin at the parent joint) from `build(bm)`."""
    bm = bmesh.new()
    build(bm)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return _finish(coll, name, parent, me, smooth)


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
    for cu in [c for c in bpy.data.curves if c.users == 0]:
        bpy.data.curves.remove(cu)


def _cloak(coll, root):
    """Back mantle: a bent grid behind the torso, thickened with a Solidify modifier that is baked into the mesh."""
    rows, cols = 9, 13
    verts, faces = [], []
    for i in range(rows):
        t = i / (rows - 1)
        z = 1.37 - t * 0.66
        radius = 0.215 + 0.17 * t ** 1.3
        span = math.radians(112 - 34 * t)
        for j in range(cols):
            s = j / (cols - 1) * 2 - 1
            th = s * span
            zz = z + (0.035 * math.sin(s * 5.0) * t)          # wavy hem
            verts.append((radius * math.sin(th), -radius * math.cos(th) * 0.78 - 0.01, zz))
    for i in range(rows - 1):
        for j in range(cols - 1):
            faces.append((i * cols + j, i * cols + j + 1, (i + 1) * cols + j + 1, (i + 1) * cols + j))
    me = bpy.data.meshes.new('Cloak_Cloak')
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new('Cloak_tmp', me)
    coll.objects.link(ob)
    mod = ob.modifiers.new('Solid', 'SOLIDIFY')
    mod.thickness = 0.018
    mod.offset = 1.0
    bpy.context.view_layer.update()
    baked = _bake(coll, ob)
    bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.meshes.remove(me)
    return _finish(coll, 'Cloak_Cloak', root, baked)


def _vine(coll, parent, centre_z0, centre_z1, radius, turns):
    """A little ivy vine spiralling round the forearm, a bevelled curve baked to a mesh."""
    cu = bpy.data.curves.new('Vine_tmp', 'CURVE')
    cu.dimensions = '3D'
    cu.bevel_depth = 0.009
    cu.bevel_resolution = 2
    sp = cu.splines.new('POLY')
    n = 28
    sp.points.add(n - 1)
    for k in range(n):
        t = k / (n - 1)
        a = t * turns * 2 * math.pi
        sp.points[k].co = (radius * math.cos(a), 0.035 + radius * math.sin(a), centre_z0 + (centre_z1 - centre_z0) * t, 1.0)
    ob = bpy.data.objects.new('Vine_tmp', cu)
    coll.objects.link(ob)
    bpy.context.view_layer.update()
    me = _bake(coll, ob)
    bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.curves.remove(cu)
    return _finish(coll, 'Vine_Leaf', parent, me)


def _watering_can(coll, arm):
    """The magic watering can held in the right hand. Unity shows / hides `Hold_Can` (WateringCanVisual), tips `Can_Tilt`
    when pouring and toggles `CanWater_Glow` while the can is full. Origin = the grip, the spout points to +Y."""
    hold = _empty(coll, 'Hold_Can', arm, (0, 0.04, -0.55))
    tilt = _empty(coll, 'Can_Tilt', hold, (0, 0, 0))
    c = (0, 0.19, 0.01)

    def body(bm):
        _ellipsoid(bm, (0.105, 0.105, 0.095), c, segs=16, rings=10)
        _tube(bm, (c[0], c[1], -0.085), (c[0], c[1], -0.07), 0.07, 0.085, segs=16)          # foot
    _part(coll, 'CanBody_Cloak', tilt, body)
    _part(coll, 'CanBand_Trim', tilt, lambda bm: _torus(bm, 0.103, 0.011, (c[0], c[1], 0.012), segs=24, ring=6))
    _part(coll, 'CanRim_Trim', tilt, lambda bm: _torus(bm, 0.062, 0.012, (c[0], c[1], 0.092), segs=20, ring=6))
    _part(coll, 'CanWater_Glow', tilt, lambda bm: _ellipsoid(bm, (0.062, 0.062, 0.01), (c[0], c[1], 0.088), segs=16, rings=5))
    _part(coll, 'CanHandle_Gear', tilt, lambda bm: _torus(bm, 0.062, 0.014, (0, 0.03, 0.0), axis=(1, 0, 0), segs=18, ring=7))
    _part(coll, 'CanSpout_Trim', tilt, lambda bm: _tube(bm, (0, 0.27, 0.0), (0, 0.46, 0.15), 0.034, 0.022, segs=12))
    _part(coll, 'CanRose_Trim', tilt, lambda bm: _tube(bm, (0, 0.455, 0.145), (0, 0.50, 0.19), 0.025, 0.06, segs=14))
    for sx in (-1, 1):
        _part(coll, 'CanGlyph%+d_Glow' % sx, tilt, lambda bm, x=sx: _gem(bm, 0.026, (x * 0.108, 0.19, 0.0), (0.45, 0.9, 1.5)), smooth=False)

    def sprig(bm):
        _leaf(bm, (0, 0.10, 0.06), (0, -0.7, 0.8), 0.12, 0.05)
        _leaf(bm, (0, 0.10, 0.06), (-0.7, -0.4, 0.7), 0.11, 0.045)
        _leaf(bm, (0, 0.10, 0.06), (0.7, -0.4, 0.7), 0.11, 0.045)
    _part(coll, 'CanSprig_Leaf', tilt, sprig)


def build_player():
    clear()
    coll = bpy.data.collections.new(NAME)
    bpy.context.scene.collection.children.link(coll)
    root = _empty(coll, NAME, None, (0, 0, 0))

    # ---- torso: tunic, belt, strap, satchel, amulet, cloak ----
    def torso(bm):
        _ellipsoid(bm, (0.21, 0.15, 0.27), (0, 0, 1.10))
        _ellipsoid(bm, (0.18, 0.14, 0.18), (0, 0, 0.95))
    _part(coll, 'Torso_Cloth', root, torso)

    def tunic(bm):
        _tube(bm, (0, 0, 0.93), (0, 0, 0.62), 0.19, 0.285, squash=0.82, segs=20, caps=False)
    _part(coll, 'Tunic_Cloth', root, tunic)
    _part(coll, 'Hem_Trim', root, lambda bm: _tube(bm, (0, 0, 0.64), (0, 0, 0.615), 0.282, 0.288, squash=0.82, segs=20, caps=False))

    _part(coll, 'Belt_Gear', root, lambda bm: _tube(bm, (0, 0, 0.87), (0, 0, 0.945), 0.205, 0.205, squash=0.78, segs=20, caps=False))
    _part(coll, 'Buckle_Trim', root, lambda bm: _ellipsoid(bm, (0.045, 0.02, 0.04), (0, 0.165, 0.91), segs=10, rings=6))
    _part(coll, 'Strap_Gear', root, lambda bm: _limb(bm, (0.17, 0.115, 1.34), (-0.16, 0.165, 0.93), 0.02, 0.02))

    def satchel(bm):
        _ellipsoid(bm, (0.07, 0.115, 0.09), (-0.255, -0.03, 0.80), segs=12, rings=8)
        _tube(bm, (-0.25, 0.0, 0.88), (-0.25, -0.12, 0.88), 0.012, 0.012)
    _part(coll, 'Satchel_Gear', root, satchel)
    _part(coll, 'Clasp_Trim', root, lambda bm: _ellipsoid(bm, (0.012, 0.03, 0.025), (-0.325, -0.01, 0.83), segs=8, rings=6))

    _part(coll, 'Amulet_Glow', root, lambda bm: _gem(bm, 0.032, (0, 0.155, 1.19), (1, 0.7, 1.3)), smooth=False)
    _part(coll, 'Setting_Trim', root, lambda bm: _torus(bm, 0.042, 0.007, (0, 0.152, 1.19), axis=(0, 1, 0), segs=16, ring=6))

    _cloak(coll, root)
    _part(coll, 'Collar_Cloak', root, lambda bm: _torus(bm, 0.145, 0.045, (0, -0.005, 1.36), squash=0.88, segs=24, ring=8))
    # glowing glyph marks along the cloak hem (the old civilisation's script)
    for ang in (-34, 0, 34):
        th = math.radians(ang)
        pos = (0.345 * math.sin(th), -0.345 * math.cos(th) * 0.78 - 0.035, 0.84)
        _part(coll, 'Glyph%+d_Glow' % ang, root, lambda bm, p=pos, t=th: _gem(bm, 0.03, p, (0.9, 0.35, 1.5)), smooth=False)

    # ---- head: face, hair, expedition hat with a leaf sprig ----
    head = _empty(coll, 'Joint_Head', root, (0, 0, NECK_Z))
    _part(coll, 'Neck_Skin', head, lambda bm: _tube(bm, (0, 0, -0.06), (0, 0, 0.07), 0.055, 0.05, segs=12))

    def face(bm):
        _ellipsoid(bm, (0.155, 0.15, 0.175), (0, 0, 0.21), segs=18, rings=12)
        _ellipsoid(bm, (0.026, 0.03, 0.03), (0, 0.155, 0.195), segs=8, rings=6)                # nose
        for sx in (-1, 1):
            _ellipsoid(bm, (0.02, 0.03, 0.04), (sx * 0.155, 0.0, 0.2), segs=8, rings=6)         # ears
    _part(coll, 'Head_Skin', head, face)

    for s, sx in (('L', -1), ('R', 1)):
        _part(coll, 'Eye_%s_Dark' % s, head, lambda bm, x=sx: _ellipsoid(bm, (0.02, 0.012, 0.03), (x * 0.06, 0.143, 0.225), segs=8, rings=6))
        _part(coll, 'Brow_%s_Hair' % s, head, lambda bm, x=sx: _tube(bm, (x * 0.035, 0.148, 0.284), (x * 0.088, 0.14, 0.27), 0.009, 0.009, segs=6))

    def hair(bm):
        _ellipsoid(bm, (0.165, 0.16, 0.16), (0, -0.045, 0.24), segs=16, rings=10)
        _ellipsoid(bm, (0.12, 0.09, 0.16), (0, -0.12, 0.12), segs=12, rings=8)                  # nape tuft
    _part(coll, 'Hair_Hair', head, hair)

    def brim(bm):
        _ellipsoid(bm, (0.33, 0.33, 0.04), (0, 0, 0.335), segs=28, rings=8)
    _part(coll, 'HatBrim_Gear', head, brim)

    def crown(bm):
        _tube(bm, (0, 0, 0.335), (0, 0, 0.47), 0.178, 0.135, segs=20)
        _ellipsoid(bm, (0.135, 0.135, 0.07), (0, 0, 0.47), segs=20, rings=8)
    _part(coll, 'HatCrown_Gear', head, crown)
    _part(coll, 'HatBand_Cloak', head, lambda bm: _tube(bm, (0, 0, 0.35), (0, 0, 0.405), 0.18, 0.172, segs=20, caps=False))
    _part(coll, 'HatGem_Glow', head, lambda bm: _gem(bm, 0.03, (0, 0.18, 0.378), (1, 0.6, 1.2)), smooth=False)
    _part(coll, 'HatPin_Trim', head, lambda bm: _torus(bm, 0.036, 0.006, (0, 0.178, 0.378), axis=(0, 1, 0), segs=14, ring=6))

    def sprig(bm):
        _leaf(bm, (0.15, 0.04, 0.40), (0.55, 0.15, 0.85), 0.20, 0.085)
        _leaf(bm, (0.15, 0.04, 0.40), (0.95, 0.30, 0.30), 0.18, 0.08)
        _leaf(bm, (0.15, 0.04, 0.40), (0.15, -0.05, 1.0), 0.15, 0.07)
    _part(coll, 'Sprig_Leaf', head, sprig)

    # ---- arms: shoulder joint, short sleeve, bare forearm with bracer, gloved hand ----
    for s, sign in (('L', -1), ('R', 1)):
        j = _empty(coll, 'Joint_Arm' + s, root, (sign * ARM_X, 0, SHOULDER_Z))
        _part(coll, 'Sleeve_%s_Cloth' % s, j, lambda bm: _limb(bm, (0, 0, 0), (0, 0.012, -0.29), 0.088, 0.078))
        _part(coll, 'Forearm_%s_Skin' % s, j, lambda bm: _limb(bm, (0, 0.012, -0.28), (0, 0.035, -0.47), 0.056, 0.048))
        _part(coll, 'Bracer_%s_Gear' % s, j, lambda bm: _tube(bm, (0, 0.02, -0.34), (0, 0.032, -0.45), 0.067, 0.062, segs=14, caps=False))
        _part(coll, 'Cuff_%s_Trim' % s, j, lambda bm: _tube(bm, (0, 0.03, -0.455), (0, 0.033, -0.475), 0.066, 0.06, segs=14, caps=False))
        _part(coll, 'Hand_%s_Gear' % s, j, lambda bm: _ellipsoid(bm, (0.056, 0.05, 0.075), (0, 0.04, -0.55), segs=12, rings=8))
        _part(coll, 'Thumb_%s_Gear' % s, j, lambda bm, x=sign: _ellipsoid(bm, (0.02, 0.025, 0.04), (-x * 0.045, 0.06, -0.52), segs=8, rings=6))
    _vine(coll, bpy.data.objects['Joint_ArmL'], -0.30, -0.45, 0.058, 2.4)

    _watering_can(coll, bpy.data.objects['Joint_ArmR'])

    # ---- legs: trousers tucked into tall boots ----
    for s, sign in (('L', -1), ('R', 1)):
        j = _empty(coll, 'Joint_Leg' + s, root, (sign * LEG_X, 0, HIP_Z))
        _part(coll, 'Trousers_%s_Cloth' % s, j, lambda bm: _limb(bm, (0, 0, 0), (0, 0.005, -0.47), 0.108, 0.082))

        def boot(bm):
            _tube(bm, (0, 0, -0.50), (0, 0, -0.74), 0.098, 0.082, segs=16)
            _ellipsoid(bm, (0.088, 0.155, 0.065), (0, 0.055, -0.755), segs=14, rings=8)      # foot, sole at -HIP_Z
        _part(coll, 'Boot_%s_Gear' % s, j, boot)
        _part(coll, 'BootCuff_%s_Trim' % s, j, lambda bm: _tube(bm, (0, 0, -0.50), (0, 0, -0.535), 0.104, 0.1, segs=16, caps=False))
    return coll


def stats():
    coll = bpy.data.collections[NAME]
    tris = 0
    dg = bpy.context.evaluated_depsgraph_get()
    zmax = 0.0
    roles = {}
    for o in coll.all_objects:
        if o.type != 'MESH':
            continue
        me = o.evaluated_get(dg).to_mesh()
        me.calc_loop_triangles()
        tris += len(me.loop_triangles)
        roles[o.name.rsplit('_', 1)[-1]] = roles.get(o.name.rsplit('_', 1)[-1], 0) + 1
        for v in me.vertices:
            zmax = max(zmax, (o.matrix_world @ v.co).z)
        o.evaluated_get(dg).to_mesh_clear()
    return {'objects': len(coll.all_objects), 'tris': tris, 'height': round(zmax, 3), 'roles': roles}


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
