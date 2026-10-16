# Blender 5.x headless: refined bathroom props + four-view sheets + FBX/PNG bake
# Usage: blender -b -P make_bath_v13.py -- <outdir>
import bpy, bmesh, sys, os, math, mathutils
from mathutils import Vector

OUT = sys.argv[sys.argv.index('--') + 1]
os.makedirs(OUT, exist_ok=True)
R = 1024
FV = 512  # each view size in four-view

OBJECTS = ['plate', 'duck', 'soap', 'shampoo', 'cup', 'loofah']

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.samples = 6
    sc.cycles.device = 'CPU'
    sc.render.resolution_x = FV
    sc.render.resolution_y = FV
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = 'JPEG'
    sc.render.image_settings.quality = 92
    world = bpy.data.worlds.new('W')
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes['Background']
    bg.inputs[0].default_value = (1, 1, 1, 1)
    bg.inputs[1].default_value = 1.0

def mat(name, build):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes['Principled BSDF']
    build(nt, b)
    return m

def col(c):
    return (c[0], c[1], c[2], 1)

def flat(c, rough=0.35, metal=0.0, spec=0.5):
    def f(nt, b):
        b.inputs['Base Color'].default_value = col(c)
        if 'Roughness' in b.inputs:
            b.inputs['Roughness'].default_value = rough
        if 'Metallic' in b.inputs:
            b.inputs['Metallic'].default_value = metal
        if 'Specular IOR Level' in b.inputs:
            b.inputs['Specular IOR Level'].default_value = spec
        elif 'Specular' in b.inputs:
            b.inputs['Specular'].default_value = spec
    return mat('flat', f)

def porcelain(base=(0.95, 0.95, 0.93), blue=(0.12, 0.32, 0.72)):
    def f(nt, b):
        tc = nt.nodes.new('ShaderNodeTexCoord')
        w = nt.nodes.new('ShaderNodeTexWave')
        w.wave_type = 'RINGS'
        w.bands_direction = 'Z'
        w.inputs['Scale'].default_value = 4.5
        w.inputs['Distortion'].default_value = 3.5
        w.inputs['Detail'].default_value = 4
        nt.links.new(tc.outputs['Object'], w.inputs['Vector'])
        n = nt.nodes.new('ShaderNodeTexNoise')
        n.inputs['Scale'].default_value = 18
        n.inputs['Detail'].default_value = 8
        nt.links.new(tc.outputs['Object'], n.inputs['Vector'])
        mix = nt.nodes.new('ShaderNodeMix')
        mix.data_type = 'RGBA'
        mix.inputs['Factor'].default_value = 0.0
        # color ramp from wave
        ramp = nt.nodes.new('ShaderNodeValToRGB')
        e = ramp.color_ramp.elements
        e[0].position = 0.42
        e[0].color = col(base)
        e[1].position = 0.55
        e[1].color = col(blue)
        nt.links.new(w.outputs[0], ramp.inputs[0])
        # subtle noise into roughness
        b.inputs['Base Color'].default_value = col(base)
        nt.links.new(ramp.outputs['Color'], b.inputs['Base Color'])
        b.inputs['Roughness'].default_value = 0.18
        if 'Clearcoat' in b.inputs:
            b.inputs['Clearcoat'].default_value = 0.6
            b.inputs['Clearcoat Roughness'].default_value = 0.08
        elif 'Coat Weight' in b.inputs:
            b.inputs['Coat Weight'].default_value = 0.55
            b.inputs['Coat Roughness'].default_value = 0.08
    return mat('porcelain', f)

def assign(o, m):
    o.data.materials.clear()
    o.data.materials.append(m)

def bev(o, w=0.02, s=3):
    m = o.modifiers.new('bev', 'BEVEL')
    m.width = w
    m.segments = s
    m.limit_method = 'ANGLE'

def sub(o, levels=2):
    m = o.modifiers.new('sub', 'SUBSURF')
    m.levels = levels
    m.render_levels = levels

def apply_mods(o):
    bpy.context.view_layer.objects.active = o
    for md in list(o.modifiers):
        try:
            bpy.ops.object.modifier_apply(modifier=md.name)
        except Exception:
            o.modifiers.remove(md)

def join_all(name):
    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for o in objs:
        apply_mods(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    # center on origin at bottom-ish
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    # move so sits near origin
    minz = min((o.matrix_world @ v.co).z for v in o.data.vertices)
    o.location.z -= minz
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(40))
    except Exception:
        bpy.ops.object.shade_smooth()
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    return o

def bake_and_export(name):
    o = bpy.context.active_object
    img = bpy.data.images.new(name + '_base', R, R)
    for m in o.data.materials:
        if not m.use_nodes:
            continue
        n = m.node_tree.nodes.new('ShaderNodeTexImage')
        n.image = img
        m.node_tree.nodes.active = n
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.samples = 6
    sc.render.bake.use_pass_direct = False
    sc.render.bake.use_pass_indirect = False
    sc.render.bake.margin = 8
    bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'})
    png = os.path.join(OUT, name + '_base.png')
    img.filepath_raw = png
    img.file_format = 'PNG'
    img.save()
    fbx = os.path.join(OUT, name + '.fbx')
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(
        filepath=fbx, use_selection=True, apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', bake_space_transform=True)
    return o

def setup_lights():
    for n in ('Key', 'Fill', 'Rim'):
        if n in bpy.data.objects:
            bpy.data.objects.remove(bpy.data.objects[n], do_unlink=True)
    def L(name, loc, energy, color=(1, 0.98, 0.94)):
        light = bpy.data.lights.new(name, 'AREA')
        light.energy = energy
        light.size = 3
        light.color = color
        ob = bpy.data.objects.new(name, light)
        bpy.context.scene.collection.objects.link(ob)
        ob.location = loc
        return ob
    L('Key', (3.5, -2.5, 4.5), 500)
    L('Fill', (-3.5, 1.5, 2.5), 220, (0.75, 0.85, 1))
    L('Rim', (0.5, 3.5, 3.0), 280, (1, 0.9, 0.85))

def cam_ortho(loc, look_at=(0, 0, 0.6), scale=2.4):
    if 'FVCam' in bpy.data.objects:
        cam = bpy.data.objects['FVCam']
    else:
        c = bpy.data.cameras.new('FVCam')
        c.type = 'ORTHO'
        cam = bpy.data.objects.new('FVCam', c)
        bpy.context.scene.collection.objects.link(cam)
        bpy.context.scene.camera = cam
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = scale
    cam.location = loc
    direction = Vector(look_at) - Vector(loc)
    cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    return cam

def render_view(path):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in dir(bpy.types) or True else 'CYCLES'
    # Blender 5 uses EEVEE_NEXT sometimes
    try:
        sc.render.engine = 'BLENDER_EEVEE_NEXT'
    except Exception:
        try:
            sc.render.engine = 'BLENDER_EEVEE'
        except Exception:
            sc.render.engine = 'CYCLES'
            sc.cycles.samples = 16
    sc.render.filepath = path
    sc.render.resolution_x = FV
    sc.render.resolution_y = FV
    bpy.ops.render.render(write_still=True)

def compose_fourview(name, view_paths, out_jpg):
    # Use Blender images to composite
    W = FV * 2
    H = FV * 2
    comp = bpy.data.images.new(name + '_fv', W, H)
    pixels = [1.0] * (W * H * 4)
    labels = ['FRONT', 'RIGHT', 'BACK', 'TOP']
    for i, vp in enumerate(view_paths):
        if not os.path.exists(vp):
            continue
        img = bpy.data.images.load(vp)
        img.colorspace_settings.name = 'sRGB'
        iw, ih = img.size
        px = list(img.pixels)
        ox = (i % 2) * FV
        oy = (1 - i // 2) * FV  # top row first
        for y in range(min(FV, ih)):
            for x in range(min(FV, iw)):
                src = (y * iw + x) * 4
                dst = ((oy + y) * W + (ox + x)) * 4
                pixels[dst:dst+4] = px[src:src+4]
        bpy.data.images.remove(img)
    comp.pixels = pixels
    comp.filepath_raw = out_jpg
    comp.file_format = 'JPEG'
    comp.save()
    # also write via PIL if available for labels
    try:
        from PIL import Image, ImageDraw, ImageFont
        im = Image.open(out_jpg).convert('RGB')
        dr = ImageDraw.Draw(im)
        try:
            font = ImageFont.truetype('arial.ttf', 28)
        except Exception:
            font = ImageFont.load_default()
        positions = [(12, 12), (FV + 12, 12), (12, FV + 12), (FV + 12, FV + 12)]
        for lab, pos in zip(labels, positions):
            dr.rectangle([pos[0]-4, pos[1]-2, pos[0]+110, pos[1]+32], fill=(255, 255, 255))
            dr.text(pos, lab, fill=(30, 30, 30), font=font)
        title = f'{name} — four-view turnaround'
        dr.text((W//2 - 120, H - 36), title, fill=(40, 40, 40), font=font)
        im.save(out_jpg, quality=92)
    except Exception as e:
        print('PIL label skip', e)

def fourview(name, ortho_scale=2.6, look_z=0.7):
    setup_lights()
    # dim world slightly for soft gray studio
    bpy.context.scene.world.node_tree.nodes['Background'].inputs[0].default_value = (0.92, 0.93, 0.95, 1)
    tmp = os.path.join(OUT, '_fv_tmp_' + name)
    os.makedirs(tmp, exist_ok=True)
    views = [
        ('front', (0, -4.5, look_z)),
        ('right', (4.5, 0, look_z)),
        ('back', (0, 4.5, look_z)),
        ('top', (0, 0, 5.5)),
    ]
    paths = []
    for i, (lab, loc) in enumerate(views):
        if lab == 'top':
            cam = cam_ortho(loc, look_at=(0, 0, look_z), scale=ortho_scale)
            cam.rotation_euler = (0, 0, 0)
            # look down
            cam.rotation_euler = (0, 0, 0)
            direction = Vector((0, 0, look_z)) - Vector(loc)
            cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        else:
            cam_ortho(loc, look_at=(0, 0, look_z), scale=ortho_scale)
        path = os.path.join(tmp, lab + '.jpg')
        render_view(path)
        paths.append(path)
    sheet_dir = os.path.join(OUT, 'refs', name)
    os.makedirs(sheet_dir, exist_ok=True)
    out_jpg = os.path.join(sheet_dir, 'fourview.jpg')
    compose_fourview(name, paths, out_jpg)
    print('FOURVIEW', out_jpg)
    return out_jpg

# ---------- builders ----------

def build_plate():
    reset()
    # dish body via lathe-like cylinder reshape
    bpy.ops.mesh.primitive_cylinder_add(vertices=128, radius=1.35, depth=0.16)
    p = bpy.context.active_object
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(p.data)
    for v in bm.verts:
        r = math.hypot(v.co.x, v.co.y)
        # bowl profile
        t = r / 1.35
        if v.co.z > 0:
            v.co.z = 0.05 + 0.12 * (t ** 2)
        else:
            v.co.z = -0.02 + 0.02 * t
            v.co.x *= 0.55 + 0.45 * t
            v.co.y *= 0.55 + 0.45 * t
    bmesh.update_edit_mesh(p.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    bev(p, 0.025, 4)
    assign(p, porcelain())
    # rim
    bpy.ops.mesh.primitive_torus_add(major_radius=1.32, minor_radius=0.05, major_segments=128, minor_segments=16, location=(0, 0, 0.12))
    assign(bpy.context.active_object, flat((0.1, 0.28, 0.65), rough=0.25))
    # inner decorative ring
    bpy.ops.mesh.primitive_torus_add(major_radius=0.72, minor_radius=0.012, major_segments=96, minor_segments=10, location=(0, 0, 0.09))
    assign(bpy.context.active_object, flat((0.85, 0.7, 0.2), rough=0.3))
    # floral bumps (small spheres around)
    for i in range(8):
        a = i * math.pi / 4
        x, y = math.cos(a) * 1.0, math.sin(a) * 1.0
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.06, segments=16, ring_count=8, location=(x, y, 0.1))
        assign(bpy.context.active_object, flat((0.15, 0.4, 0.75), rough=0.3))
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.025, segments=12, ring_count=6, location=(x * 0.85, y * 0.85, 0.11))
        assign(bpy.context.active_object, flat((0.75, 0.2, 0.25), rough=0.35))
    o = join_all('plate')
    bake_and_export('plate')
    fourview('plate', ortho_scale=3.2, look_z=0.15)
    print('DONE_plate')

def build_duck():
    reset()
    Y = (1.0, 0.82, 0.12)
    Yo = (0.95, 0.7, 0.08)
    # body
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=0.85, location=(0, 0, 0.75))
    body = bpy.context.active_object
    body.scale = (1.05, 0.85, 0.78)
    assign(body, flat(Y, rough=0.28))
    sub(body, 2)
    # chest bulge
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.45, location=(0.35, 0, 0.55))
    c = bpy.context.active_object
    c.scale = (0.9, 0.85, 0.75)
    assign(c, flat(Y, rough=0.28))
    # head
    bpy.ops.mesh.primitive_uv_sphere_add(segments=40, ring_count=20, radius=0.42, location=(0.55, 0, 1.45))
    head = bpy.context.active_object
    assign(head, flat(Y, rough=0.25))
    # beak
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.18, radius2=0.06, depth=0.38, location=(0.95, 0, 1.38), rotation=(0, math.radians(90), 0))
    beak = bpy.context.active_object
    beak.scale = (0.7, 1.1, 1)
    assign(beak, flat((0.95, 0.45, 0.1), rough=0.35))
    # lower beak
    bpy.ops.mesh.primitive_cone_add(vertices=20, radius1=0.14, radius2=0.05, depth=0.3, location=(0.9, 0, 1.28), rotation=(0, math.radians(100), 0))
    assign(bpy.context.active_object, flat((0.9, 0.4, 0.08), rough=0.4))
    # eyes
    for s in (-1, 1):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.08, segments=16, ring_count=8, location=(0.72, s * 0.22, 1.55))
        assign(bpy.context.active_object, flat((0.05, 0.05, 0.05), rough=0.15))
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.03, segments=12, ring_count=6, location=(0.78, s * 0.22, 1.58))
        assign(bpy.context.active_object, flat((1, 1, 1), rough=0.2))
    # wings
    for s in (-1, 1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.35, location=(-0.05, s * 0.7, 0.85))
        w = bpy.context.active_object
        w.scale = (0.85, 0.35, 0.55)
        w.rotation_euler = (s * 0.35, 0.2, s * 0.2)
        assign(w, flat(Yo, rough=0.3))
    # tail
    bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=0.28, radius2=0.02, depth=0.45, location=(-0.85, 0, 1.05), rotation=(0, math.radians(-55), 0))
    assign(bpy.context.active_object, flat(Y, rough=0.28))
    # ring on head
    bpy.ops.mesh.primitive_torus_add(major_radius=0.12, minor_radius=0.02, location=(0.55, 0, 1.85), major_segments=24, minor_segments=10)
    assign(bpy.context.active_object, flat((0.2, 0.55, 0.9), rough=0.25, metal=0.2))
    join_all('duck')
    bake_and_export('duck')
    fourview('duck', ortho_scale=3.0, look_z=0.9)
    print('DONE_duck')

def build_soap():
    reset()
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.35))
    s = bpy.context.active_object
    s.scale = (1.3, 0.8, 0.35)
    bev(s, 0.12, 5)
    sub(s, 2)
    assign(s, flat((0.95, 0.75, 0.85), rough=0.45))
    # swirl inset as torus flattened
    bpy.ops.mesh.primitive_torus_add(major_radius=0.35, minor_radius=0.05, location=(0, 0, 0.55), major_segments=48, minor_segments=12)
    t = bpy.context.active_object
    t.scale = (1.4, 1.0, 0.35)
    assign(t, flat((0.85, 0.45, 0.7), rough=0.4))
    # small flower
    for i in range(5):
        a = i * 2 * math.pi / 5
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.06, location=(math.cos(a) * 0.22, math.sin(a) * 0.16, 0.58))
        assign(bpy.context.active_object, flat((0.95, 0.9, 0.95), rough=0.4))
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.05, location=(0, 0, 0.6))
    assign(bpy.context.active_object, flat((0.9, 0.55, 0.2), rough=0.35))
    join_all('soap')
    bake_and_export('soap')
    fourview('soap', ortho_scale=3.0, look_z=0.35)
    print('DONE_soap')

def build_shampoo():
    reset()
    # bottle body
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.55, depth=1.8, location=(0, 0, 0.95))
    b = bpy.context.active_object
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(b.data)
    for v in bm.verts:
        t = (v.co.z + 0.9) / 1.8
        # squeeze taper
        rmul = 0.85 + 0.2 * math.sin(t * math.pi) - 0.15 * max(0, t - 0.75)
        if abs(v.co.x) > 1e-6 or abs(v.co.y) > 1e-6:
            v.co.x *= rmul
            v.co.y *= rmul * 0.75  # flatter sides
    bmesh.update_edit_mesh(b.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    bev(b, 0.04, 3)
    assign(b, flat((0.25, 0.75, 0.85), rough=0.22))
    # label band
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.57, depth=0.45, location=(0, 0, 0.95))
    lab = bpy.context.active_object
    lab.scale = (1.01, 0.78, 1)
    assign(lab, flat((0.98, 0.98, 0.95), rough=0.4))
    # shoulder
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.42, location=(0, 0, 1.75))
    sh = bpy.context.active_object
    sh.scale = (1.05, 0.8, 0.55)
    assign(sh, flat((0.25, 0.75, 0.85), rough=0.22))
    # neck
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.16, depth=0.25, location=(0, 0, 2.05))
    assign(bpy.context.active_object, flat((0.2, 0.65, 0.75), rough=0.25))
    # flip cap
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.2, depth=0.18, location=(0, 0, 2.25))
    assign(bpy.context.active_object, flat((0.15, 0.35, 0.55), rough=0.3))
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.12, 0, 2.38))
    cap = bpy.context.active_object
    cap.scale = (0.22, 0.14, 0.06)
    bev(cap, 0.02, 2)
    assign(cap, flat((0.15, 0.35, 0.55), rough=0.3))
    # pump hint sphere
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.05, location=(0, 0, 2.4))
    assign(bpy.context.active_object, flat((0.9, 0.9, 0.92), rough=0.2, metal=0.3))
    join_all('shampoo')
    bake_and_export('shampoo')
    fourview('shampoo', ortho_scale=3.2, look_z=1.1)
    print('DONE_shampoo')

def build_cup():
    reset()
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.7, depth=1.2, location=(0, 0, 0.65))
    c = bpy.context.active_object
    # hollow via solidify-like: scale inner
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.inset_faces(thickness=0.08)
    bm = bmesh.from_edit_mesh(c.data)
    for v in bm.verts:
        if v.select and v.co.z > 0.2:
            v.co.z = 0.15  # push bottom of inset down? actually select inset
    # better hollow: delete top face and extrude inward manually later
    bmesh.update_edit_mesh(c.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    # redo cup properly
    bpy.data.objects.remove(c, do_unlink=True)
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.7, depth=1.15, location=(0, 0, 0.6))
    outer = bpy.context.active_object
    assign(outer, porcelain((0.96, 0.96, 0.94), (0.2, 0.55, 0.75)))
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.58, depth=1.05, location=(0, 0, 0.72))
    inner = bpy.context.active_object
    # boolean difference
    mod = outer.modifiers.new('bool', 'BOOLEAN')
    mod.operation = 'DIFFERENCE'
    mod.object = inner
    bpy.context.view_layer.objects.active = outer
    apply_mods(outer)
    bpy.data.objects.remove(inner, do_unlink=True)
    bev(outer, 0.02, 2)
    # handle
    bpy.ops.mesh.primitive_torus_add(major_radius=0.32, minor_radius=0.06, major_segments=32, minor_segments=12,
                                    location=(0.85, 0, 0.7), rotation=(math.radians(90), 0, math.radians(90)))
    h = bpy.context.active_object
    # cut handle a bit - just use as is
    assign(h, flat((0.92, 0.92, 0.9), rough=0.3))
    # rim stripe
    bpy.ops.mesh.primitive_torus_add(major_radius=0.68, minor_radius=0.025, major_segments=64, minor_segments=10, location=(0, 0, 1.15))
    assign(bpy.context.active_object, flat((0.15, 0.45, 0.7), rough=0.28))
    join_all('cup')
    bake_and_export('cup')
    fourview('cup', ortho_scale=2.8, look_z=0.7)
    print('DONE_cup')

def build_loofah():
    reset()
    # main oval sponge from icosphere scaled
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=4, radius=0.9, location=(0, 0, 0.7))
    L = bpy.context.active_object
    L.scale = (1.15, 0.75, 0.7)
    assign(L, flat((0.95, 0.78, 0.35), rough=0.85))
    # bumpy surface via displace
    tex = bpy.data.textures.new('loof', 'CLOUDS')
    tex.noise_scale = 0.45
    d = L.modifiers.new('disp', 'DISPLACE')
    d.texture = tex
    d.strength = 0.12
    sub(L, 1)
    # woven bands (torus rings)
    for i, z in enumerate((0.45, 0.7, 0.95)):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.85 - i * 0.05, minor_radius=0.04, major_segments=48, minor_segments=10,
                                        location=(0, 0, z), rotation=(math.radians(12 * (i - 1)), 0, 0))
        t = bpy.context.active_object
        t.scale = (1.15, 0.75, 1)
        assign(t, flat((0.85, 0.65, 0.25), rough=0.7))
    # string loop
    bpy.ops.mesh.primitive_torus_add(major_radius=0.22, minor_radius=0.025, major_segments=24, minor_segments=10,
                                    location=(0, 0, 1.45), rotation=(math.radians(90), 0, 0))
    assign(bpy.context.active_object, flat((0.55, 0.35, 0.15), rough=0.5))
    join_all('loofah')
    bake_and_export('loofah')
    fourview('loofah', ortho_scale=3.0, look_z=0.75)
    print('DONE_loofah')

def main():
    builders = {
        'plate': build_plate,
        'duck': build_duck,
        'soap': build_soap,
        'shampoo': build_shampoo,
        'cup': build_cup,
        'loofah': build_loofah,
    }
    only = os.environ.get('ONLY')
    names = [only] if only in builders else list(builders.keys())
    for n in names:
        print('BUILD', n)
        builders[n]()
    print('ALL_DONE')

if __name__ == '__main__':
    main()
