"""T02 round-trip test object: an asymmetric 'L' shape with an Anchor empty and a growth_t attribute.

    exec(open(S + 't02_make_test_object.py').read()); make_test_asym()

Layout in Blender (Z up, metres): base block 1x1x1 centred on the origin footprint (pivot at the bottom centre),
an ARM sticking out along +X (length 1 m, 0.4 m wide), and a small nub on +Y near the top.
Expected in Unity (after import with the pinned settings): the base is 1 m wide, 1 m tall; the arm and nub must sit on the
same sides as in Blender once the -Z forward / Y up mapping is accounted for. See docs/pipeline.md "Round-trip test".
"""
import bpy
import bmesh


def make_test_asym(name='Test_Asym'):
    for o in list(bpy.data.objects):
        if o.name.startswith(name) or o.name == 'Anchor_Test':
            bpy.data.objects.remove(o, do_unlink=True)
    old = bpy.data.collections.get(name)
    if old:
        bpy.data.collections.remove(old)
    coll = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(coll)

    bm = bmesh.new()
    def box(cx, cy, cz, sx, sy, sz):
        r = bmesh.ops.create_cube(bm, size=1.0)
        bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=r['verts'])
        bmesh.ops.translate(bm, vec=(cx, cy, cz), verts=r['verts'])
    box(0, 0, 0.5, 1.0, 1.0, 1.0)        # base: x,y in [-0.5,0.5], z in [0,1]
    box(1.0, 0, 0.5, 1.0, 0.4, 0.4)      # arm along +X (x 0.5..1.5)
    box(0, 0.65, 0.9, 0.3, 0.3, 0.3)     # nub on +Y near the top
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()

    attr = me.color_attributes.new('growth_t', 'FLOAT_COLOR', 'POINT')
    for i, v in enumerate(me.vertices):
        t = max(0.0, min(1.0, v.co.z))     # 0 at the bottom, 1 at the top
        attr.data[i].color = (t, 0.0, 0.0, 1.0)

    obj = bpy.data.objects.new(name, me)
    coll.objects.link(obj)
    anchor = bpy.data.objects.new('Anchor_Test', None)
    anchor.empty_display_type = 'ARROWS'
    anchor.parent = obj
    coll.objects.link(anchor)
    print('created', name)
    return obj
