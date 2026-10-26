"""T28: procedural Water Collector plant (pitcher bloom) + 6 baked variants Plant_Collector_01..06.

In Blender (via MCP execute_blender_code):
    S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
    exec(open(S + 'pipeline_common.py').read())
    exec(open(S + 'plants_collector.py').read())
    build_all()            # 6 variants, validation-ready, saves plants.blend; then export_collections([...Plant_Collector_NN...])

Deviation from the task text: built with bmesh from per-seed parameters instead of a Geometry Nodes group (faster to iterate; the
variants are still fully procedural and deterministic per seed). Each variant: a curved stem, a double-walled bowl, 5-8 curled
petals around the rim, 4-6 broad base leaves and cyan dew dots on the rim.
growth_t (point colour): R = z / height (base -> rim/petal tips), G = part id for the shader palette (.67 stem and leaves, .92 bowl,
.8 dew dots, 1.0 petals), B = random tint. Reservoir is a separate mesh (flat cylinder from WaterMin up to WaterMax, pivot at its base)
that Unity scales 0..1 for the fill level. INTERACT_NN is a trigger box around the bowl. Pivot at Anchor (the soil).
"""
import math
import random
import bpy
import bmesh
import mathutils

_S = '/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/scripts/'
_ns = {'__name__': 'plants_bridge_helpers'}
exec(open(_S + 'plants_bridge.py').read(), _ns)
make_material = _ns['make_material']
_empty = _ns['_empty']
MAT = _ns['MAT']

# Height, BowlRadius, PetalCount, Curl, Leaves, StemRadius
VARIANTS = [
    (2.0, 0.80, 6, 0.50, 5, 0.13),
    (2.4, 0.70, 7, 0.65, 4, 0.12),
    (1.8, 0.90, 5, 0.40, 6, 0.15),
    (2.6, 0.75, 8, 0.55, 5, 0.12),
    (2.1, 0.85, 6, 0.70, 4, 0.14),
    (2.3, 0.78, 7, 0.45, 6, 0.13),
]
SEG = 24


def variant_params(n):
    h, br, pc, curl, leaves, sr = VARIANTS[(n - 1) % len(VARIANTS)]
    return dict(Seed=n * 13 + 5, Height=h, BowlRadius=br, PetalCount=pc, Curl=curl, Leaves=leaves, StemRadius=sr)


class _Mesh:
    def __init__(self):
        self.v, self.f, self.part = [], [], []

    def add(self, co, part):
        self.v.append(tuple(co)); self.part.append(part)
        return len(self.v) - 1

    def ring(self, centre, r, z, part, seg=SEG, ox=0.0, oy=0.0, rib=0.0):
        # rib: T67 vertical ridges (8 round the ring), fraction of the radius
        return [self.add((centre[0] + ox + math.cos(a * math.tau / seg) * r * (1 + rib * math.cos(a * 8 * math.tau / seg)),
                          centre[1] + oy + math.sin(a * math.tau / seg) * r * (1 + rib * math.cos(a * 8 * math.tau / seg)), z), part) for a in range(seg)]

    def bridge(self, a, b, seg=SEG, flip=False):
        for i in range(seg):
            j = (i + 1) % seg
            q = (a[i], a[j], b[j], b[i])
            self.f.append(q[::-1] if flip else q)

    def cap(self, ring, flip=False):
        self.f.append(tuple(ring[::-1] if flip else ring))


def _build(p):
    rnd = random.Random(p['Seed'])
    H, R, PC, CURL, NL, SR = p['Height'], p['BowlRadius'], p['PetalCount'], p['Curl'], p['Leaves'], p['StemRadius']
    m = _Mesh()
    bowl_h = 0.62 * R + 0.4
    zb = H * 0.5                                  # bowl base height (top of the stem)
    lean = (rnd.uniform(-0.2, 0.2), rnd.uniform(-0.2, 0.2))
    # ---- stem: curved tube from the soil to the bowl base
    rings = []
    for i in range(13):
        t = i / 12
        r = SR * (1.5 - 0.6 * t) * (1.0 + 0.16 * math.sin(t * 30.0 + p['Seed']) ** 2 + 0.5 * max(0.0, 1.0 - t * 8.0) ** 2)   # T67: nodes + flared root
        rings.append(m.ring((0, 0), r, zb * t, 0.67, ox=lean[0] * t * t, oy=lean[1] * t * t, rib=0.05))
    for a, b in zip(rings, rings[1:]):
        m.bridge(a, b)
    m.cap(rings[0], flip=True)
    cx, cy = lean[0], lean[1]
    # ---- bowl: outer wall up to the rim, a rolled lip, then the inner wall down to the floor
    outer = [(0.14, 0.00), (0.34, 0.04), (0.56, 0.11), (0.76, 0.20), (0.92, 0.33), (1.00, 0.46), (0.97, 0.60), (0.88, 0.74), (0.84, 0.85), (0.92, 0.93), (1.12, 0.98), (1.24, 1.01), (1.27, 1.03)]   # T67: pitcher: belly, waist, flared lip
    inner = [(1.22, 1.06), (1.10, 1.02), (0.90, 0.92), (0.80, 0.80), (0.82, 0.62), (0.70, 0.38), (0.48, 0.20), (0.24, 0.14), (0.0, 0.12)]
    prof = [(R * rr, zb + bowl_h * hh) for rr, hh in outer] + [(R * rr, zb + bowl_h * hh) for rr, hh in inner]
    bowl_rings = []
    for k, (r, z) in enumerate(prof):
        bowl_rings.append(m.ring((cx, cy), max(r, 0.001), z, 0.92, rib=(0.12 if 10 <= k <= 12 else 0.045) if 2 <= k < len(outer) else 0.0))   # ribbed peristome (the lip) 
    for a, b in zip(bowl_rings, bowl_rings[1:]):
        m.bridge(a, b)
    rim_z = zb + bowl_h
    # ---- petals: curled tapered strips around the rim, white
    top = rim_z
    PC = max(3, PC - 3)
    petal_scale = 0.75
    for k in range(PC):
        ang = k / PC * math.tau + rnd.uniform(-0.1, 0.1)
        L = R * rnd.uniform(0.7, 1.0) * petal_scale; W = R * 0.62 * (6.0 / PC) ** 0.5
        steps = 6
        left, mid, right = [], [], []
        for s_ in range(steps + 1):
            t = s_ / steps
            out = R * 0.95 + L * t * (1.0 - 0.35 * CURL)
            z = rim_z + L * 0.9 * math.sin(t * math.pi * 0.5) * (1.0 - CURL * t)
            w = W * (1.0 - t) ** 0.7 * (0.4 + 0.6 * math.sin(min(1.0, t * 1.6 + 0.2) * math.pi)) + 0.012
            c, sn = math.cos(ang), math.sin(ang)
            px, py = cx + c * out, cy + sn * out
            cup = 0.5 * w * (1.0 - t * 0.5)          # T67: petals cup around a raised midrib
            left.append(m.add((px - sn * w, py + c * w, z + cup * 0.35), 1.0))
            mid.append(m.add((px, py, z - cup * 0.25), 1.0))
            right.append(m.add((px + sn * w, py - c * w, z + cup * 0.35), 1.0))
            top = max(top, z + cup * 0.35)
        for s_ in range(steps):
            for a_, b_ in ((left, mid), (mid, right)):
                m.f.append((a_[s_], b_[s_], b_[s_ + 1], a_[s_ + 1]))
                m.f.append((a_[s_ + 1], b_[s_ + 1], b_[s_], a_[s_]))   # two-sided so the shader culling never hides it
    # ---- the lid (operculum): a hood arching over the back of the opening, two-sided, with a midrib ridge
    ang_b = rnd.uniform(0, math.tau)
    LR, LC = 8, 10
    grid = []
    for ri in range(LR + 1):
        v = ri / LR
        half = 0.95 * (1.0 - 0.4 * v ** 1.5)
        row = []
        for ci in range(LC + 1):
            u = ci / LC * 2.0 - 1.0
            a = ang_b + u * half
            rad = R * (1.22 - 0.75 * v * (1.0 - 0.3 * abs(u)))
            z = rim_z + 0.03 + 0.5 * R * math.sin(v * math.pi * 0.55) * (1.0 - 0.4 * u * u) + 0.03 * R * (1 - abs(u)) * v
            row.append(m.add((cx + math.cos(a) * max(rad, 0.02), cy + math.sin(a) * max(rad, 0.02), z), 0.92))
            top = max(top, z)
        grid.append(row)
    for ri in range(LR):
        for ci in range(LC):
            q = (grid[ri][ci], grid[ri][ci + 1], grid[ri + 1][ci + 1], grid[ri + 1][ci])
            m.f.append(q)
            m.f.append(q[::-1])
    # ---- base leaves: broad curved blades lying outward from the stem foot
    for k in range(NL):
        ang = k / NL * math.tau + rnd.uniform(-0.25, 0.25)
        L = rnd.uniform(0.9, 1.4); W = rnd.uniform(0.28, 0.42)
        cols = 8
        left, mid, right = [], [], []
        for s_ in range(cols):
            t = s_ / (cols - 1)
            out = 0.12 + L * t
            z = 0.06 + 0.5 * math.sin(t * math.pi * 0.7) * (1.0 - 0.3 * t)
            w = W * math.sin(min(1.0, t * 1.1 + 0.08) * math.pi) + 0.01
            ripple = 0.025 * math.sin(t * 17.0 + k)                      # T67: wavy margin
            c, sn = math.cos(ang), math.sin(ang)
            left.append(m.add((c * out - sn * w, sn * out + c * w, z - 0.12 * w + ripple), 0.67))
            mid.append(m.add((c * out, sn * out, z + 0.05), 0.67))
            right.append(m.add((c * out + sn * w, sn * out - c * w, z - 0.12 * w - ripple), 0.67))
        for s_ in range(cols - 1):
            for a_, b_ in ((left, mid), (mid, right)):
                m.f.append((a_[s_], b_[s_], b_[s_ + 1], a_[s_ + 1]))
                m.f.append((a_[s_ + 1], b_[s_ + 1], b_[s_], a_[s_]))
    # ---- dew dots on the rim: little octahedra, cyan accent
    for k in range(8):
        ang = k / 8 * math.tau + 0.2
        d = 0.045
        px, py, pz = cx + math.cos(ang) * R * 0.99, cy + math.sin(ang) * R * 0.99, rim_z + 0.03
        v = [m.add((px + d, py, pz), 0.8), m.add((px - d, py, pz), 0.8), m.add((px, py + d, pz), 0.8),
             m.add((px, py - d, pz), 0.8), m.add((px, py, pz + d), 0.8), m.add((px, py, pz - d), 0.8)]
        for a, b, c_ in ((0, 2, 4), (2, 1, 4), (1, 3, 4), (3, 0, 4), (2, 0, 5), (1, 2, 5), (3, 1, 5), (0, 3, 5)):
            m.f.append((v[a], v[b], v[c_]))
    info = dict(rim_z=rim_z, top=top, zb=zb, bowl_h=bowl_h, cx=cx, cy=cy, R=R)
    return m, info


def _clear(name):
    coll = bpy.data.collections.get(name)
    if coll:
        for o in list(coll.objects):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(coll)


def _box(name, centre, size, main, coll):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= size[0]; v.co.y *= size[1]; v.co.z *= size[2]
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me)
    coll.objects.link(o)
    o.parent = main
    o.matrix_parent_inverse = main.matrix_world.inverted()
    o.matrix_world = main.matrix_world @ mathutils.Matrix.Translation(centre)
    o.display_type = 'WIRE'; o.hide_render = True
    return o


def bake_variant(n, offset_x=0.0):
    name = 'Plant_Collector_%02d' % n
    nn = '%02d' % n
    _clear(name)
    p = variant_params(n)
    m, info = _build(p)
    top = max(z for _, _, z in m.v)
    me = bpy.data.meshes.new(name)
    me.from_pydata(m.v, [], m.f)
    me.update()
    ca = me.color_attributes.new('growth_t', 'FLOAT_COLOR', 'POINT')
    rnd = random.Random(p['Seed'] + 1)
    tint = rnd.random()
    for i, (co, part) in enumerate(zip(m.v, m.part)):
        ca.data[i].color = (max(0.0, min(1.0, co[2] / top)), part, tint, 1.0)
    me.materials.append(make_material())
    nstem = 13 * SEG
    for poly in me.polygons:   # T67: smooth the stem and the bowl (two-sided leaves / petals share verts, so they stay flat)
        vs = poly.vertices
        poly.use_smooth = all(v < nstem for v in vs) or all(m.part[v] == 0.92 for v in vs)
    coll = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(coll)
    main = bpy.data.objects.new(name, me)
    coll.objects.link(main)
    main.location = (offset_x, 0, 0)
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bm.to_mesh(me); bm.free()
    mk = lambda nm, loc: _empty(nm, loc, main, coll)
    cx, cy, R = info['cx'], info['cy'], info['R']
    zmin = info['zb'] + info['bowl_h'] * 0.14
    zmax = info['zb'] + info['bowl_h'] * 0.80
    mk('Anchor_%s@C' % nn, mathutils.Vector((0, 0, 0)))
    mk('Tip_%s@C' % nn, mathutils.Vector((cx, cy, info['rim_z'])))
    mk('WaterMin_%s@C' % nn, mathutils.Vector((cx, cy, zmin)))
    mk('WaterMax_%s@C' % nn, mathutils.Vector((cx, cy, zmax)))
    # Reservoir: flat cylinder, pivot at its base (WaterMin); Unity scales its local Y (Blender Z) 0..1
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=R * 0.78, radius2=R * 0.84, depth=(zmax - zmin))
    for v in bm.verts:
        v.co.z += (zmax - zmin) / 2
    res_me = bpy.data.meshes.new('Reservoir_' + nn)
    bm.to_mesh(res_me); bm.free()
    rca = res_me.color_attributes.new('growth_t', 'FLOAT_COLOR', 'POINT')
    for i in range(len(res_me.vertices)):
        rca.data[i].color = (0.5, 0.8, 0.5, 1.0)
    res = bpy.data.objects.new('Reservoir_' + nn, res_me)
    coll.objects.link(res)
    res.parent = main
    res.location = (cx, cy, zmin)
    # trigger box around the bowl and petals + a small body collider
    _box('INTERACT_' + nn, (cx, cy, info['zb'] + info['bowl_h'] * 0.6), (R * 3.2, R * 3.2, info['bowl_h'] * 1.8), main, coll)
    _box('COL_%s_0@C' % nn, (cx * 0.5, cy * 0.5, info['zb'] * 0.5), (0.4, 0.4, info['zb']), main, coll)
    main['kind'] = 'Collector'
    main['params'] = str({k: round(v, 3) if isinstance(v, float) else v for k, v in p.items()})
    return main


def build_all(count=6, save=True):
    mains = [bake_variant(i + 1, offset_x=i * 6.0 - 14.0) for i in range(count)]
    reports = {}
    for mn in mains:
        reports[mn.name] = dict(tris=sum(len(p.vertices) - 2 for p in mn.data.polygons), height=round(max(v.co.z for v in mn.data.vertices), 2),
                                params=mn['params'])
    if save:
        bpy.ops.wm.save_as_mainfile(filepath='/Users/anton.podkletnov/Projects/PlantGame/Source/Blender/plants.blend')
    return reports
