"""T07: Geometry Nodes beanstalk ladder (GN_Plant_Ladder) + variant baking.

In Blender (via MCP execute_blender_code):
    S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
    exec(open(S + 'plants_ladder.py').read())
    build_all()            # node group, 6 baked variants Plant_Ladder_01..06, saves plants.blend

The ladder grows along Blender +Z (= Unity +Y). The climbable face is Blender +Y (Unity +Z); leaves stay on the back (-Y),
so the front column has >= 0.8 m clearance. T51: no top platform; the stalk ends at z = Height right beside the target island's rim and `Land` is 1.9 m in front of the stalk top.
growth_t: R = z / (Height + VINE_EXT) (0..1; the stalk tapers into a thin vine above Height), G = part id (0 stalk, .34 vines + bean pods, .67 rungs/leaves, ~.8 beans (yellow in the shader)), B = random tint.
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

GROUP = 'GN_Plant_Ladder'
SEGMENTS = 64
VINE_EXT = 1.4         # the stalk keeps going this far above the top as a thin vine
TAPER_LEN = 3.0        # the trunk starts narrowing this far below the top
VINE_R = 0.035         # vine radius at the very tip
LAND_Y = 1.9           # Land: where the climber steps off, in front of the stalk top (on the target island)


def _interface(tree):
    it = tree.interface
    it.new_socket('Geometry', in_out='OUTPUT', socket_type='NodeSocketGeometry')
    it.new_socket('Geometry', in_out='INPUT', socket_type='NodeSocketGeometry')
    spec = [
        ('Seed', 'NodeSocketInt', 1, 0, 9999),
        ('Height', 'NodeSocketFloat', 6.0, 4.0, 9.0),
        ('Twist', 'NodeSocketFloat', 0.7, 0.0, 1.5),
        ('StalkRadius', 'NodeSocketFloat', 0.18, 0.08, 0.3),
        ('RungSpacing', 'NodeSocketFloat', 0.5, 0.4, 0.7),
        ('LeafSize', 'NodeSocketFloat', 1.0, 0.4, 1.8),
        ('Sway', 'NodeSocketFloat', 0.25, 0.0, 0.5),
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
    seed, height, twist, rad, spacing, leafs, sway = (I[n] for n in (
        'Seed', 'Height', 'Twist', 'StalkRadius', 'RungSpacing', 'LeafSize', 'Sway'))

    def store_part(geo, value, domain='POINT'):
        return b.make('GeometryNodeStoreNamedAttribute', {'Geometry': geo, 'Name': 'part', 'Value': value},
                      data_type='FLOAT', domain=domain)

    def xform(geo, trans):
        return b.make('GeometryNodeTransform', {'Geometry': geo, 'Translation': trans})

    # ---- stalk path: vertical, lateral sway fading to 0 at both ends (top stays above the anchor)
    def sway_curve(top, count):
        ln = b.make('GeometryNodeCurvePrimitiveLine', {'End': b.xyz(0, 0, top)})
        rs = b.make('GeometryNodeResampleCurve', {'Curve': ln, 'Count': count}, mode='COUNT')
        tt_ = b.make('GeometryNodeSplineParameter').outputs['Factor']
        zc = b.m('MULTIPLY', tt_, top)
        sn = b.m('SINE', b.m('MULTIPLY', b.m('MINIMUM', b.m('DIVIDE', zc, height), 1.0), math.pi))
        ps = b.make('GeometryNodeInputPosition').outputs[0]
        nx = b.make('ShaderNodeTexNoise', {'Vector': ps, 'W': b.m('MULTIPLY', seed, 3.7), 'Scale': 0.25}, noise_dimensions='4D')
        ny = b.make('ShaderNodeTexNoise', {'Vector': ps, 'W': b.m('MULTIPLY', seed, 5.3), 'Scale': 0.25}, noise_dimensions='4D')
        ox = b.m('MULTIPLY', b.m('MULTIPLY', b.m('SUBTRACT', nx.outputs['Fac'], 0.5), 2.0), b.m('MULTIPLY', sway, sn))
        oy = b.m('MULTIPLY', b.m('MULTIPLY', b.m('SUBTRACT', ny.outputs['Fac'], 0.5), 2.0), b.m('MULTIPLY', sway, sn))
        return b.make('GeometryNodeSetPosition', {'Geometry': rs, 'Offset': b.xyz(ox, oy, 0)}), zc

    base, _ = sway_curve(height, SEGMENTS)
    # the stalk itself runs VINE_EXT above the top and tapers to a thin vine (climbing, rungs and pods use `base`)
    top2 = b.m('ADD', height, VINE_EXT)
    stalk_path, zz_ = sway_curve(top2, b.m('ROUND', b.m('MULTIPLY', b.m('DIVIDE', top2, height), SEGMENTS)))
    knots = b.m('ADD', 1.0, b.m('MULTIPLY', 0.22, b.m('MAXIMUM', b.m('SINE', b.m('ADD', b.m('MULTIPLY', zz_, 4.2), b.m('MULTIPLY', seed, 0.7))), 0.0)))   # T67: knobbly nodes
    zf = b.m('DIVIDE', zz_, height)
    flare = b.m('ADD', 1.0, b.m('MULTIPLY', 0.7, b.m('POWER', b.m('MAXIMUM', b.m('SUBTRACT', 1.0, b.m('MULTIPLY', zf, 9.0)), 0.0), 2.0)))   # swollen root
    body = b.m('MULTIPLY', b.m('MULTIPLY', rad, b.m('SUBTRACT', 1.15, b.m('MULTIPLY', zf, 0.35))), b.m('MULTIPLY', knots, flare))
    # smoothstep from the trunk into the vine: starts TAPER_LEN below the top, thin vine at the very tip
    u = b.m('DIVIDE', b.m('SUBTRACT', zz_, b.m('SUBTRACT', height, TAPER_LEN)), TAPER_LEN + VINE_EXT, clamp=True)
    sm = b.m('MULTIPLY', b.m('MULTIPLY', u, u), b.m('SUBTRACT', 3.0, b.m('MULTIPLY', u, 2.0)))
    taper = b.m('ADD', b.m('MULTIPLY', body, b.m('SUBTRACT', 1.0, sm)), b.m('MULTIPLY', VINE_R, sm))

    parts = []
    # ---- stalk
    prof = b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 10, 'Radius': 1.0})
    stalk = b.make('GeometryNodeCurveToMesh', {'Curve': stalk_path, 'Profile Curve': prof, 'Scale': taper, 'Fill Caps': True})
    parts.append(store_part(stalk, 0.0).outputs[0])

    # ---- two guide vines spiralling round the stalk
    hres = b.make('GeometryNodeResampleCurve', {'Curve': base, 'Count': 64}, mode='COUNT')
    ht = b.make('GeometryNodeSplineParameter').outputs['Factor']
    hz = b.m('MULTIPLY', ht, height)
    circ = b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 4, 'Radius': 0.04})
    for k in (0.0, math.pi):
        ang = b.m('ADD', b.m('MULTIPLY', hz, twist), k)
        ro = b.m('ADD', rad, 0.09)
        off = b.xyz(b.m('MULTIPLY', b.m('COSINE', ang), ro), b.m('MULTIPLY', b.m('SINE', ang), ro), 0)
        vine = b.make('GeometryNodeSetPosition', {'Geometry': hres, 'Offset': off})
        m = b.make('GeometryNodeCurveToMesh', {'Curve': vine, 'Profile Curve': circ})
        parts.append(store_part(m, 0.34).outputs[0])

    # ---- rungs (front, climbable side) between z = 0.3 and Height - 0.45
    rcount = b.m('MAXIMUM', b.m('ROUND', b.m('DIVIDE', height, spacing)), 2.0)
    rpts = b.make('GeometryNodeCurveToPoints', {'Curve': base, 'Count': rcount}, mode='COUNT')
    pz = b.make('ShaderNodeSeparateXYZ', {'Vector': b.make('GeometryNodeInputPosition')}).outputs['Z']
    sel = b.m('MULTIPLY', b.m('GREATER_THAN', pz, 0.3), b.m('LESS_THAN', pz, b.m('SUBTRACT', height, 0.45)))
    # rounded step: a slab smoothed with one Catmull-Clark level (tri budget), so every corner and edge is soft (the plain box looked artificial)
    cube = b.make('GeometryNodeMeshCube', {'Size': b.xyz(0.86, 0.32, 0.1)})
    cube = b.make('GeometryNodeSubdivisionSurface', {'Mesh': cube, 'Level': 1})
    rung = xform(cube, b.xyz(0, b.m('ADD', rad, 0.05), 0))
    rrot = b.make('FunctionNodeRandomValue', {'Min': (0, 0, -0.2), 'Max': (0, 0, 0.2), 'Seed': b.m('ADD', seed, 7)},
                  data_type='FLOAT_VECTOR')
    ri = b.make('GeometryNodeInstanceOnPoints', {'Points': rpts.outputs['Points'], 'Selection': sel, 'Instance': rung,
                                                 'Rotation': b.en(rrot)})
    parts.append(store_part(ri, 0.67, 'INSTANCE').outputs[0])

    # ---- leaves and blossoms on the back side (-Y)
    lcount = b.m('MAXIMUM', b.m('ROUND', b.m('MULTIPLY', height, 1.6)), 1.0)
    lf = b.make('GeometryNodeFillCurve', {'Curve': b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 12, 'Radius': 0.3})}, mode='N-gons')
    lf = b.make('GeometryNodeSubdivideMesh', {'Mesh': lf, 'Level': 1})
    lf = b.make('GeometryNodeTransform', {'Geometry': lf, 'Scale': b.xyz(0.75, 1.9, 1.0), 'Translation': b.xyz(0, -0.55, 0)})   # T67: long pointed blade, base at the stalk
    ly = b.make('ShaderNodeSeparateXYZ', {'Vector': b.make('GeometryNodeInputPosition')}).outputs['Y']
    lx = b.make('ShaderNodeSeparateXYZ', {'Vector': b.make('GeometryNodeInputPosition')}).outputs['X']
    droop = b.m('ADD', b.m('MULTIPLY', b.m('MULTIPLY', ly, ly), -0.35), b.m('MULTIPLY', b.m('MULTIPLY', lx, lx), 1.6))   # drooping tip, cupped sides
    lf = b.make('GeometryNodeSetPosition', {'Geometry': lf, 'Offset': b.xyz(0, 0, droop)})
    leaf_geo = xform(lf, b.xyz(0, b.m('MULTIPLY', rad, -1.6), 0))
    for cnt, geo, pid, smin, smax, sd in ((lcount, leaf_geo, 0.67, 0.7, 1.3, 11),):
        pts = b.make('GeometryNodeCurveToPoints', {'Curve': base, 'Count': cnt}, mode='COUNT')
        rv = b.make('FunctionNodeRandomValue', {'Min': (-0.4, 0.0, -1.2), 'Max': (0.4, 0.0, 1.2),
                                                'Seed': b.m('ADD', seed, sd)}, data_type='FLOAT_VECTOR')
        rs = b.make('FunctionNodeRandomValue', {'Min': smin, 'Max': smax, 'Seed': b.m('ADD', seed, sd + 5)}, data_type='FLOAT')
        sc = b.m('MULTIPLY', b.en(rs), leafs)
        ip = b.make('GeometryNodeInstanceOnPoints', {'Points': pts.outputs['Points'], 'Instance': geo,
                                                     'Rotation': b.en(rv), 'Scale': b.xyz(sc, sc, sc)})
        parts.append(store_part(ip, pid, 'INSTANCE').outputs[0])

    # ---- curling tendrils (T67)
    sp = b.make('GeometryNodeCurveSpiral', {'Resolution': 6, 'Rotations': 2.2, 'Start Radius': 0.12, 'End Radius': 0.01, 'Height': 0.1})
    sp = b.make('GeometryNodeTransform', {'Geometry': sp, 'Rotation': b.xyz(math.pi / 2, 0, 0)})
    tm = b.make('GeometryNodeCurveToMesh', {'Curve': sp, 'Profile Curve': b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 3, 'Radius': 0.014})})
    tcount = b.m('MAXIMUM', b.m('ROUND', b.m('MULTIPLY', height, 1.4)), 3.0)
    tpts = b.make('GeometryNodeCurveToPoints', {'Curve': base, 'Count': tcount}, mode='COUNT')
    trv = b.make('FunctionNodeRandomValue', {'Min': (-0.5, 0.0, -2.6), 'Max': (0.5, 0.0, 2.6), 'Seed': b.m('ADD', seed, 71)}, data_type='FLOAT_VECTOR')
    tmesh = xform(tm, b.xyz(0, b.m('MULTIPLY', rad, -0.9), 0))
    tins = b.make('GeometryNodeInstanceOnPoints', {'Points': tpts.outputs['Points'], 'Instance': tmesh, 'Rotation': b.en(trv),
                                                   'Scale': b.xyz(1.6, 1.6, 1.6)})
    parts.append(store_part(tins, 0.34, 'INSTANCE').outputs[0])

    # ---- bean pods hanging on the back side: slim closed green pods (a seam ridge, a curled tip) and two open pods (one low, one near the top) showing three yellow beans
    pcount = b.m('MAXIMUM', b.m('ROUND', b.m('MULTIPLY', height, 1.5)), 3.0)

    def pod_shape(open_pod):
        body = b.make('GeometryNodeMeshUVSphere', {'Segments': 8, 'Rings': 7, 'Radius': 0.5})
        body = xform(b.make('GeometryNodeTransform', {'Geometry': body, 'Scale': b.xyz(0.2, 0.17, 0.5)}), b.xyz(0, 0, -0.3))
        body = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': body, 'Name': 'part', 'Value': 0.34}, data_type='FLOAT', domain='POINT')
        pj = b.make('GeometryNodeJoinGeometry')
        tree.links.new(body.outputs[0], pj.inputs[0])
        ridge = b.make('GeometryNodeMeshUVSphere', {'Segments': 6, 'Rings': 8, 'Radius': 0.5})                    # seam along the front of the pod
        ridge = xform(b.make('GeometryNodeTransform', {'Geometry': ridge, 'Scale': b.xyz(0.04, 0.05, 0.46)}), b.xyz(0, 0.075, -0.3))
        ridge = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': ridge, 'Name': 'part', 'Value': 0.67}, data_type='FLOAT', domain='POINT')
        tree.links.new(ridge.outputs[0], pj.inputs[0])
        tip = b.make('GeometryNodeMeshUVSphere', {'Segments': 6, 'Rings': 4, 'Radius': 0.5})                       # little curled stem end
        tip = xform(b.make('GeometryNodeTransform', {'Geometry': tip, 'Scale': b.xyz(0.05, 0.05, 0.14)}), b.xyz(0, 0, -0.02))
        tip = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': tip, 'Name': 'part', 'Value': 0.67}, data_type='FLOAT', domain='POINT')
        tree.links.new(tip.outputs[0], pj.inputs[0])
        if open_pod:
            for zz in (-0.14, -0.3, -0.46):
                bean = b.make('GeometryNodeMeshUVSphere', {'Segments': 8, 'Rings': 5, 'Radius': 0.085})
                bean = xform(bean, b.xyz(0, 0.085, zz))
                bean = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': bean, 'Name': 'part', 'Value': 0.8}, data_type='FLOAT', domain='POINT')
                tree.links.new(bean.outputs[0], pj.inputs[0])
        return xform(b.make('GeometryNodeRealizeInstances', {'Geometry': pj}), b.xyz(0, b.m('MULTIPLY', rad, -1.2), 0))

    ppts = b.make('GeometryNodeCurveToPoints', {'Curve': base, 'Count': pcount}, mode='COUNT')
    prv = b.make('FunctionNodeRandomValue', {'Min': (-0.55, 0.0, -1.3), 'Max': (-0.15, 0.0, 1.3), 'Seed': b.m('ADD', seed, 53)}, data_type='FLOAT_VECTOR')
    psz = b.en(b.make('FunctionNodeRandomValue', {'Min': 1.3, 'Max': 1.9, 'Seed': b.m('ADD', seed, 59)}, data_type='FLOAT'))
    pidx = b.make('GeometryNodeInputIndex').outputs[0]
    is_open = b.m('MULTIPLY', b.m('GREATER_THAN', pidx, 2.5), b.m('LESS_THAN', pidx, 3.5))
    top_open = b.m('MULTIPLY', b.m('GREATER_THAN', pidx, b.m('SUBTRACT', pcount, 3.5)), b.m('LESS_THAN', pidx, b.m('SUBTRACT', pcount, 2.5)))   # user request: a second open pod near the top (second from last)
    is_open = b.m('MAXIMUM', is_open, top_open)
    for open_pod, sel_ in ((False, b.m('SUBTRACT', 1.0, is_open)), (True, is_open)):
        pins = b.make('GeometryNodeInstanceOnPoints', {'Points': ppts.outputs['Points'], 'Selection': sel_, 'Instance': pod_shape(open_pod),
                                                       'Rotation': b.en(prv), 'Scale': b.xyz(psz, psz, psz)})
        parts.append(pins.outputs[0])

    # (T51: no platform at the top any more: the climber steps straight onto the target island at `Land`)

    join = b.make('GeometryNodeJoinGeometry')
    for p in parts:
        tree.links.new(p, join.inputs[0])
    real = b.make('GeometryNodeRealizeInstances', {'Geometry': join})
    pz2 = b.make('ShaderNodeSeparateXYZ', {'Vector': b.make('GeometryNodeInputPosition')}).outputs['Z']
    tt = b.m('DIVIDE', pz2, b.m('ADD', height, VINE_EXT), clamp=True)
    part = b.make('GeometryNodeInputNamedAttribute', data_type='FLOAT', inputs={'Name': 'part'}).outputs['Attribute']
    tint = b.en(b.make('FunctionNodeRandomValue', {'Min': 0.0, 'Max': 1.0, 'Seed': seed}, data_type='FLOAT'))
    col = b.make('FunctionNodeCombineColor', {'Red': tt, 'Green': part, 'Blue': tint}, mode='RGB')
    st = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': real, 'Name': 'growth_t', 'Value': col},
                data_type='FLOAT_COLOR', domain='POINT')
    mat = b.make('GeometryNodeSetMaterial', {'Geometry': st, 'Material': make_material()})
    tree.links.new(mat.outputs[0], go.inputs[0])
    return tree


# ---------------------------------------------------------------- variants
def variant_params(n):
    r = random.Random(2000 + n)
    heights = [8.5, 9.5, 11.0, 11.5, 12.0, 12.3]   # T51: taller (variant 3 = 11 m is the generator's LadderHeight)
    return dict(Seed=n * 13 + 5, Height=heights[(n - 1) % 6], Twist=r.uniform(0.3, 1.2),
                StalkRadius=r.uniform(0.17, 0.23), RungSpacing=r.uniform(0.48, 0.6),
                LeafSize=r.uniform(0.7, 1.4), Sway=r.uniform(0.1, 0.4))


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


def _box(name, centre, size, parent, coll, main):
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
    name = 'Plant_Ladder_%02d' % n
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
    H = p['Height']
    mk = lambda nm, loc: _empty(nm, loc, main, coll)
    mk('Anchor_%s@L' % nn, mathutils.Vector((0, 0, 0)))
    mk('Tip_%s@L' % nn, mathutils.Vector((0, 0, H)))
    mk('Land_%s@L' % nn, mathutils.Vector((0, LAND_Y, H)))
    # one climb trigger: a column in front of the stalk (line at 0.5 m), reaching 1.2 m above the top so W keeps climbing to the step-off
    _box('CLIMB_%s_0@L' % nn, (0, 0.55, (H + 1.2) / 2), (1.1, 1.0, H + 1.2), main, coll, main)
    # a thin collider along the stalk (the pipeline wants one proxy; it stays clear of the climb line at 0.5 m)
    _box('COL_%s_0@L' % nn, (0, 0, H / 2), (0.12, 0.12, H), main, coll, main)
    main['kind'] = 'Ladder'
    main['params'] = str({k: round(v, 3) for k, v in p.items()})
    return main


def build_all(count=6, save=True):
    build_group()
    mains = [bake_variant(i + 1, offset_x=i * 4.0) for i in range(count)]
    old = bpy.data.objects.get('GN_Ladder_Source')
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    src = _modifier_obj('GN_Ladder_Source', variant_params(1))
    src.location = (-5, 0, 0)
    if save:
        bpy.ops.wm.save_as_mainfile(filepath='/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/plants.blend')
    return [m.name for m in mains]
