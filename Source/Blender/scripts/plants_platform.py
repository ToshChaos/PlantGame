"""T08: Geometry Nodes shelf-fungus platform plant (GN_Plant_Platform) + variant baking + jump check.

In Blender (via MCP execute_blender_code):
    S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
    exec(open(S + 'pipeline_common.py').read())
    exec(open(S + 'plants_platform.py').read())
    build_all()            # node group, 6 baked variants Plant_Platform_01..06, jump check, saves plants.blend

The plant grows along Blender +Z (= Unity +Y). A thin tapered stem rises through the middle; N pads (shallow dishes: rim at pad height, middle DISH lower, T48) spiral
out from it: pad k sits at height (k+1)/N * Rise, angle start + k*Twist (+ wobble), radius Reach * (0.25 + 0.75 k/(N-1)).
Each pad hangs off the stem on a short arm; rim spots (cyan accent, part id 1) ring every pad.
growth_t: R = z / Rise (0..1), G = part id (.67 main stem, 1 mushroom stalks + white spots, .92 caps = red in the shader), B = random tint.
Extra float attribute `padi` (pad index, pads only) is used by the bake to build COL_ boxes and by the jump check; FBX does not export it.
Reuses the node helpers and preview material of plants_bridge.py.
"""
import math
import random
import bpy
import bmesh
import mathutils

_S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
_ns = {'__name__': 'plants_bridge_helpers'}
exec(open(_S + 'plants_bridge.py').read(), _ns)
_B = _ns['_B']
make_material = _ns['make_material']
_empty = _ns['_empty']
MAT = _ns['MAT']

GROUP = 'GN_Plant_Platform'
SEGMENTS = 40
PAD_DEPTH = 0.18
DISH = 0.22                            # T48: how far the middle of a cap sits below its rim (task: 0.15-0.3 m)
MAX_RISE_STEP, MAX_HOP = 1.2, 3.2      # jump-delta limits (task file)


def _interface(tree):
    it = tree.interface
    it.new_socket('Geometry', in_out='OUTPUT', socket_type='NodeSocketGeometry')
    it.new_socket('Geometry', in_out='INPUT', socket_type='NodeSocketGeometry')
    spec = [
        ('Seed', 'NodeSocketInt', 1, 0, 9999),
        ('Pads', 'NodeSocketInt', 3, 2, 7),
        ('RiseTotal', 'NodeSocketFloat', 3.0, 2.0, 4.0),
        ('Reach', 'NodeSocketFloat', 4.0, 3.0, 6.0),
        ('PadSize', 'NodeSocketFloat', 1.8, 1.5, 2.6),
        ('Wobble', 'NodeSocketFloat', 0.3, 0.0, 0.6),
        ('StemThickness', 'NodeSocketFloat', 0.2, 0.1, 0.4),
        ('Twist', 'NodeSocketFloat', 0.3, 0.0, 0.5),        # radians between neighbouring pads (kept small so hops stay jumpable)
    ]
    for name, st, d, lo, hi in spec:
        s = it.new_socket(name, in_out='INPUT', socket_type=st)
        s.default_value = d
        s.min_value = lo
        s.max_value = hi


def build_group():
    old = bpy.data.node_groups.get(GROUP)
    if old:
        bpy.data.node_groups.remove(old)
    tree = bpy.data.node_groups.new(GROUP, 'GeometryNodeTree')
    _interface(tree)
    b = _B(tree)
    gi = b.node('NodeGroupInput')
    go = b.node('NodeGroupOutput')
    I = gi.outputs
    seed, pads, rise, reach, psize, wob, stemt, twist = (I[n] for n in (
        'Seed', 'Pads', 'RiseTotal', 'Reach', 'PadSize', 'Wobble', 'StemThickness', 'Twist'))

    def store(geo, name, value, domain):
        return b.make('GeometryNodeStoreNamedAttribute', {'Geometry': geo, 'Name': name, 'Value': value},
                      data_type='FLOAT', domain=domain)

    def xform(geo, trans):
        return b.make('GeometryNodeTransform', {'Geometry': geo, 'Translation': trans})

    def rnd(lo, hi, sd):
        return b.en(b.make('FunctionNodeRandomValue', {'Min': lo, 'Max': hi, 'Seed': b.m('ADD', seed, sd)}, data_type='FLOAT'))

    # ---- per-pad fields (evaluated on the pad points, index = pad number)
    idx = b.make('GeometryNodeInputIndex').outputs[0]
    z = b.m('MULTIPLY', b.m('DIVIDE', b.m('ADD', idx, 1.0), pads), rise)
    frac = b.m('DIVIDE', idx, b.m('SUBTRACT', pads, 1.0))
    start = b.m('MULTIPLY', seed, 2.399)
    theta = b.m('ADD', b.m('ADD', start, b.m('MULTIPLY', idx, twist)), b.m('MULTIPLY', b.m('MULTIPLY', rnd(-1.0, 1.0, 5), 0.12), wob))
    r = b.m('ADD', b.m('MULTIPLY', reach, b.m('ADD', 0.25, b.m('MULTIPLY', frac, 0.75))),
            b.m('MULTIPLY', b.m('MULTIPLY', rnd(-1.0, 1.0, 7), 0.3), wob))
    size = b.m('MULTIPLY', psize, rnd(1.0, 1.25, 9))
    padpos = b.xyz(b.m('MULTIPLY', r, b.m('COSINE', theta)), b.m('MULTIPLY', r, b.m('SINE', theta)), z)

    line = b.make('GeometryNodeMeshLine', {'Count': pads})
    pts = b.make('GeometryNodeMeshToPoints', {'Mesh': line}, mode='VERTICES')
    pad_pts = b.make('GeometryNodeSetPosition', {'Geometry': pts, 'Position': padpos})
    # mushroom stalk: starts on the main stem 0.9 m below the cap (never under the ground) and slopes up to the cap's underside
    zs = b.m('MAXIMUM', b.m('SUBTRACT', z, 0.9), 0.05)
    dz = b.m('SUBTRACT', b.m('SUBTRACT', z, 0.42), zs)
    slen = b.m('SQRT', b.m('ADD', b.m('MULTIPLY', r, r), b.m('MULTIPLY', dz, dz)))
    pitch = b.m('ARCTAN2', dz, r)
    axis_pts = b.make('GeometryNodeSetPosition', {'Geometry': pts, 'Position': b.xyz(0, 0, zs)})

    parts = []
    # ---- caps: a shallow dish (rim at the point's height, middle DISH lower) over an inverted cone, like a bracket mushroom
    cyl = b.make('GeometryNodeMeshCylinder', {'Vertices': 32, 'Radius': 0.5, 'Depth': PAD_DEPTH, 'Fill Segments': 6}, fill_type='TRIANGLE_FAN')
    flat = xform(cyl, b.xyz(0, 0, -PAD_DEPTH / 2))
    pxyz = b.make('ShaderNodeSeparateXYZ', {'Vector': b.make('GeometryNodeInputPosition')})
    r2 = b.m('ADD', b.m('MULTIPLY', pxyz.outputs['X'], pxyz.outputs['X']), b.m('MULTIPLY', pxyz.outputs['Y'], pxyz.outputs['Y']))
    bend = b.m('SUBTRACT', b.m('MULTIPLY', r2, 4.0 * DISH), DISH)        # -DISH in the middle, 0 at the rim (r = 0.5)
    disc = b.make('GeometryNodeSetPosition', {'Geometry': flat, 'Offset': b.xyz(0, 0, bend)})
    under = xform(b.make('GeometryNodeMeshCone', {'Vertices': 32, 'Radius Top': 0.5, 'Radius Bottom': 0.08, 'Depth': 0.36, 'Side Segments': 3}),
                  b.xyz(0, 0, -PAD_DEPTH - 0.16 - DISH))
    cap_join = b.make('GeometryNodeJoinGeometry')
    # T67: rolled rim lip (torus) (gills come from the shader)
    lip = b.make('GeometryNodeCurveToMesh', {'Curve': b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 32, 'Radius': 0.5}),
                                             'Profile Curve': b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 6, 'Radius': 0.045})})
    tree.links.new(xform(lip, b.xyz(0, 0, -0.04)).outputs[0], cap_join.inputs[0])
    tree.links.new(disc.outputs[0], cap_join.inputs[0])
    tree.links.new(under.outputs[0], cap_join.inputs[0])
    pi = b.make('GeometryNodeInstanceOnPoints', {'Points': pad_pts, 'Instance': cap_join, 'Scale': b.xyz(size, size, 1.0)})
    pi = store(pi, 'part', 0.92, 'INSTANCE')
    pi = store(pi, 'padi', idx, 'INSTANCE')
    parts.append(pi.outputs[0])

    # ---- user request: a mushroom patch. Every cap has its own stalk rising straight from the ground under it (no central stem), so the
    # caps end up at different heights. Stalk = tapered cone with a swollen foot and a collar ring.
    px_ = b.m('MULTIPLY', r, b.m('COSINE', theta))
    py_ = b.m('MULTIPLY', r, b.m('SINE', theta))
    slen2 = b.m('MAXIMUM', b.m('SUBTRACT', z, 0.45), 0.3)                                  # stalk top sits inside the cap's underside
    foot_pts = b.make('GeometryNodeSetPosition', {'Geometry': pts, 'Position': b.xyz(px_, py_, 0)})
    ring_pts = b.make('GeometryNodeSetPosition', {'Geometry': pts, 'Position': b.xyz(px_, py_, b.m('MULTIPLY', slen2, 0.6))})
    sw = b.m('ADD', 0.32, b.m('MULTIPLY', stemt, 0.5))                                       # stalk diameter scale
    cone = xform(b.make('GeometryNodeMeshCone', {'Vertices': 16, 'Radius Top': 0.5, 'Radius Bottom': 0.78, 'Depth': 1.0, 'Side Segments': 6}), b.xyz(0, 0, 0.0))   # the GN cone's base sits at z = 0
    sti = b.make('GeometryNodeInstanceOnPoints', {'Points': foot_pts, 'Instance': cone, 'Scale': b.xyz(sw, sw, slen2)})
    parts.append(store(sti, 'part', 1.0, 'INSTANCE').outputs[0])
    foot = b.make('GeometryNodeMeshUVSphere', {'Segments': 16, 'Rings': 8, 'Radius': 0.5})
    fti = b.make('GeometryNodeInstanceOnPoints', {'Points': foot_pts, 'Instance': foot, 'Scale': b.xyz(b.m('MULTIPLY', sw, 1.9), b.m('MULTIPLY', sw, 1.9), 0.5)})
    parts.append(store(fti, 'part', 1.0, 'INSTANCE').outputs[0])
    ringg = b.make('GeometryNodeMeshUVSphere', {'Segments': 16, 'Rings': 6, 'Radius': 0.5})
    rgi = b.make('GeometryNodeInstanceOnPoints', {'Points': ring_pts, 'Instance': ringg, 'Scale': b.xyz(b.m('MULTIPLY', sw, 1.45), b.m('MULTIPLY', sw, 1.45), 0.1)})
    parts.append(store(rgi, 'part', 1.0, 'INSTANCE').outputs[0])
    # small decorative mushrooms at the foot of the patch, so there are always three (variants with two walkable caps get one extra); no collider, padi = -1
    bth = b.m('ADD', theta, 2.4)
    br = b.m('ADD', r, 1.7)
    baby_sel = b.m('LESS_THAN', idx, b.m('SUBTRACT', 3.0, pads))
    baby_pts = b.make('GeometryNodeSetPosition', {'Geometry': pts, 'Position': b.xyz(b.m('MULTIPLY', br, b.m('COSINE', bth)), b.m('MULTIPLY', br, b.m('SINE', bth)), 0.85)})
    baby_foot = b.make('GeometryNodeSetPosition', {'Geometry': pts, 'Position': b.xyz(b.m('MULTIPLY', br, b.m('COSINE', bth)), b.m('MULTIPLY', br, b.m('SINE', bth)), 0)})
    bcap = b.make('GeometryNodeInstanceOnPoints', {'Points': baby_pts, 'Selection': baby_sel, 'Instance': cap_join, 'Scale': b.xyz(0.7, 0.7, 0.8)})
    bcap = store(store(bcap, 'part', 0.92, 'INSTANCE'), 'padi', -1.0, 'INSTANCE')
    parts.append(bcap.outputs[0])
    bst = b.make('GeometryNodeInstanceOnPoints', {'Points': baby_foot, 'Selection': baby_sel, 'Instance': cone, 'Scale': b.xyz(0.3, 0.3, 0.62)})
    parts.append(store(bst, 'part', 1.0, 'INSTANCE').outputs[0])

    # ---- white spots on top of each cap: two small rings (5 + 3), built once in unit-cap space and scaled with the cap
    spot = xform(b.make('GeometryNodeMeshCylinder', {'Vertices': 8, 'Radius': 0.07, 'Depth': 0.04}), b.xyz(0, 0, 0.02))
    spot_join = b.make('GeometryNodeJoinGeometry')
    for res_, rad_ in ((5, 0.3), (3, 0.13)):
        rp = b.make('GeometryNodeCurveToPoints', {'Curve': b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': res_, 'Radius': rad_}),
                                                  'Count': res_}, mode='COUNT')
        rp_low = b.make('GeometryNodeSetPosition', {'Geometry': rp.outputs['Points'], 'Offset': b.xyz(0, 0, 4.0 * DISH * rad_ * rad_ - DISH)})   # follow the dish
        ri = b.make('GeometryNodeInstanceOnPoints', {'Points': rp_low, 'Instance': spot})
        tree.links.new(ri.outputs[0], spot_join.inputs[0])
    spots = b.make('GeometryNodeRealizeInstances', {'Geometry': spot_join})
    si = b.make('GeometryNodeInstanceOnPoints', {'Points': pad_pts, 'Instance': spots, 'Scale': b.xyz(size, size, 1.0)})
    parts.append(store(si, 'part', 1.0, 'INSTANCE').outputs[0])

    join = b.make('GeometryNodeJoinGeometry')
    for p in parts:
        tree.links.new(p, join.inputs[0])
    real = b.make('GeometryNodeRealizeInstances', {'Geometry': join})
    pz = b.make('ShaderNodeSeparateXYZ', {'Vector': b.make('GeometryNodeInputPosition')}).outputs['Z']
    tt = b.m('DIVIDE', pz, rise, clamp=True)
    part = b.make('GeometryNodeInputNamedAttribute', data_type='FLOAT', inputs={'Name': 'part'}).outputs['Attribute']
    tint = b.en(b.make('FunctionNodeRandomValue', {'Min': 0.0, 'Max': 1.0, 'Seed': seed}, data_type='FLOAT'))
    col = b.make('FunctionNodeCombineColor', {'Red': tt, 'Green': part, 'Blue': tint}, mode='RGB')
    st = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': real, 'Name': 'growth_t', 'Value': col},
                data_type='FLOAT_COLOR', domain='POINT')
    mat = b.make('GeometryNodeSetMaterial', {'Geometry': st, 'Material': make_material()})
    tree.links.new(mat.outputs[0], go.inputs[0])
    return tree


# ---------------------------------------------------------------- variants
VARIANTS = [
    # Pads, RiseTotal, Reach, PadSize, Wobble, Stem  (few, big steps: 2-3 pads)
    (2, 2.4, 3.4, 2.1, 0.25, 0.18),
    (2, 2.4, 3.8, 2.0, 0.30, 0.22),
    (3, 3.6, 5.0, 1.9, 0.35, 0.26),
    (3, 3.6, 5.8, 1.8, 0.30, 0.20),
    (2, 2.4, 4.0, 2.3, 0.40, 0.30),
    (3, 3.3, 3.8, 2.0, 0.20, 0.16),
]


def variant_params(n):
    pads, rise, reach, size, wob, stem = VARIANTS[(n - 1) % len(VARIANTS)]
    return dict(Seed=n * 11 + 7, Pads=pads, RiseTotal=rise, Reach=reach, PadSize=size, Wobble=wob, StemThickness=stem,
                Twist=min(0.5, 1.4 / reach))


def _modifier_obj(name, params):
    me = bpy.data.meshes.new(name + '_src')
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    for k, v in params.items():
        bpy.data.node_groups[GROUP].interface.items_tree[k].default_value = v
    md = ob.modifiers.new(GROUP, 'NODES')
    md.node_group = bpy.data.node_groups[GROUP]
    return ob


def _clear(name):
    coll = bpy.data.collections.get(name)
    if coll:
        for o in list(coll.objects):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(coll)


def pad_info(me):
    """Per pad (ordered base -> tip): dict(centre=(x,y), top=z, radius=r) measured from the baked mesh."""
    ca = me.color_attributes['growth_t']
    padi = me.attributes['padi'].data
    groups = {}
    for i, v in enumerate(me.vertices):
        if 0.88 < ca.data[i].color[1] < 0.96 and padi[i].value > -0.5:   # walkable caps only (decorative baby caps have padi -1)
            groups.setdefault(int(round(padi[i].value)), []).append(v.co)
    out = []
    for k in sorted(groups):
        cs = groups[k]
        cx = sum(c.x for c in cs) / len(cs)
        cy = sum(c.y for c in cs) / len(cs)
        out.append(dict(centre=(cx, cy), top=max(c.z for c in cs),
                        radius=max(math.hypot(c.x - cx, c.y - cy) for c in cs)))
    return out


def jump_check(pads):
    """Returns (ok, report). Pad 0 is measured from the ground (z = 0, stem axis), pad k from pad k-1."""
    errs, worst_rise, worst_hop = [], 0.0, 0.0
    prev_c, prev_z = (0.0, 0.0), 0.0
    for k, p in enumerate(pads):
        rise = p['top'] - prev_z
        hop = math.hypot(p['centre'][0] - prev_c[0], p['centre'][1] - prev_c[1]) if k else 0.0
        worst_rise, worst_hop = max(worst_rise, rise), max(worst_hop, hop)
        if rise > MAX_RISE_STEP + 1e-6 or rise < 0.2:
            errs.append('pad %d rise %.2f' % (k, rise))
        if hop > MAX_HOP + 1e-6:
            errs.append('pad %d hop %.2f' % (k, hop))
        if 2 * p['radius'] < 1.5:
            errs.append('pad %d diameter %.2f < 1.5' % (k, 2 * p['radius']))
        prev_c, prev_z = p['centre'], p['top']
    last = pads[-1]
    reach = math.hypot(*last['centre'])
    if not 2.0 <= last['top'] <= 4.05:
        errs.append('total rise %.2f outside 2-4 m' % last['top'])
    if not 3.0 <= reach <= 6.2:
        errs.append('reach %.2f outside 3-6 m' % reach)
    return not errs, dict(pads=len(pads), total_rise=round(last['top'], 2), reach=round(reach, 2),
                          worst_rise=round(worst_rise, 2), worst_hop=round(worst_hop, 2), errors=errs)


def _box(name, centre, size, main, coll):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= size[0]
        v.co.y *= size[1]
        v.co.z *= size[2]
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    coll.objects.link(o)
    o.parent = main
    o.matrix_parent_inverse = main.matrix_world.inverted()
    o.matrix_world = main.matrix_world @ mathutils.Matrix.Translation(centre)
    o.display_type = 'WIRE'
    o.hide_render = True
    return o


def bake_variant(n, offset_x=0.0):
    name = 'Plant_Platform_%02d' % n
    nn = '%02d' % n
    _clear(name)
    p = variant_params(n)
    tmp = _modifier_obj('tmp_' + name, p)
    ev = tmp.evaluated_get(bpy.context.evaluated_depsgraph_get())
    me = bpy.data.meshes.new_from_object(ev)
    me.name = name
    bpy.data.objects.remove(tmp, do_unlink=True)
    coll = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(coll)
    main = bpy.data.objects.new(name, me)
    coll.objects.link(main)
    main.location = (offset_x, 0, 0)
    bm = bmesh.new()
    bm.from_mesh(me)
    closed = [f for f in bm.faces if all(len(e.link_faces) == 2 for e in f.edges)]
    bmesh.ops.recalc_face_normals(bm, faces=closed)
    bm.to_mesh(me)
    bm.free()
    me.materials.clear()
    me.materials.append(bpy.data.materials[MAT])
    for poly in me.polygons:
        poly.use_smooth = False
    pads = pad_info(me)
    mk = lambda nm, loc: _empty(nm, loc, main, coll)
    last = pads[-1]
    tip = mathutils.Vector((last['centre'][0], last['centre'][1], last['top']))
    out = mathutils.Vector((tip.x, tip.y, 0)).normalized() * 0.4
    mk('Anchor_%s@P' % nn, mathutils.Vector((0, 0, 0)))
    mk('Tip_%s@P' % nn, tip)
    mk('Land_%s@P' % nn, tip + mathutils.Vector((out.x, out.y, 0)))
    for k, pd in enumerate(pads):                       # flat-top collider per pad, ordered base -> tip
        d = 2 * pd['radius'] * 0.92
        _box('COL_%s_%d@P' % (nn, k), (pd['centre'][0], pd['centre'][1], pd['top'] - DISH * 0.55 - 0.1), (d, d, 0.2), main, coll)   # top halfway down the dish
    main['kind'] = 'Platform'
    main['params'] = str({k: round(v, 3) for k, v in p.items()})
    return main


def build_all(count=6, save=True):
    build_group()
    mains = [bake_variant(i + 1, offset_x=i * 14.0) for i in range(count)]
    reports = {}
    for m in mains:
        ok, rep = jump_check(pad_info(m.data))
        reports[m.name] = dict(ok=ok, **rep)
    old = bpy.data.objects.get('GN_Platform_Source')
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    src = _modifier_obj('GN_Platform_Source', variant_params(1))
    src.location = (-12, 0, 0)
    if save:
        bpy.ops.wm.save_as_mainfile(filepath='/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/plants.blend')
    return reports
