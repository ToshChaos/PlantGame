"""T06: Geometry Nodes vine bridge (GN_Plant_Bridge) + variant baking.

In Blender (via MCP execute_blender_code):
    S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
    exec(open(S + 'plants_bridge.py').read())
    build_all()            # node group, preview material, 6 baked variants Plant_Bridge_01..06, saves plants.blend

The bridge grows along Blender +Y (straight in Y, arc in Z, lateral wobble in X), starts at the origin (Anchor).
growth_t (vertex colour attribute): R = growth 0..1 (= y / Length, monotonic), G = part id (0 deck, .34 vines,
.67 leaves, 1 blossoms), B = random tint. Palette lookup by G happens in the shader (M_PlantGrow); the Blender preview
material M_Bridge_Preview mirrors it.
"""
import math
import random
import bpy
import bmesh
import mathutils

SPROUT_LEN = 2.7        # user request: the sprout is about 3x the first version (0.9 m)
GROUP = 'GN_Plant_Bridge'
MAT = 'M_Bridge_Preview'
SEGMENTS = 24


# ---------------------------------------------------------------- node helpers
class _B:
    def __init__(self, tree):
        self.t = tree
        self.n = tree.nodes
        self.x = 0

    def node(self, idname, **props):
        nd = self.n.new(idname)
        for k, v in props.items():
            if hasattr(nd, k):
                setattr(nd, k, v)
            else:  # Blender 5: mode enums became menu input sockets
                nd.inputs[k.capitalize()].default_value = (v if '-' in v else v.replace('_', ' ').title()) if isinstance(v, str) else v
        nd.location = (self.x, 0)
        self.x += 190
        return nd

    def out(self, v, name=None):
        """v: socket, node (first enabled output / `name`) or constant."""
        if isinstance(v, bpy.types.NodeSocket):
            return v
        if isinstance(v, bpy.types.Node):
            if name:
                return v.outputs[name]
            for s in v.outputs:
                if s.enabled:
                    return s
        return None

    def en(self, nd):
        return next(x for x in nd.outputs if x.enabled)

    def set(self, nd, inp, v):
        sock = inp if not isinstance(inp, str) else next(
            (x for x in nd.inputs if x.name == inp and x.enabled), nd.inputs[inp])
        if isinstance(inp, int):
            sock = [x for x in nd.inputs if x.enabled][inp] if nd.bl_idname != 'ShaderNodeMath' else nd.inputs[inp]
        o = self.out(v)
        if o is not None:
            self.t.links.new(o, sock)
        else:
            sock.default_value = v

    def make(self, idname, inputs=None, **props):
        nd = self.node(idname, **props)
        for k, v in (inputs or {}).items():
            self.set(nd, k, v)
        return nd

    # math
    def m(self, op, a, b=None, c=None, clamp=False):
        nd = self.node('ShaderNodeMath', operation=op)
        nd.use_clamp = clamp
        for i, v in enumerate((a, b, c)):
            if v is not None:
                self.set(nd, i, v)
        return nd.outputs[0]

    def vm(self, op, a, b=None, c=None):
        nd = self.node('ShaderNodeVectorMath', operation=op)
        for i, v in enumerate((a, b, c)):
            if v is not None:
                self.set(nd, i, v)
        return nd.outputs['Vector'] if op not in ('DOT_PRODUCT', 'LENGTH', 'DISTANCE') else nd.outputs['Value']

    def xyz(self, x, y, z):
        nd = self.node('ShaderNodeCombineXYZ')
        for i, v in enumerate((x, y, z)):
            self.set(nd, i, v)
        return nd.outputs[0]


def _interface(tree):
    it = tree.interface
    it.new_socket('Geometry', in_out='OUTPUT', socket_type='NodeSocketGeometry')
    it.new_socket('Geometry', in_out='INPUT', socket_type='NodeSocketGeometry')  # unused host geometry
    spec = [
        ('Seed', 'NodeSocketInt', 1, 0, 9999),
        ('Length', 'NodeSocketFloat', 11.0, 8.0, 14.0),
        ('Arc', 'NodeSocketFloat', 1.2, 0.0, 2.5),
        ('Width', 'NodeSocketFloat', 1.5, 1.2, 2.5),
        ('Thickness', 'NodeSocketFloat', 0.14, 0.06, 0.3),
        ('Curl', 'NodeSocketFloat', 0.8, 0.0, 2.0),
        ('LeafDensity', 'NodeSocketFloat', 1.0, 0.0, 2.0),
        ('Droop', 'NodeSocketFloat', 0.3, 0.0, 1.0),
    ]
    for name, st, d, lo, hi in spec:
        s = it.new_socket(name, in_out='INPUT', socket_type=st)
        s.default_value = d
        s.min_value = lo
        s.max_value = hi


def make_material():
    m = bpy.data.materials.get(MAT) or bpy.data.materials.new(MAT)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.7
    attr = nt.nodes.new('ShaderNodeVertexColor')
    attr.layer_name = 'growth_t'
    sep = nt.nodes.new('ShaderNodeSeparateColor')
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.interpolation = 'CONSTANT'
    cols = [(0.0, '#2F7D4F'), (0.17, '#5DB46B'), (0.5, '#9BE0A8'), (0.72, '#F2C84B'), (0.88, '#D6453D'), (0.97, '#F4F8F6')]   # 0.72-0.88 yellow flowers, 0.88-0.97 red mushroom caps (platform)
    el = ramp.color_ramp.elements
    el[0].position, el[1].position = cols[0][0], cols[-1][0]
    for p, _ in cols[1:-1]:
        el.new(p)
    for e, (p, h) in zip(sorted(el, key=lambda e: e.position), cols):
        e.position = p
        r, g, b = (int(h[i:i + 2], 16) / 255 for i in (1, 3, 5))
        e.color = tuple(x ** 2.2 for x in (r, g, b)) + (1.0,)
    mul = nt.nodes.new('ShaderNodeMix')
    mul.data_type = 'RGBA'
    mul.blend_type = 'MULTIPLY'
    mul.inputs[0].default_value = 0.35
    tint = nt.nodes.new('ShaderNodeMath')
    tint.operation = 'ADD'
    tint.inputs[1].default_value = 0.75
    L = nt.links.new
    L(attr.outputs['Color'], sep.inputs['Color'])
    L(sep.outputs['Green'], ramp.inputs['Fac'])
    L(sep.outputs['Blue'], tint.inputs[0])
    L(ramp.outputs['Color'], mul.inputs[6])
    L(tint.outputs[0], mul.inputs[7])  # float -> colour (grey tint)
    L(mul.outputs[2], bsdf.inputs['Base Color'])
    L(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return m


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
    seed, length, arc, width, thick, curl, leafd, droop = (I[n] for n in (
        'Seed', 'Length', 'Arc', 'Width', 'Thickness', 'Curl', 'LeafDensity', 'Droop'))

    # ---- path: straight in Y, arc (Z) + lateral wobble (X) fading to 0 at both ends
    line = b.make('GeometryNodeCurvePrimitiveLine', {'End': b.xyz(0, length, 0)})
    res = b.make('GeometryNodeResampleCurve', {'Curve': line, 'Count': SEGMENTS}, mode='COUNT')
    t = b.make('GeometryNodeSplineParameter').outputs['Factor']
    s = b.m('SINE', b.m('MULTIPLY', t, math.pi))
    pos = b.make('GeometryNodeInputPosition').outputs[0]
    sw = b.make('ShaderNodeTexNoise', {'Vector': pos, 'W': b.m('MULTIPLY', seed, 3.7), 'Scale': 0.22},
                noise_dimensions='4D')
    sw2 = b.make('ShaderNodeTexNoise', {'Vector': pos, 'W': b.m('MULTIPLY', seed, 5.1), 'Scale': 0.5},
                 noise_dimensions='4D')
    lat = b.m('MULTIPLY', b.m('MULTIPLY', b.m('SUBTRACT', sw.outputs['Fac'], 0.5), 2.0), b.m('MULTIPLY', curl, s))
    lift = b.m('ADD', b.m('SUBTRACT', b.m('MULTIPLY', arc, s), b.m('MULTIPLY', droop, b.m('MULTIPLY', t, t))),
               b.m('MULTIPLY', b.m('SUBTRACT', sw2.outputs['Fac'], 0.5), b.m('MULTIPLY', 0.3, s)))
    moved = b.make('GeometryNodeSetPosition', {'Geometry': res, 'Offset': b.xyz(lat, 0, lift)})
    base = b.make('GeometryNodeSetCurveNormal', {'Curve': moved}, mode='Z_UP')

    tan = b.make('GeometryNodeInputTangent').outputs[0]
    nor = b.make('GeometryNodeInputNormal').outputs[0] if False else b.xyz(0, 0, 1)
    side = b.vm('NORMALIZE', b.vm('CROSS_PRODUCT', tan, nor))      # sideways (right-hand)
    up = b.vm('NORMALIZE', b.vm('CROSS_PRODUCT', side, tan))       # up, perpendicular to the path

    def store_part(geo, value, domain='POINT'):
        return b.make('GeometryNodeStoreNamedAttribute', {'Geometry': geo, 'Name': 'part', 'Value': value},
                      data_type='FLOAT', domain=domain)

    # ---- deck: a braid of fat vine strands running along the path (no solid slab; the collision proxies are added at bake time)
    strand_circ = b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 6, 'Radius': 0.15})
    wrap_circ = b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 4, 'Radius': 0.09})
    deck_parts = []
    ph_d = b.m('MULTIPLY', t, b.m('MULTIPLY', length, 1.5))   # slow braid
    n_str = 4
    for k in range(n_str):
        lat0 = (k / (n_str - 1) - 0.5) * 0.78                  # fraction of the width
        ang = b.m('ADD', ph_d, k * 1.9)
        offs = b.vm('ADD', b.vm('SCALE', side, b.m('ADD', b.m('MULTIPLY', width, lat0), b.m('MULTIPLY', b.m('SINE', ang), 0.1))),
                    b.vm('SCALE', up, b.m('ADD', -0.13, b.m('MULTIPLY', b.m('COSINE', ang), 0.045))))
        sp = b.make('GeometryNodeSetPosition', {'Geometry': base, 'Offset': offs})
        sm = b.make('GeometryNodeCurveToMesh', {'Curve': sp, 'Profile Curve': strand_circ, 'Fill Caps': True})
        deck_parts.append(store_part(sm, 0.0).outputs[0])
    # two thin vines that zig-zag across the whole width, weaving over and under the strands
    for k in (0.0, math.pi):
        ang = b.m('ADD', b.m('MULTIPLY', t, b.m('MULTIPLY', length, 2.2)), k)
        offs = b.vm('ADD', b.vm('SCALE', side, b.m('MULTIPLY', b.m('MULTIPLY', width, 0.5), b.m('SINE', ang))),
                    b.vm('SCALE', up, b.m('ADD', -0.06, b.m('MULTIPLY', b.m('COSINE', b.m('MULTIPLY', ang, 2.0)), 0.07))))
        wp = b.make('GeometryNodeSetPosition', {'Geometry': base, 'Offset': offs})
        wm = b.make('GeometryNodeCurveToMesh', {'Curve': wp, 'Profile Curve': wrap_circ, 'Fill Caps': True})
        deck_parts.append(store_part(wm, 0.34).outputs[0])
    deck = b.make('GeometryNodeJoinGeometry')
    for dpart in deck_parts:
        tree.links.new(dpart, deck.inputs[0])

    # ---- woven slats across the deck (vine bundles), every ~0.4 m, sitting just above the path
    rib_pts = b.make('GeometryNodeCurveToPoints', {'Curve': base, 'Count': b.m('MAXIMUM', b.m('ROUND', b.m('DIVIDE', length, 0.9)), 4.0)}, mode='COUNT')
    rib_geo = b.make('GeometryNodeMeshCube', {'Size': b.xyz(b.m('MULTIPLY', width, 0.98), 0.07, 0.05)})
    rib_rot = b.make('FunctionNodeRandomValue', {'Min': (0, 0, -0.18), 'Max': (0, 0, 0.18), 'Seed': b.m('ADD', seed, 41)}, data_type='FLOAT_VECTOR')
    ribs = b.make('GeometryNodeInstanceOnPoints', {'Points': rib_pts.outputs['Points'], 'Instance': rib_geo, 'Rotation': b.en(rib_rot)})
    ribs = store_part(ribs, 0.34, 'INSTANCE')

    # ---- rails and braided strands, both sides
    circ = b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 4, 'Radius': 0.045})
    circ_thin = b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 4, 'Radius': 0.075})
    parts = [deck.outputs[0], ribs.outputs[0]]
    rails = []
    ph = b.m('MULTIPLY', t, b.m('MULTIPLY', length, 2.6))  # braid phase (tighter twist: reads as woven vines)
    for sgn in (-1.0, 1.0):
        edge = b.vm('SCALE', side, b.m('MULTIPLY', width, 0.5 * sgn))
        # handrail
        off = b.vm('ADD', edge, b.vm('SCALE', up, 0.85))
        hr = b.make('GeometryNodeSetPosition', {'Geometry': base, 'Offset': off})
        rails.append(hr)
        m = b.make('GeometryNodeCurveToMesh', {'Curve': hr, 'Profile Curve': circ})
        parts.append(store_part(m, 0.34).outputs[0])
        # two twisting strands at the deck edge
        for k in (0.0, math.pi):
            ang = b.m('ADD', ph, k)
            wob = b.vm('ADD', b.vm('SCALE', side, b.m('MULTIPLY', b.m('COSINE', ang), 0.15)),
                       b.vm('SCALE', up, b.m('MULTIPLY', b.m('SINE', ang), 0.15)))
            st = b.make('GeometryNodeSetPosition', {'Geometry': base, 'Offset': b.vm('ADD', edge, wob)})
            m = b.make('GeometryNodeCurveToMesh', {'Curve': st, 'Profile Curve': circ_thin})
            parts.append(store_part(m, 0.34).outputs[0])

    # ---- leaves and blossoms scattered on the handrails
    leaf_n = b.m('MAXIMUM', b.m('ROUND', b.m('MULTIPLY', length, b.m('MULTIPLY', leafd, 1.3))), 0.0)
    flw_n = b.m('MAXIMUM', b.m('ROUND', b.m('MULTIPLY', length, b.m('MULTIPLY', leafd, 0.3))), 2.0)
    # T67: pointed blade with drooping tip and cupped sides (was a 4-gon diamond)
    lf = b.make('GeometryNodeFillCurve', {'Curve': b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 10, 'Radius': 0.3})}, mode='N-gons')
    lf = b.make('GeometryNodeSubdivideMesh', {'Mesh': lf, 'Level': 1})
    lf = b.make('GeometryNodeTransform', {'Geometry': lf, 'Scale': b.xyz(0.8, 1.7, 1.0), 'Translation': b.xyz(0, 0.5, 0)})
    _lp = b.make('GeometryNodeInputPosition')
    ly = b.make('ShaderNodeSeparateXYZ', {'Vector': _lp}).outputs['Y']
    lx = b.make('ShaderNodeSeparateXYZ', {'Vector': _lp}).outputs['X']
    leaf_geo = b.make('GeometryNodeSetPosition', {'Geometry': lf, 'Offset': b.xyz(0, 0, b.m('ADD', b.m('MULTIPLY', b.m('MULTIPLY', ly, ly), -0.3), b.m('MULTIPLY', b.m('MULTIPLY', lx, lx), 1.4)))})
    def flower(outer_part, inner_part, centre_part):
        # T40: two rings of flattened, cupped petals (7 outer, 5 inner turned half a step) round a glowing bead; `part` is stored per vertex
        # so the outer petals, inner petals and centre can differ (shader PlantGrow: .995 glow centre, .985 white petals, .975 mist-blue petals; 1.0 stays plain white)
        def ring(count, radius, tilt, depth, rb, part, phase):
            fi = b.make('GeometryNodeInputIndex').outputs[0]
            ang = b.m('ADD', b.m('MULTIPLY', fi, 2 * math.pi / count), phase)
            fpts = b.make('GeometryNodeMeshToPoints', {'Mesh': b.make('GeometryNodeMeshLine', {'Count': count})}, mode='VERTICES')
            fpos = b.make('GeometryNodeSetPosition', {'Geometry': fpts, 'Position': b.xyz(b.m('MULTIPLY', b.m('COSINE', ang), radius), b.m('MULTIPLY', b.m('SINE', ang), radius), 0)})
            petal = b.make('GeometryNodeMeshCone', {'Vertices': 3, 'Radius Top': 0.0, 'Radius Bottom': rb, 'Depth': depth})
            pet = b.make('GeometryNodeInstanceOnPoints', {'Points': fpos, 'Instance': petal, 'Rotation': b.xyz(0, math.pi / 2 - tilt, ang), 'Scale': b.xyz(1.0, 0.45, 1.0)})
            pet = b.make('GeometryNodeRealizeInstances', {'Geometry': pet})
            return b.make('GeometryNodeStoreNamedAttribute', {'Geometry': pet, 'Name': 'part', 'Value': part}, data_type='FLOAT', domain='POINT')
        outer = ring(7, 0.045, 0.55, 0.2, 0.05, outer_part, 0.0)
        inner = ring(5, 0.03, 0.95, 0.13, 0.04, inner_part, math.pi / 5)
        cen = b.make('GeometryNodeMeshCone', {'Vertices': 4, 'Radius Top': 0.0, 'Radius Bottom': 0.04, 'Depth': 0.07})
        cen = b.make('GeometryNodeTransform', {'Geometry': cen, 'Translation': (0, 0, 0.03)})
        cen = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': cen, 'Name': 'part', 'Value': centre_part}, data_type='FLOAT', domain='POINT')
        fj = b.make('GeometryNodeJoinGeometry')
        tree.links.new(outer.outputs[0], fj.inputs[0])
        tree.links.new(inner.outputs[0], fj.inputs[0])
        tree.links.new(cen.outputs[0], fj.inputs[0])
        return fj
    flower_white = flower(0.985, 0.975, 0.995)    # white petals, mist-blue inner petals, glowing centre
    flower_yellow = flower(0.8, 0.82, 0.995)   # yellow petals, glowing centre
    inst = []
    for i, hr in enumerate(rails):
        for kind, cnt, geo, pid, smin, smax, sd in (('leaf', leaf_n, leaf_geo, 0.67, 0.6, 1.4, 11),
                                                  ('flw_w', flw_n, flower_white, None, 1.8, 2.6, 23),
                                                  ('flw_y', flw_n, flower_yellow, None, 1.8, 2.6, 37)):
            pts = b.make('GeometryNodeCurveToPoints', {'Curve': hr, 'Count': cnt}, mode='COUNT')
            tilt = 0.7 if pid is not None else 0.25     # leaves tumble, flowers stay mostly upright-facing
            rv = b.make('FunctionNodeRandomValue', {'Min': (-tilt, -tilt, 0.0), 'Max': (tilt, tilt, 6.28),
                                                    'Seed': b.m('ADD', seed, sd + i)}, data_type='FLOAT_VECTOR')
            rs = b.make('FunctionNodeRandomValue', {'Min': smin, 'Max': smax, 'Seed': b.m('ADD', seed, sd + 5 + i)},
                        data_type='FLOAT')
            scl, sel = b.en(rs), True
            if pid is not None:
                # user request: fewer, smaller leaves mid-span (walking area); full size toward the ends
                ypos = b.make('ShaderNodeSeparateXYZ', {'Vector': b.make('GeometryNodeInputPosition')}).outputs['Y']
                edge = b.m('MINIMUM', b.m('MULTIPLY', b.m('ABSOLUTE', b.m('SUBTRACT', b.m('MULTIPLY', b.m('DIVIDE', ypos, length), 2.0), 1.0)), 1.6), 1.0)
                scl = b.m('MULTIPLY', scl, b.m('ADD', 0.35, b.m('MULTIPLY', edge, 0.55)))
                keep = b.en(b.make('FunctionNodeRandomValue', {'Min': 0.0, 'Max': 1.0, 'Seed': b.m('ADD', seed, sd + 91 + i)}, data_type='FLOAT'))
                sel = b.m('LESS_THAN', b.m('MULTIPLY', keep, b.m('SUBTRACT', 1.5, edge)), 0.38)   # mid-span keeps only a few leaves
            ip = b.make('GeometryNodeInstanceOnPoints', {'Points': pts.outputs['Points'], 'Instance': geo,
                                                         'Rotation': b.en(rv), 'Scale': b.xyz(scl, scl, scl)})
            if pid is not None:
                tree.links.new(sel, ip.inputs['Selection'])
                ip = store_part(ip, pid, 'INSTANCE')
            parts.append(ip.outputs[0])

    # ---- user request: the sprout (growth 0.08) reads as two small vines heading in the bridge direction. They sit in the first 0.9 m
    # (growth_t R <= 0.075) and are flagged `spr`; the rest of the bridge starts revealing only after the sprout (R remapped below).
    for side in (-1.0, 1.0):
        vl = b.make('GeometryNodeCurvePrimitiveLine', {'End': b.xyz(0, SPROUT_LEN, 0)})
        vr = b.make('GeometryNodeResampleCurve', {'Curve': vl, 'Count': 24}, mode='COUNT')
        vt = b.make('GeometryNodeSplineParameter').outputs['Factor']
        vx = b.m('MULTIPLY', b.m('ADD', 0.4, b.m('MULTIPLY', 0.22, b.m('SINE', b.m('MULTIPLY', vt, 5.0)))), side)
        vz = b.m('ADD', 0.12, b.m('MULTIPLY', 0.8, b.m('POWER', vt, 1.6)))
        vm = b.make('GeometryNodeSetPosition', {'Geometry': vr, 'Offset': b.xyz(vx, 0, vz)})
        vprof = b.make('GeometryNodeCurvePrimitiveCircle', {'Resolution': 6, 'Radius': 1.0})
        vmesh = b.make('GeometryNodeCurveToMesh', {'Curve': vm, 'Profile Curve': vprof, 'Scale': b.m('MULTIPLY', 0.075, b.m('SUBTRACT', 1.0, b.m('MULTIPLY', vt, 0.55))), 'Fill Caps': True})
        sv = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': store_part(vmesh, 0.34), 'Name': 'spr', 'Value': 1.0}, data_type='FLOAT', domain='POINT')
        parts.append(sv.outputs[0])
        lpts = b.make('GeometryNodeCurveToPoints', {'Curve': vm, 'Count': 5}, mode='COUNT')
        lrot = b.make('FunctionNodeRandomValue', {'Min': (-0.3, -0.3, 0.0), 'Max': (0.3, 0.3, 6.28), 'Seed': b.m('ADD', seed, 400 + int(side))}, data_type='FLOAT_VECTOR')
        lins = b.make('GeometryNodeInstanceOnPoints', {'Points': lpts.outputs['Points'], 'Instance': leaf_geo, 'Rotation': b.en(lrot), 'Scale': b.xyz(0.75, 0.75, 0.75)})
        lins = store_part(lins, 0.67, 'INSTANCE')
        lsv = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': lins, 'Name': 'spr', 'Value': 1.0}, data_type='FLOAT', domain='INSTANCE')
        parts.append(lsv.outputs[0])

    join = b.make('GeometryNodeJoinGeometry')
    for p in parts:
        tree.links.new(p, join.inputs[0])
    real = b.make('GeometryNodeRealizeInstances', {'Geometry': join})
    # ---- growth_t vertex colour: R = y / Length, G = part, B = tint
    py = b.make('ShaderNodeSeparateXYZ', {'Vector': b.make('GeometryNodeInputPosition')}).outputs['Y']
    tt0 = b.m('DIVIDE', py, length, clamp=True)
    tts = b.m('MULTIPLY', b.m('DIVIDE', py, SPROUT_LEN, clamp=True), 0.075)   # sprout vines: R spans 0..0.075 over their own length
    spr = b.make('GeometryNodeInputNamedAttribute', data_type='FLOAT', inputs={'Name': 'spr'}).outputs['Attribute']
    tt = b.m('ADD', b.m('MULTIPLY', b.m('ADD', 0.085, b.m('MULTIPLY', tt0, 0.915)), b.m('SUBTRACT', 1.0, spr)), b.m('MULTIPLY', tts, spr))   # sprout vines have their own R range, the bridge starts after them
    part = b.make('GeometryNodeInputNamedAttribute', data_type='FLOAT', inputs={'Name': 'part'}).outputs['Attribute']
    tint = b.make('FunctionNodeRandomValue', {'Min': 0.0, 'Max': 1.0, 'Seed': seed}, data_type='FLOAT')
    tint = b.en(tint)
    col = b.make('FunctionNodeCombineColor', {'Red': tt, 'Green': part, 'Blue': tint}, mode='RGB')
    st = b.make('GeometryNodeStoreNamedAttribute', {'Geometry': real, 'Name': 'growth_t', 'Value': col},
                data_type='FLOAT_COLOR', domain='POINT')
    mat = b.make('GeometryNodeSetMaterial', {'Geometry': st, 'Material': make_material()})
    tree.links.new(mat.outputs[0], go.inputs[0])
    return tree


# ---------------------------------------------------------------- variants
def variant_params(n):
    r = random.Random(1000 + n)
    length = r.uniform(8.5, 13.5)
    arc = length * r.uniform(0.12, 0.16)      # clearly arched; peak slope <= ~27 degrees
    return dict(Seed=n * 17 + 3, Length=length, Arc=arc, Width=r.uniform(1.3, 1.9),
                Thickness=r.uniform(0.10, 0.2), Curl=r.uniform(0.3, 1.5), LeafDensity=r.uniform(0.5, 1.1),
                Droop=r.uniform(0.0, 0.25))


def _modifier_obj(name, params):
    me = bpy.data.meshes.new(name + '_src')
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    for k, v in params.items():
        bpy.data.node_groups[GROUP].interface.items_tree[k].default_value = v
    md = ob.modifiers.new('GN_Plant_Bridge', 'NODES')
    md.node_group = bpy.data.node_groups[GROUP]
    # Blender 5.2 does not allow writing modifier inputs by key from Python: use the group's interface defaults
    # (a fresh modifier starts from them), so params must be set before the modifier is created.
    return ob


def _clear(name):
    coll = bpy.data.collections.get(name)
    if coll:
        for o in list(coll.objects):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(coll)


def bake_variant(n, offset_x=0.0):
    name = 'Plant_Bridge_%02d' % n
    nn = '%02d' % n
    _clear(name)
    p = variant_params(n)
    tmp = _modifier_obj('tmp_' + name, p)
    dg = bpy.context.evaluated_depsgraph_get()
    ev = tmp.evaluated_get(dg)
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
    closed = [f for f in bm.faces if all(len(e.link_faces) == 2 for e in f.edges)]  # skip single-sided leaf cards
    bmesh.ops.recalc_face_normals(bm, faces=closed)
    bm.to_mesh(me)
    bm.free()
    me.materials.clear()
    me.materials.append(bpy.data.materials[MAT])  # contract: one material slot
    for poly in me.polygons:
        poly.use_smooth = False
    # deck centreline from vertices with part id 0 (G channel)
    ca = me.color_attributes['growth_t']
    deck = [(v.co.copy(), ca.data[i].color[0]) for i, v in enumerate(me.vertices) if ca.data[i].color[1] < 0.1]
    nseg = max(6, int(p['Length'] / 1.4))
    bins = [[] for _ in range(nseg + 1)]
    for co, tt in deck:
        bins[min(nseg, int(round(tt * nseg)))].append(co)
    centres = []
    for bn in bins:
        if bn:
            c = sum(bn, mathutils.Vector()) / len(bn)
            centres.append(c)
    mk = lambda nm, loc: _empty(nm, loc, main, coll)
    mk('Anchor_%s@B' % nn, mathutils.Vector((0, 0, 0)))
    mk('Tip_%s@B' % nn, centres[-1])
    mk('Land_%s@B' % nn, centres[-1] + mathutils.Vector((0, 0.8, 0.0)))
    for i in range(len(centres) - 1):
        a, c = centres[i], centres[i + 1]
        d = c - a
        mid = (a + c) / 2
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co.x *= p['Width']
            v.co.y *= d.length * 1.08
            v.co.z *= 0.3
            v.co.z -= 0.08
        cm = bpy.data.meshes.new('COL_%s_%d@B' % (nn, i))
        bm.to_mesh(cm)
        bm.free()
        co = bpy.data.objects.new('COL_%s_%d@B' % (nn, i), cm)
        coll.objects.link(co)
        co.parent = main
        co.matrix_parent_inverse = main.matrix_world.inverted()
        co.matrix_world = main.matrix_world @ mathutils.Matrix.Translation(mid) @ d.to_track_quat('Y', 'Z').to_matrix().to_4x4()
        co.display_type = 'WIRE'
        co.hide_render = True  # wire display still renders solid
    main['kind'] = 'Bridge'
    main['params'] = str({k: round(v, 3) for k, v in p.items()})
    return main


def _empty(name, loc, parent, coll):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = 'ARROWS'
    e.empty_display_size = 0.4
    e.location = loc
    coll.objects.link(e)
    e.parent = parent
    return e


def build_all(count=6, save=True):
    exec_ns = globals()
    build_group()
    for o in list(bpy.data.objects):
        if o.name in ('Cube',):
            bpy.data.objects.remove(o, do_unlink=True)
    mains = [bake_variant(i + 1, offset_x=i * 5.0) for i in range(count)]
    # editable procedural source, kept for the demo (hidden from export collections)
    old = bpy.data.objects.get('GN_Bridge_Source')
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    src = _modifier_obj('GN_Bridge_Source', variant_params(1))
    src.location = (-6, 0, 0)
    if save:
        bpy.ops.wm.save_as_mainfile(filepath='/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/plants.blend')
    return [m.name for m in mains]
