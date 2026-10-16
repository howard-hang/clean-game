# -*- coding: utf-8 -*-
"""Generate stylized FBX + baked albedo for 洗洗更解压 v5 themes."""
import bpy, bmesh, sys, os, math
out = sys.argv[sys.argv.index('--')+1]
os.makedirs(out, exist_ok=True)
R = 768

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.samples = 2
    sc.cycles.device = 'CPU'
    sc.cycles.use_denoising = False

def mat(name, build):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes['Principled BSDF']; build(nt, b); return m

def col(c):
    return (c[0], c[1], c[2], 1)

def flat(c, rough=0.45, metal=0.0):
    def f(nt, b):
        b.inputs['Base Color'].default_value = col(c)
        if 'Roughness' in b.inputs: b.inputs['Roughness'].default_value = rough
        if 'Metallic' in b.inputs: b.inputs['Metallic'].default_value = metal
    return mat('flat', f)

def noise_mix(c1, c2, scale=5, det=4, kind='noise', ramp=(0.4, 0.6), coord='Object', rough=0.5):
    def f(nt, b):
        tc = nt.nodes.new('ShaderNodeTexCoord')
        if kind == 'noise':
            t = nt.nodes.new('ShaderNodeTexNoise'); t.inputs['Detail'].default_value = det
        elif kind == 'wave':
            t = nt.nodes.new('ShaderNodeTexWave'); t.inputs['Distortion'].default_value = 6; t.inputs['Detail'].default_value = det
        elif kind == 'voronoi':
            t = nt.nodes.new('ShaderNodeTexVoronoi')
        elif kind == 'checker':
            t = nt.nodes.new('ShaderNodeTexChecker')
            t.inputs['Color1'].default_value = col(c1); t.inputs['Color2'].default_value = col(c2)
        elif kind == 'brick':
            t = nt.nodes.new('ShaderNodeTexBrick')
            t.inputs['Color1'].default_value = col(c1); t.inputs['Color2'].default_value = col(c2)
            t.inputs['Mortar'].default_value = col((0.2, 0.2, 0.22))
            t.inputs['Scale'].default_value = scale
            nt.links.new(tc.outputs[coord], t.inputs['Vector'])
            nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
            if 'Roughness' in b.inputs: b.inputs['Roughness'].default_value = rough
            return
        t.inputs['Scale'].default_value = scale
        nt.links.new(tc.outputs[coord], t.inputs['Vector'])
        if kind == 'checker':
            nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
        else:
            r = nt.nodes.new('ShaderNodeValToRGB')
            e = r.color_ramp.elements; e[0].position, e[1].position = ramp
            e[0].color = col(c1); e[1].color = col(c2)
            src = t.outputs[0 if kind != 'voronoi' else 'Distance']
            nt.links.new(src, r.inputs['Fac']); nt.links.new(r.outputs['Color'], b.inputs['Base Color'])
        if 'Roughness' in b.inputs: b.inputs['Roughness'].default_value = rough
    return mat('n', f)

def assign(o, m):
    o.data.materials.clear(); o.data.materials.append(m)

def bev(o, w=0.03, s=3):
    m = o.modifiers.new('b', 'BEVEL'); m.width = w; m.segments = s; m.limit_method = 'ANGLE'

def finish(name):
    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not objs:
        print('NO MESH', name); return
    for o in objs:
        bpy.context.view_layer.objects.active = o
        for md in list(o.modifiers):
            try: bpy.ops.object.modifier_apply(modifier=md.name)
            except Exception: pass
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    o = bpy.context.active_object
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(40))
    except Exception:
        bpy.ops.object.shade_smooth()
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.012)
    bpy.ops.object.mode_set(mode='OBJECT')
    img = bpy.data.images.new(name + '_base', R, R)
    for m in o.data.materials:
        if not m or not m.use_nodes: continue
        n = m.node_tree.nodes.new('ShaderNodeTexImage'); n.image = img; m.node_tree.nodes.active = n
    sc = bpy.context.scene
    sc.render.bake.use_pass_direct = False
    sc.render.bake.use_pass_indirect = False
    sc.render.bake.margin = 8
    bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'})
    img.filepath_raw = os.path.join(out, name + '_base.png')
    img.file_format = 'PNG'; img.save()
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(out, name + '.fbx'), use_selection=True,
        apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
        mesh_smooth_type='FACE', bake_space_transform=True)
    print('OK', name)

# ========== BATHROOM ==========
def make_plate():
    reset(); WHITE=(0.95,0.95,0.92); BLUE=(0.12,0.32,0.72); GOLD=(0.85,0.65,0.18)
    bpy.ops.mesh.primitive_cylinder_add(vertices=96, radius=1.35, depth=0.12)
    p = bpy.context.active_object
    bpy.ops.object.mode_set(mode='EDIT'); bm = bmesh.from_edit_mesh(p.data)
    for v in bm.verts:
        if v.co.z < 0: v.co.x *= 0.62; v.co.y *= 0.62
        elif v.co.z > 0: v.co.z += 0.02 * (1 - (v.co.x**2+v.co.y**2)**0.5/1.4)
    bmesh.update_edit_mesh(p.data); bpy.ops.object.mode_set(mode='OBJECT'); bev(p, 0.035, 4)
    assign(p, noise_mix(WHITE, BLUE, scale=10, kind='wave', ramp=(0.48, 0.52), rough=0.25))
    bpy.ops.mesh.primitive_torus_add(major_radius=1.35, minor_radius=0.07, major_segments=96, minor_segments=14, location=(0,0,0.07))
    assign(bpy.context.active_object, flat(BLUE, 0.3))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.95, minor_radius=0.02, major_segments=96, minor_segments=8, location=(0,0,0.08))
    assign(bpy.context.active_object, flat(GOLD, 0.2, 0.7))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.55, minor_radius=0.015, major_segments=64, minor_segments=8, location=(0,0,0.08))
    assign(bpy.context.active_object, flat(BLUE, 0.3))
    # center flower dots
    for i in range(6):
        a = i * math.pi/3
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.06, segments=12, ring_count=8, location=(math.cos(a)*0.28, math.sin(a)*0.28, 0.09))
        assign(bpy.context.active_object, flat(BLUE if i%2==0 else GOLD, 0.35))
    finish('plate')

def make_duck():
    reset(); Y=(1.0,0.82,0.14); YO=(1.0,0.68,0.05); OR=(1.0,0.42,0.05)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=1); o=bpy.context.active_object
    o.scale=(1.25,1.05,0.85); assign(o, noise_mix(Y, YO, scale=4, kind='noise', rough=0.4)); bev(o, 0.02, 2)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.58, location=(0.82,0,0.88))
    assign(bpy.context.active_object, flat(Y, 0.4))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.26, location=(1.35,0,0.74)); o=bpy.context.active_object
    o.scale=(1.35,1,0.48); assign(o, flat(OR, 0.45))
    for s in (-1,1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.11, location=(1.2,s*0.26,1.02)); assign(bpy.context.active_object, flat((0.05,0.05,0.06)))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.045, location=(1.28,s*0.28,1.07)); assign(bpy.context.active_object, flat((1,1,1)))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.5, location=(-0.05,s*0.9,0.18)); o=bpy.context.active_object
        o.scale=(1.15,0.28,0.65); assign(o, flat(YO, 0.4))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.1, location=(1.08,s*0.4,0.78)); o=bpy.context.active_object
        o.scale=(1,0.4,0.7); assign(o, flat((1,0.5,0.5), 0.5))
    bpy.ops.mesh.primitive_cone_add(radius1=0.32, depth=0.55, location=(-1.3,0,0.48), rotation=(0,-2.15,0)); assign(bpy.context.active_object, flat(Y, 0.4))
    # bow tie
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.55,0,0.55)); o=bpy.context.active_object; o.scale=(0.08,0.35,0.18); assign(o, flat((0.85,0.15,0.25)))
    finish('duck')

def make_soap():
    reset()
    bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.active_object; o.scale=(1.4,0.85,0.55); bev(o, 0.12, 5)
    assign(o, noise_mix((0.95,0.55,0.75), (0.85,0.35,0.6), scale=3, kind='noise', rough=0.35))
    # embossed oval
    bpy.ops.mesh.primitive_torus_add(major_radius=0.45, minor_radius=0.04, major_segments=48, minor_segments=10, location=(0,0,0.28), rotation=(0,0,0))
    o=bpy.context.active_object; o.scale=(1.2,0.8,1); assign(o, flat((0.98,0.75,0.85), 0.3))
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.12, location=(0,0,0.3)); assign(bpy.context.active_object, flat((0.9,0.25,0.5)))
    finish('soap')

def make_shampoo():
    reset()
    # bottle body
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.55, depth=1.8); o=bpy.context.active_object; bev(o, 0.06, 3)
    assign(o, noise_mix((0.25,0.55,0.95), (0.15,0.35,0.75), scale=2, kind='noise', rough=0.25))
    # shoulder
    bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=0.55, radius2=0.22, depth=0.35, location=(0,0,1.05)); assign(bpy.context.active_object, flat((0.2,0.45,0.9), 0.25))
    # neck + cap
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.18, depth=0.35, location=(0,0,1.35)); assign(bpy.context.active_object, flat((0.9,0.9,0.92), 0.3))
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.28, depth=0.28, location=(0,0,1.6)); assign(bpy.context.active_object, flat((0.95,0.75,0.2), 0.35))
    # label band
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.56, depth=0.55, location=(0,0,0.1)); assign(bpy.context.active_object, flat((1,1,1), 0.4))
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.565, depth=0.12, location=(0,0,0.1)); assign(bpy.context.active_object, flat((0.95,0.55,0.15)))
    finish('shampoo')

def make_toothbrush_cup():
    reset()
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.7, depth=1.4); o=bpy.context.active_object
    # hollow-ish look via inner cylinder darker
    assign(o, noise_mix((0.55,0.85,0.75), (0.35,0.7,0.65), scale=6, kind='wave', rough=0.2)); bev(o, 0.04, 3)
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.55, depth=1.2, location=(0,0,0.05)); assign(bpy.context.active_object, flat((0.9,0.95,0.95), 0.15))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.7, minor_radius=0.05, major_segments=64, minor_segments=12, location=(0,0,0.7)); assign(bpy.context.active_object, flat((0.3,0.7,0.65), 0.25))
    # toothbrush sticks
    for i, c in enumerate([(0.95,0.3,0.35), (0.3,0.55,0.95), (0.95,0.85,0.2)]):
        a = -0.3 + i*0.3
        bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.05, depth=1.5, location=(a, 0.15, 1.1)); assign(bpy.context.active_object, flat(c))
        bpy.ops.mesh.primitive_cube_add(size=1, location=(a, 0.15, 1.85)); o=bpy.context.active_object; o.scale=(0.12,0.06,0.22); assign(o, flat((0.95,0.95,0.98)))
    finish('cup')

def make_loofah():
    reset()
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0); o=bpy.context.active_object
    assign(o, noise_mix((1.0,0.55,0.35), (0.9,0.35,0.2), scale=8, kind='voronoi', ramp=(0.3,0.5), rough=0.85))
    # string loop
    bpy.ops.mesh.primitive_torus_add(major_radius=0.35, minor_radius=0.04, major_segments=32, minor_segments=10, location=(0,0,1.05), rotation=(math.pi/2,0,0))
    assign(bpy.context.active_object, flat((0.85,0.75,0.55), 0.5))
    finish('loofah')

# ========== KITCHEN ==========
def make_pan():
    reset()
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=1.2, depth=0.25); o=bpy.context.active_object
    bpy.ops.object.mode_set(mode='EDIT'); bm=bmesh.from_edit_mesh(o.data)
    for v in bm.verts:
        if v.co.z > 0: v.co.z += 0.15
        r=(v.co.x**2+v.co.y**2)**0.5
        if r > 1.05 and v.co.z > 0: v.co.z += 0.2
    bmesh.update_edit_mesh(o.data); bpy.ops.object.mode_set(mode='OBJECT'); bev(o, 0.03, 3)
    assign(o, flat((0.12,0.12,0.14), 0.35, 0.85))
    # inner
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=1.05, depth=0.08, location=(0,0,0.08)); assign(bpy.context.active_object, flat((0.18,0.18,0.2), 0.4, 0.7))
    # handle
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.1, depth=1.4, location=(1.9,0,0.15), rotation=(0,math.pi/2,0)); assign(bpy.context.active_object, flat((0.25,0.2,0.18), 0.6))
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.14, depth=0.5, location=(2.5,0,0.15), rotation=(0,math.pi/2,0)); assign(bpy.context.active_object, flat((0.55,0.35,0.2), 0.55))
    # rivets
    for x in (1.15, 1.35):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.06, location=(x,0,0.25)); assign(bpy.context.active_object, flat((0.7,0.7,0.72), 0.3, 0.9))
    finish('pan')

def make_teapot():
    reset()
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=1.0); o=bpy.context.active_object
    o.scale=(1.15,1.15,0.95); assign(o, noise_mix((0.75,0.15,0.18), (0.55,0.08,0.1), scale=3, kind='noise', rough=0.25)); bev(o, 0.02)
    # lid
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.55, location=(0,0,0.85)); o=bpy.context.active_object
    o.scale=(1,1,0.45); assign(o, flat((0.7,0.12,0.15), 0.25))
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.12, location=(0,0,1.15)); assign(bpy.context.active_object, flat((0.85,0.7,0.25), 0.3, 0.6))
    # spout
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.22, radius2=0.1, depth=0.9, location=(1.15,0,0.25), rotation=(0,1.1,0)); assign(bpy.context.active_object, flat((0.7,0.12,0.15), 0.25))
    # handle
    bpy.ops.mesh.primitive_torus_add(major_radius=0.45, minor_radius=0.08, major_segments=32, minor_segments=12, location=(-1.05,0,0.15), rotation=(math.pi/2,0,0))
    assign(bpy.context.active_object, flat((0.65,0.1,0.12), 0.3))
    # gold band
    bpy.ops.mesh.primitive_torus_add(major_radius=1.05, minor_radius=0.035, major_segments=64, minor_segments=10, location=(0,0,0.2)); assign(bpy.context.active_object, flat((0.85,0.7,0.25), 0.25, 0.8))
    finish('teapot')

def make_mug():
    reset()
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.7, depth=1.5); o=bpy.context.active_object; bev(o, 0.05, 3)
    assign(o, noise_mix((0.95,0.95,0.92), (0.2,0.45,0.75), scale=1.5, kind='wave', ramp=(0.45,0.55), rough=0.3))
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.55, depth=1.3, location=(0,0,0.05)); assign(bpy.context.active_object, flat((0.92,0.9,0.85), 0.2))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.45, minor_radius=0.08, major_segments=24, minor_segments=12, location=(0.85,0,0), rotation=(math.pi/2,0,0))
    assign(bpy.context.active_object, flat((0.95,0.95,0.92), 0.3))
    # stripe
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.71, depth=0.25, location=(0,0,0.2)); assign(bpy.context.active_object, flat((0.9,0.35,0.25)))
    finish('mug')

def make_bowl():
    reset()
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=1.2); o=bpy.context.active_object
    bpy.ops.object.mode_set(mode='EDIT'); bm=bmesh.from_edit_mesh(o.data)
    for v in list(bm.verts):
        if v.co.z > 0.15: bm.verts.remove(v)
    bmesh.update_edit_mesh(o.data); bpy.ops.object.mode_set(mode='OBJECT')
    o.scale=(1.2,1.2,0.85); bev(o, 0.04, 3)
    assign(o, noise_mix((0.15,0.45,0.55), (0.1,0.3,0.4), scale=4, kind='noise', rough=0.25))
    bpy.ops.mesh.primitive_torus_add(major_radius=1.35, minor_radius=0.05, major_segments=64, minor_segments=12, location=(0,0,0.12)); assign(bpy.context.active_object, flat((0.85,0.75,0.35), 0.3, 0.5))
    # foot
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.45, depth=0.15, location=(0,0,-0.95)); assign(bpy.context.active_object, flat((0.12,0.35,0.45), 0.3))
    finish('bowl')

def make_cutting_board():
    reset()
    bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.active_object; o.scale=(2.0,1.3,0.18); bev(o, 0.06, 4)
    assign(o, noise_mix((0.78,0.55,0.32), (0.55,0.35,0.18), scale=2.5, kind='wave', rough=0.55))
    # juice groove
    bpy.ops.mesh.primitive_torus_add(major_radius=0.95, minor_radius=0.04, major_segments=48, minor_segments=10, location=(0,0,0.1)); o=bpy.context.active_object
    o.scale=(1.7,1.1,1); assign(o, flat((0.6,0.4,0.22), 0.6))
    # handle hole ring
    bpy.ops.mesh.primitive_torus_add(major_radius=0.18, minor_radius=0.05, major_segments=24, minor_segments=10, location=(-1.7,0,0), rotation=(math.pi/2,0,0))
    assign(bpy.context.active_object, flat((0.7,0.48,0.28), 0.5))
    finish('cuttingboard')

def make_kettle():
    reset()
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.85, depth=1.4); o=bpy.context.active_object; bev(o, 0.05, 3)
    assign(o, flat((0.75,0.78,0.82), 0.2, 0.9))
    bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=0.85, radius2=0.35, depth=0.4, location=(0,0,0.9)); assign(bpy.context.active_object, flat((0.7,0.72,0.76), 0.2, 0.9))
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.2, depth=0.25, location=(0,0,1.2)); assign(bpy.context.active_object, flat((0.2,0.2,0.22), 0.4))
    # spout
    bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=0.18, radius2=0.08, depth=0.7, location=(1.0,0,0.5), rotation=(0,1.0,0)); assign(bpy.context.active_object, flat((0.7,0.72,0.76), 0.25, 0.85))
    # handle
    bpy.ops.mesh.primitive_torus_add(major_radius=0.55, minor_radius=0.07, major_segments=24, minor_segments=10, location=(0,0,1.35), rotation=(0,0,0)); o=bpy.context.active_object
    o.scale=(0.7,1,1); assign(o, flat((0.15,0.15,0.16), 0.45))
    # blue accent band
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.86, depth=0.15, location=(0,0,0.2)); assign(bpy.context.active_object, flat((0.2,0.45,0.85)))
    finish('kettle')

# ========== GARAGE ==========
def make_toolbox():
    reset()
    bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.active_object; o.scale=(1.8,1.0,1.0); bev(o, 0.04, 3)
    assign(o, flat((0.85,0.25,0.12), 0.4))
    # lid seam
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.35)); o=bpy.context.active_object; o.scale=(1.82,1.02,0.08); assign(o, flat((0.7,0.2,0.1), 0.4))
    # latches
    for x in (-0.6, 0.6):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x,1.02,0.15)); o=bpy.context.active_object; o.scale=(0.2,0.08,0.25); assign(o, flat((0.75,0.75,0.3), 0.3, 0.7))
    # handle
    bpy.ops.mesh.primitive_torus_add(major_radius=0.4, minor_radius=0.06, major_segments=24, minor_segments=10, location=(0,0,1.15), rotation=(math.pi/2,0,0)); o=bpy.context.active_object
    o.scale=(1.5,1,1); assign(o, flat((0.2,0.2,0.22), 0.45))
    # sticker
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,-1.01,0)); o=bpy.context.active_object; o.scale=(0.7,0.02,0.35); assign(o, flat((0.95,0.9,0.2)))
    finish('toolbox')

def make_tire():
    reset()
    bpy.ops.mesh.primitive_torus_add(major_radius=1.1, minor_radius=0.4, major_segments=48, minor_segments=24)
    assign(bpy.context.active_object, noise_mix((0.12,0.12,0.12), (0.25,0.25,0.25), scale=12, kind='noise', rough=0.8))
    # rim
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.75, depth=0.35); assign(bpy.context.active_object, flat((0.75,0.78,0.82), 0.25, 0.85))
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.35, depth=0.4); assign(bpy.context.active_object, flat((0.15,0.15,0.16), 0.4))
    # spokes
    for i in range(5):
        a = i * 2*math.pi/5
        bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.active_object
        o.scale=(0.7,0.08,0.12); o.rotation_euler=(0,0,a); o.location=(math.cos(a)*0.35, math.sin(a)*0.35, 0)
        assign(o, flat((0.7,0.72,0.76), 0.3, 0.8))
    # hubcap center
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.2, location=(0,0,0.15)); assign(bpy.context.active_object, flat((0.85,0.15,0.15), 0.3))
    finish('tire')

def make_jerrycan():
    reset()
    bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.active_object; o.scale=(1.0,0.7,1.4); bev(o, 0.08, 4)
    assign(o, flat((0.15,0.55,0.25), 0.45))
    # spout
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.18, depth=0.4, location=(0.55,0,1.5), rotation=(0,0.4,0)); assign(bpy.context.active_object, flat((0.2,0.2,0.2)))
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.22, depth=0.15, location=(0.7,0,1.7)); assign(bpy.context.active_object, flat((0.85,0.75,0.2)))
    # handle
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,1.55)); o=bpy.context.active_object; o.scale=(0.55,0.12,0.12); assign(o, flat((0.12,0.45,0.2)))
    # diamond plate panel
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,-0.71,0.2)); o=bpy.context.active_object; o.scale=(0.7,0.02,0.7)
    assign(o, noise_mix((0.2,0.6,0.3), (0.1,0.4,0.2), scale=8, kind='checker', rough=0.5))
    finish('jerrycan')

def make_helmet():
    reset()
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=1.0); o=bpy.context.active_object
    bpy.ops.object.mode_set(mode='EDIT'); bm=bmesh.from_edit_mesh(o.data)
    for v in list(bm.verts):
        if v.co.z < -0.15: bm.verts.remove(v)
    bmesh.update_edit_mesh(o.data); bpy.ops.object.mode_set(mode='OBJECT')
    assign(o, flat((0.9,0.2,0.15), 0.3)); bev(o, 0.03, 2)
    # visor
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.55,0,-0.05)); o=bpy.context.active_object; o.scale=(0.35,0.85,0.25); bev(o, 0.05, 2)
    assign(o, flat((0.1,0.12,0.15), 0.15, 0.5))
    # stripe
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.7)); o=bpy.context.active_object; o.scale=(0.15,1.0,0.55); assign(o, flat((0.95,0.9,0.2)))
    # chin strap pegs
    for s in (-1,1):
        bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.06, depth=0.15, location=(0.3,s*0.7,-0.2), rotation=(math.pi/2,0,0)); assign(bpy.context.active_object, flat((0.2,0.2,0.22)))
    finish('helmet')

def make_oilcan():
    reset()
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.65, depth=1.5); assign(bpy.context.active_object, flat((0.75,0.55,0.15), 0.4)); bev(bpy.context.active_object, 0.04)
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.65, radius2=0.2, depth=0.35, location=(0,0,0.9)); assign(bpy.context.active_object, flat((0.7,0.5,0.12), 0.4))
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.15, depth=0.5, location=(0.55,0,1.1), rotation=(0,0.9,0)); assign(bpy.context.active_object, flat((0.3,0.3,0.32), 0.35, 0.6))
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.66, depth=0.3, location=(0,0,0)); assign(bpy.context.active_object, flat((0.2,0.2,0.22)))
    # label
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.66, depth=0.5, location=(0,0,0.1)); assign(bpy.context.active_object, flat((0.9,0.15,0.12)))
    finish('oilcan')

def make_wrench():
    reset()
    # handle
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.12, depth=2.4); assign(bpy.context.active_object, flat((0.7,0.72,0.75), 0.3, 0.85)); bev(bpy.context.active_object, 0.02)
    # head open
    bpy.ops.mesh.primitive_torus_add(major_radius=0.35, minor_radius=0.12, major_segments=24, minor_segments=12, location=(0,0,1.35), rotation=(0,0,0)); o=bpy.context.active_object
    o.scale=(1,0.7,1); assign(o, flat((0.75,0.78,0.82), 0.25, 0.9))
    # box end
    bpy.ops.mesh.primitive_torus_add(major_radius=0.28, minor_radius=0.1, major_segments=6, minor_segments=10, location=(0,0,-1.3)); assign(bpy.context.active_object, flat((0.75,0.78,0.82), 0.25, 0.9))
    # grip grooves
    for z in (-0.3,0,0.3):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.13, minor_radius=0.02, major_segments=16, minor_segments=8, location=(0,0,z)); assign(bpy.context.active_object, flat((0.4,0.4,0.42)))
    finish('wrench')

# ========== GARDEN ==========
def make_flowerpot():
    reset()
    bpy.ops.mesh.primitive_cone_add(vertices=48, radius1=0.9, radius2=1.15, depth=1.4); o=bpy.context.active_object; bev(o, 0.04, 3)
    assign(o, noise_mix((0.75,0.35,0.22), (0.55,0.22,0.12), scale=3, kind='noise', rough=0.55))
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=1.18, depth=0.12, location=(0,0,0.7)); assign(bpy.context.active_object, flat((0.7,0.3,0.18), 0.5))
    # soil
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.95, depth=0.15, location=(0,0,0.55)); assign(bpy.context.active_object, flat((0.3,0.2,0.12), 0.85))
    # flower
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.04, depth=0.9, location=(0,0,1.05)); assign(bpy.context.active_object, flat((0.25,0.55,0.25)))
    for i in range(6):
        a=i*math.pi/3
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.2, location=(math.cos(a)*0.25, math.sin(a)*0.25, 1.55)); assign(bpy.context.active_object, flat((0.95,0.35,0.55), 0.4))
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.15, location=(0,0,1.55)); assign(bpy.context.active_object, flat((0.95,0.85,0.2)))
    finish('flowerpot')

def make_wateringcan():
    reset()
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.75, depth=1.2); assign(bpy.context.active_object, flat((0.25,0.55,0.85), 0.35)); bev(bpy.context.active_object, 0.05)
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.75, radius2=0.5, depth=0.3, location=(0,0,0.75)); assign(bpy.context.active_object, flat((0.2,0.5,0.8), 0.35))
    # spout
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.1, depth=1.3, location=(1.1,0,0.5), rotation=(0,1.0,0)); assign(bpy.context.active_object, flat((0.3,0.55,0.8)))
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.22, location=(1.7,0,0.15)); assign(bpy.context.active_object, flat((0.85,0.75,0.25)))
    # handle
    bpy.ops.mesh.primitive_torus_add(major_radius=0.5, minor_radius=0.07, major_segments=24, minor_segments=10, location=(-0.85,0,0.3), rotation=(math.pi/2,0,0)); assign(bpy.context.active_object, flat((0.2,0.45,0.75)))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.35, minor_radius=0.06, major_segments=20, minor_segments=10, location=(0,0,1.15)); assign(bpy.context.active_object, flat((0.2,0.45,0.75)))
    finish('wateringcan')

def make_gnome():
    reset()
    # body
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.7, radius2=0.45, depth=1.2, location=(0,0,0)); assign(bpy.context.active_object, flat((0.2,0.35,0.75), 0.45)); bev(bpy.context.active_object, 0.03)
    # head
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.4, location=(0,0,0.85)); assign(bpy.context.active_object, flat((0.95,0.78,0.65), 0.5))
    # hat
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.5, radius2=0.02, depth=0.9, location=(0,0,1.35)); assign(bpy.context.active_object, flat((0.85,0.15,0.15), 0.4))
    # beard
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.35, location=(0.15,0,0.55)); o=bpy.context.active_object; o.scale=(0.7,0.9,1.2); assign(o, flat((0.95,0.95,0.92), 0.7))
    # nose
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.1, location=(0.35,0,0.85)); assign(bpy.context.active_object, flat((0.9,0.55,0.5)))
    # boots
    for s in (-1,1):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.25, location=(0.15,s*0.3,-0.55)); o=bpy.context.active_object; o.scale=(1.3,0.7,0.6); assign(o, flat((0.35,0.2,0.12)))
    finish('gnome')

def make_birdhouse():
    reset()
    bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.active_object; o.scale=(1.0,1.0,1.1); bev(o, 0.03, 2)
    assign(o, noise_mix((0.85,0.65,0.35), (0.65,0.45,0.22), scale=3, kind='wave', rough=0.55))
    # roof
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=1.0, depth=0.7, location=(0,0,0.9), rotation=(0,0,math.pi/4)); assign(bpy.context.active_object, flat((0.75,0.25,0.2), 0.45))
    # hole
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.22, depth=0.15, location=(0,-1.0,0.15), rotation=(math.pi/2,0,0)); assign(bpy.context.active_object, flat((0.15,0.1,0.08)))
    # perch
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.05, depth=0.45, location=(0,-1.15,-0.05), rotation=(math.pi/2,0,0)); assign(bpy.context.active_object, flat((0.55,0.35,0.2)))
    # chimney
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.4,0.3,1.15)); o=bpy.context.active_object; o.scale=(0.18,0.18,0.35); assign(o, flat((0.45,0.45,0.48)))
    finish('birdhouse')

def make_frog():
    reset()
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.9); o=bpy.context.active_object; o.scale=(1.2,1.0,0.7)
    assign(o, noise_mix((0.35,0.75,0.35), (0.2,0.55,0.25), scale=4, kind='noise', rough=0.45))
    # eyes
    for s in (-1,1):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.28, location=(0.55,s*0.4,0.55)); assign(bpy.context.active_object, flat((0.95,0.95,0.9)))
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.14, location=(0.72,s*0.4,0.62)); assign(bpy.context.active_object, flat((0.1,0.1,0.1)))
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.05, location=(0.8,s*0.38,0.68)); assign(bpy.context.active_object, flat((1,1,1)))
    # legs
    for s in (-1,1):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.35, location=(-0.3,s*0.85,-0.2)); o=bpy.context.active_object; o.scale=(0.9,0.5,0.45); assign(o, flat((0.3,0.65,0.3)))
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.25, location=(0.6,s*0.7,-0.35)); o=bpy.context.active_object; o.scale=(0.8,0.45,0.35); assign(o, flat((0.3,0.65,0.3)))
    # smile
    bpy.ops.mesh.primitive_torus_add(major_radius=0.35, minor_radius=0.04, major_segments=24, minor_segments=8, location=(0.7,0,0.1), rotation=(math.pi/2,0.3,0)); o=bpy.context.active_object
    o.scale=(0.5,1,0.4); assign(o, flat((0.2,0.45,0.2)))
    finish('frog')

def make_wheelbarrow():
    reset()
    # tub
    bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.active_object; o.scale=(1.6,1.0,0.55); bev(o, 0.05, 3)
    assign(o, flat((0.55,0.2,0.15), 0.4))
    # handles
    for s in (-1,1):
        bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.06, depth=1.6, location=(-1.5,s*0.45,0.1), rotation=(0,math.pi/2.2,0)); assign(bpy.context.active_object, flat((0.45,0.3,0.15), 0.55))
    # wheel
    bpy.ops.mesh.primitive_torus_add(major_radius=0.45, minor_radius=0.1, major_segments=24, minor_segments=12, location=(1.3,0,-0.55), rotation=(math.pi/2,0,0)); assign(bpy.context.active_object, flat((0.15,0.15,0.15), 0.7))
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.2, depth=0.15, location=(1.3,0,-0.55), rotation=(math.pi/2,0,0)); assign(bpy.context.active_object, flat((0.7,0.7,0.72), 0.3, 0.8))
    # legs
    for s in (-1,1):
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.05, depth=0.7, location=(-0.8,s*0.5,-0.4), rotation=(0.4,0,0)); assign(bpy.context.active_object, flat((0.4,0.28,0.15)))
    finish('wheelbarrow')

# ========== also upgrade crate/ball/vase for bathroom extras ==========
def make_crate():
    reset(); W=(0.78,0.52,0.28); D=(0.45,0.27,0.12)
    wood=noise_mix(W,D,scale=3,det=6,kind='wave',ramp=(0.3,0.8)); dark=noise_mix(D,(0.3,0.17,0.07),scale=4,kind='wave')
    for side in range(4):
        a=side*math.pi/2
        for i in range(4):
            z=-0.6+i*0.4
            bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.active_object; o.scale=(1.5,0.08,0.36)
            o.rotation_euler=(0,0,a); o.location=(math.sin(a)*0.75,-math.cos(a)*0.75,z); bev(o,0.025); assign(o,wood)
    for sx in (-1,1):
        for sy in (-1,1):
            bpy.ops.mesh.primitive_cube_add(size=1,location=(sx*0.76,sy*0.76,0)); o=bpy.context.active_object; o.scale=(0.16,0.16,1.62); bev(o,0.03); assign(o,dark)
    bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,0.78)); o=bpy.context.active_object; o.scale=(1.5,1.5,0.08); bev(o,0.02); assign(o,wood)
    for side in range(4):
        a=side*math.pi/2
        for sx in (-0.6,0.6):
            for z in (-0.6,-0.2,0.2,0.6):
                x=sx*math.cos(a)+math.sin(a)*0.8; y=sx*math.sin(a)-math.cos(a)*0.8
                bpy.ops.mesh.primitive_uv_sphere_add(radius=0.035,segments=10,ring_count=6,location=(x,y,z)); assign(bpy.context.active_object,flat((0.55,0.55,0.6),0.3,0.8))
    finish('crate')

def make_ball():
    reset()
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=4, radius=1)
    assign(bpy.context.active_object, noise_mix((0.95,0.95,0.95),(0.9,0.15,0.2),scale=2.0,kind='voronoi',ramp=(0.34,0.38),rough=0.4))
    finish('ball')

def make_vase():
    reset()
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.55, depth=0.3, location=(0,0,-1.0)); assign(bpy.context.active_object, flat((0.15,0.35,0.55),0.25))
    # body via stacked scaled cylinders approximating vase profile
    profiles = [(0.7,-0.7),(0.95,-0.2),(1.05,0.3),(0.85,0.8),(0.55,1.2),(0.45,1.5),(0.55,1.7)]
    for i,(r,z) in enumerate(profiles[:-1]):
        r2,z2 = profiles[i+1]
        bpy.ops.mesh.primitive_cone_add(vertices=48, radius1=r, radius2=r2, depth=abs(z2-z)+0.01, location=(0,0,(z+z2)/2))
        assign(bpy.context.active_object, noise_mix((0.2,0.45,0.7),(0.85,0.75,0.35),scale=6,kind='wave',ramp=(0.45,0.55),rough=0.2))
    # handles
    for s in (-1,1):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.35, minor_radius=0.07, major_segments=24, minor_segments=10, location=(s*0.95,0,0.4), rotation=(math.pi/2,0,0))
        assign(bpy.context.active_object, flat((0.85,0.7,0.25),0.25,0.7))
    # gold bands
    for z in (-0.3, 0.9, 1.55):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.9 if z<1 else 0.5, minor_radius=0.03, major_segments=48, minor_segments=8, location=(0,0,z))
        assign(bpy.context.active_object, flat((0.85,0.7,0.25),0.25,0.8))
    finish('vase')

ALL = [
    # bathroom (6)
    make_plate, make_duck, make_soap, make_shampoo, make_toothbrush_cup, make_loofah,
    # kitchen (6)
    make_pan, make_teapot, make_mug, make_bowl, make_cutting_board, make_kettle,
    # garage (6)
    make_toolbox, make_tire, make_jerrycan, make_helmet, make_oilcan, make_wrench,
    # garden (6)
    make_flowerpot, make_wateringcan, make_gnome, make_birdhouse, make_frog, make_wheelbarrow,
    # extras kept for compatibility / variety
    make_crate, make_ball, make_vase,
]

if __name__ == '__main__':
    only = None
    if '--only' in sys.argv:
        only = sys.argv[sys.argv.index('--only')+1]
    for fn in ALL:
        name = fn.__name__.replace('make_', '')
        if only and only not in name and only not in fn.__name__:
            continue
        print('===', fn.__name__, '===')
        try:
            fn()
        except Exception as e:
            print('FAIL', fn.__name__, e)
            import traceback; traceback.print_exc()
    print('DONE')
