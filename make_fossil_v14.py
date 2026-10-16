# Blender 5.x headless: fossil + excavation-tool props + four-view + FBX/PNG bake
# Usage: blender -b -P make_fossil_v14.py -- <outdir>
# Mirrors make_bath_v13.py pipeline for the archaeological theme.
import bpy, bmesh, sys, os, math, shutil
from mathutils import Vector, Matrix

OUT = sys.argv[sys.argv.index('--') + 1]
os.makedirs(OUT, exist_ok=True)
R = 1024
FV = 512

# Unity Resources path (sibling of clean-game-repo Scripts)
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
RES_DIR = os.path.join(SCRIPT_DIR, 'Assets', 'Resources')

# mesh resource name -> concept-ref folder id
FOLDER = {
    'trex_skull': 'trex',
    'triceratops_skull': 'triceratops',
    'stego_plate': 'stego',
    'ammonite': 'ammonite',
    'trilobite': 'trilobite',
    'mammoth_tusk': 'mammoth',
    'dig_brush': 'brush',
    'dig_chisel': 'chisel',
}

OBJECTS = list(FOLDER.keys())


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


def flat(c, rough=0.45, metal=0.0, spec=0.4):
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


def fossil_bone(base=(0.82, 0.74, 0.58), accent=(0.55, 0.45, 0.32)):
    """Beige/cream porous fossil bone with sediment noise."""
    def f(nt, b):
        tc = nt.nodes.new('ShaderNodeTexCoord')
        n = nt.nodes.new('ShaderNodeTexNoise')
        n.inputs['Scale'].default_value = 14
        n.inputs['Detail'].default_value = 10
        n.inputs['Roughness'].default_value = 0.65
        nt.links.new(tc.outputs['Object'], n.inputs['Vector'])
        n2 = nt.nodes.new('ShaderNodeTexNoise')
        n2.inputs['Scale'].default_value = 42
        n2.inputs['Detail'].default_value = 6
        nt.links.new(tc.outputs['Object'], n2.inputs['Vector'])
        ramp = nt.nodes.new('ShaderNodeValToRGB')
        e = ramp.color_ramp.elements
        e[0].position = 0.35
        e[0].color = col(accent)
        e[1].position = 0.68
        e[1].color = col(base)
        nt.links.new(n.outputs[0], ramp.inputs[0])
        nt.links.new(ramp.outputs['Color'], b.inputs['Base Color'])
        # porous roughness
        mathn = nt.nodes.new('ShaderNodeMath')
        mathn.operation = 'MULTIPLY'
        mathn.inputs[1].default_value = 0.35
        nt.links.new(n2.outputs[0], mathn.inputs[0])
        add = nt.nodes.new('ShaderNodeMath')
        add.operation = 'ADD'
        add.inputs[1].default_value = 0.42
        nt.links.new(mathn.outputs[0], add.inputs[0])
        nt.links.new(add.outputs[0], b.inputs['Roughness'])
        if 'Specular IOR Level' in b.inputs:
            b.inputs['Specular IOR Level'].default_value = 0.25
        elif 'Specular' in b.inputs:
            b.inputs['Specular'].default_value = 0.25
    return mat('fossil_bone', f)


def ivory(base=(0.93, 0.88, 0.75), vein=(0.78, 0.68, 0.52)):
    def f(nt, b):
        tc = nt.nodes.new('ShaderNodeTexCoord')
        w = nt.nodes.new('ShaderNodeTexWave')
        w.wave_type = 'BANDS'
        w.bands_direction = 'Z'
        w.inputs['Scale'].default_value = 6.5
        w.inputs['Distortion'].default_value = 2.2
        w.inputs['Detail'].default_value = 3
        nt.links.new(tc.outputs['Object'], w.inputs['Vector'])
        ramp = nt.nodes.new('ShaderNodeValToRGB')
        e = ramp.color_ramp.elements
        e[0].position = 0.4
        e[0].color = col(vein)
        e[1].position = 0.6
        e[1].color = col(base)
        nt.links.new(w.outputs[0], ramp.inputs[0])
        nt.links.new(ramp.outputs['Color'], b.inputs['Base Color'])
        b.inputs['Roughness'].default_value = 0.32
        if 'Specular IOR Level' in b.inputs:
            b.inputs['Specular IOR Level'].default_value = 0.45
    return mat('ivory', f)


def wood(base=(0.45, 0.28, 0.14)):
    def f(nt, b):
        tc = nt.nodes.new('ShaderNodeTexCoord')
        w = nt.nodes.new('ShaderNodeTexWave')
        w.wave_type = 'BANDS'
        w.bands_direction = 'Z'
        w.inputs['Scale'].default_value = 9
        w.inputs['Distortion'].default_value = 1.5
        nt.links.new(tc.outputs['Object'], w.inputs['Vector'])
        ramp = nt.nodes.new('ShaderNodeValToRGB')
        e = ramp.color_ramp.elements
        e[0].position = 0.4
        e[0].color = col((base[0] * 0.7, base[1] * 0.7, base[2] * 0.7))
        e[1].position = 0.65
        e[1].color = col(base)
        nt.links.new(w.outputs[0], ramp.inputs[0])
        nt.links.new(ramp.outputs['Color'], b.inputs['Base Color'])
        b.inputs['Roughness'].default_value = 0.55
    return mat('wood', f)


def steel(c=(0.55, 0.57, 0.6)):
    def f(nt, b):
        b.inputs['Base Color'].default_value = col(c)
        b.inputs['Roughness'].default_value = 0.28
        b.inputs['Metallic'].default_value = 0.85
        if 'Specular IOR Level' in b.inputs:
            b.inputs['Specular IOR Level'].default_value = 0.7
    return mat('steel', f)


def assign(o, m):
    o.data.materials.clear()
    o.data.materials.append(m)


def bev(o, w=0.02, s=3):
    m = o.modifiers.new('bev', 'BEVEL')
    m.width = w
    m.segments = s
    m.limit_method = 'ANGLE'


def sub(o, levels=1):
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
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
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
    # also into Unity Resources
    if os.path.isdir(RES_DIR):
        shutil.copy2(fbx, os.path.join(RES_DIR, name + '.fbx'))
        shutil.copy2(png, os.path.join(RES_DIR, name + '_base.png'))
        print('RES', name)
    # per-object folder copies
    folder = FOLDER.get(name, name)
    obj_dir = os.path.join(OUT, folder)
    os.makedirs(obj_dir, exist_ok=True)
    shutil.copy2(fbx, os.path.join(obj_dir, name + '.fbx'))
    shutil.copy2(png, os.path.join(obj_dir, name + '_base.png'))
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
        oy = (1 - i // 2) * FV
        for y in range(min(FV, ih)):
            for x in range(min(FV, iw)):
                src = (y * iw + x) * 4
                dst = ((oy + y) * W + (ox + x)) * 4
                pixels[dst:dst + 4] = px[src:src + 4]
        bpy.data.images.remove(img)
    comp.pixels = pixels
    comp.filepath_raw = out_jpg
    comp.file_format = 'JPEG'
    comp.save()
    try:
        from PIL import Image, ImageDraw, ImageFont
        im = Image.open(out_jpg).convert('RGB')
        dr = ImageDraw.Draw(im)
        try:
            font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 26)
        except Exception:
            try:
                font = ImageFont.truetype('arial.ttf', 26)
            except Exception:
                font = ImageFont.load_default()
        positions = [(12, 12), (FV + 12, 12), (12, FV + 12), (FV + 12, FV + 12)]
        for lab, pos in zip(labels, positions):
            dr.rectangle([pos[0] - 4, pos[1] - 2, pos[0] + 110, pos[1] + 32], fill=(255, 255, 255))
            dr.text(pos, lab, fill=(30, 30, 30), font=font)
        title = f'{name} — blender four-view'
        dr.text((W // 2 - 140, H - 36), title, fill=(40, 40, 40), font=font)
        im.save(out_jpg, quality=92)
    except Exception as e:
        print('PIL label skip', e)


def fourview(name, ortho_scale=2.6, look_z=0.7):
    setup_lights()
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
    for lab, loc in views:
        if lab == 'top':
            cam = cam_ortho(loc, look_at=(0, 0, look_z), scale=ortho_scale)
            direction = Vector((0, 0, look_z)) - Vector(loc)
            cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        else:
            cam_ortho(loc, look_at=(0, 0, look_z), scale=ortho_scale)
        path = os.path.join(tmp, lab + '.jpg')
        render_view(path)
        paths.append(path)
    folder = FOLDER.get(name, name)
    sheet_dir = os.path.join(OUT, folder)
    os.makedirs(sheet_dir, exist_ok=True)
    out_jpg = os.path.join(sheet_dir, 'blender_fourview.jpg')
    compose_fourview(name, paths, out_jpg)
    print('FOURVIEW', out_jpg)
    return out_jpg


# ---------- builders ----------

def build_trex_skull():
    """Tyrannosaurus rex skull fossil: open jaws, teeth, fenestrae."""
    reset()
    bone = fossil_bone((0.84, 0.76, 0.60), (0.52, 0.42, 0.30))
    dark = fossil_bone((0.45, 0.36, 0.26), (0.30, 0.24, 0.18))
    tooth_m = flat((0.92, 0.88, 0.78), rough=0.35)

    # main cranium (elongated)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.85, location=(0.15, 0, 1.15))
    cr = bpy.context.active_object
    cr.scale = (1.55, 0.72, 0.78)
    assign(cr, bone)
    sub(cr, 1)

    # snout taper
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.55, location=(1.35, 0, 0.95))
    sn = bpy.context.active_object
    sn.scale = (1.15, 0.55, 0.55)
    assign(sn, bone)

    # nasal bridge ridge
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.9, 0, 1.45))
    ridge = bpy.context.active_object
    ridge.scale = (1.1, 0.12, 0.12)
    bev(ridge, 0.04, 2)
    assign(ridge, bone)

    # antorbital fenestrae (recessed dark cavities)
    for s in (-1, 1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.28, location=(0.55, s * 0.55, 1.05))
        f = bpy.context.active_object
        f.scale = (0.9, 0.35, 0.7)
        assign(f, dark)

    # orbits
    for s in (-1, 1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.22, location=(-0.15, s * 0.58, 1.25))
        o = bpy.context.active_object
        o.scale = (0.7, 0.4, 0.85)
        assign(o, dark)

    # lateral temporal fenestrae
    for s in (-1, 1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.25, location=(-0.55, s * 0.5, 1.1))
        t = bpy.context.active_object
        t.scale = (0.8, 0.35, 0.75)
        assign(t, dark)

    # upper tooth row
    for i in range(10):
        x = 0.35 + i * 0.18
        for s in (-1, 1):
            bpy.ops.mesh.primitive_cone_add(
                vertices=8, radius1=0.045, radius2=0.012, depth=0.22,
                location=(x, s * 0.32, 0.72), rotation=(math.radians(180), 0, 0))
            assign(bpy.context.active_object, tooth_m)

    # mandible (lower jaw) — open
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.7, 0, 0.35))
    jaw = bpy.context.active_object
    jaw.scale = (1.35, 0.38, 0.18)
    jaw.rotation_euler = (0, math.radians(8), 0)
    bev(jaw, 0.05, 2)
    assign(jaw, bone)

    # jaw tip
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.28, location=(1.85, 0, 0.42))
    jt = bpy.context.active_object
    jt.scale = (0.9, 0.55, 0.45)
    assign(jt, bone)

    # lower teeth
    for i in range(8):
        x = 0.5 + i * 0.18
        for s in (-1, 1):
            bpy.ops.mesh.primitive_cone_add(
                vertices=8, radius1=0.04, radius2=0.01, depth=0.18,
                location=(x, s * 0.28, 0.55))
            assign(bpy.context.active_object, tooth_m)

    # occipital / rear knob
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.35, location=(-1.0, 0, 1.05))
    oc = bpy.context.active_object
    oc.scale = (0.7, 0.85, 0.75)
    assign(oc, bone)

    # zygomatic arches
    for s in (-1, 1):
        bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.07, depth=1.0,
                                           location=(-0.2, s * 0.72, 0.95), rotation=(0, math.radians(90), 0))
        assign(bpy.context.active_object, bone)

    join_all('trex_skull')
    bake_and_export('trex_skull')
    fourview('trex_skull', ortho_scale=4.2, look_z=0.95)
    print('DONE_trex_skull')


def build_triceratops_skull():
    """Triceratops skull: 2 brow horns + nasal horn + frill."""
    reset()
    bone = fossil_bone((0.80, 0.72, 0.55), (0.50, 0.40, 0.28))
    horn_m = fossil_bone((0.72, 0.64, 0.48), (0.42, 0.34, 0.24))
    dark = fossil_bone((0.42, 0.34, 0.24), (0.28, 0.22, 0.16))

    # main skull body
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.75, location=(0.2, 0, 0.9))
    body = bpy.context.active_object
    body.scale = (1.3, 0.9, 0.75)
    assign(body, bone)
    sub(body, 1)

    # beak / rostrum
    bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=0.35, radius2=0.08, depth=0.7,
                                   location=(1.35, 0, 0.7), rotation=(0, math.radians(90), 0))
    beak = bpy.context.active_object
    beak.scale = (0.7, 1.0, 0.85)
    assign(beak, bone)

    # nasal horn (shorter)
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=0.12, radius2=0.02, depth=0.55,
                                   location=(0.95, 0, 1.35), rotation=(0, math.radians(-25), 0))
    assign(bpy.context.active_object, horn_m)

    # brow horns (long)
    for s in (-1, 1):
        bpy.ops.mesh.primitive_cone_add(
            vertices=12, radius1=0.14, radius2=0.025, depth=1.15,
            location=(0.15, s * 0.38, 1.55),
            rotation=(s * math.radians(12), math.radians(-35), 0))
        assign(bpy.context.active_object, horn_m)
        # horn base boss
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.16,
                                            location=(0.05, s * 0.35, 1.25))
        assign(bpy.context.active_object, bone)

    # frill — large shield behind skull
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=1.35, depth=0.12, location=(-0.85, 0, 1.35))
    frill = bpy.context.active_object
    frill.scale = (0.55, 1.0, 1.0)
    frill.rotation_euler = (0, math.radians(70), 0)
    assign(frill, bone)
    bev(frill, 0.03, 2)

    # frill scallops / epoccipitals
    for i in range(10):
        a = -math.pi * 0.55 + i * (math.pi * 1.1 / 9)
        # frill local ellipse then tilt
        lx = math.cos(a) * 0.7
        ly = math.sin(a) * 1.25
        # approximate world after rotation around Y ~70deg around (-0.85,0,1.35)
        # place scallops on rim
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=0.12,
                                            location=(-0.85 - math.cos(a) * 0.55 * 0.55,
                                                      math.sin(a) * 1.2,
                                                      1.35 + math.cos(a) * 0.55 * 0.9))
        sc = bpy.context.active_object
        sc.scale = (0.7, 1.0, 0.7)
        assign(sc, horn_m)

    # fenestrae in frill (two openings)
    for s in (-1, 1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.32,
                                            location=(-1.0, s * 0.55, 1.55))
        fen = bpy.context.active_object
        fen.scale = (0.35, 0.85, 0.7)
        assign(fen, dark)

    # orbits
    for s in (-1, 1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.18,
                                            location=(0.35, s * 0.7, 1.0))
        o = bpy.context.active_object
        o.scale = (0.7, 0.4, 0.8)
        assign(o, dark)

    # lower jaw hint
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.7, 0, 0.35))
    j = bpy.context.active_object
    j.scale = (0.9, 0.35, 0.15)
    bev(j, 0.04, 2)
    assign(j, bone)

    join_all('triceratops_skull')
    bake_and_export('triceratops_skull')
    fourview('triceratops_skull', ortho_scale=4.0, look_z=1.1)
    print('DONE_triceratops_skull')


def build_stego_plate():
    """Stegosaurus dorsal plate — diamond with central keel."""
    reset()
    bone = fossil_bone((0.78, 0.70, 0.52), (0.48, 0.38, 0.26))
    keel_m = fossil_bone((0.68, 0.58, 0.42), (0.40, 0.32, 0.22))

    # diamond plate via scaled/rotated cube shaped with bmesh
    bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, 1.0))
    p = bpy.context.active_object
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(p.data)
    for v in bm.verts:
        x, y, z = v.co.x, v.co.y, v.co.z
        # diamond in XZ, thin in Y
        # map cube to kite: |x|/a + |z-center|/b <= 1
        v.co.y *= 0.08
        # taper thickness toward edges
        edge = abs(x) / 1.0 + abs(z) / 1.0
        thin = max(0.15, 1.0 - 0.7 * edge)
        v.co.y *= thin
        # raise to diamond proportions: taller than wide a bit
        v.co.z *= 1.15
        v.co.x *= 0.85
    bmesh.update_edit_mesh(p.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    bev(p, 0.03, 2)
    sub(p, 1)
    assign(p, bone)

    # central vertical keel ridge
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 1.0))
    k = bpy.context.active_object
    k.scale = (0.06, 0.12, 1.15)
    bev(k, 0.02, 2)
    assign(k, keel_m)

    # base thickened root (where plate meets spine)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.15))
    root = bpy.context.active_object
    root.scale = (0.55, 0.18, 0.2)
    bev(root, 0.04, 2)
    assign(root, bone)

    # vascular groove hints (shallow dark lines)
    dark = flat((0.55, 0.45, 0.32), rough=0.6)
    for s in (-1, 1):
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.02, depth=1.6,
                                           location=(s * 0.15, 0.09, 1.0), rotation=(0, 0, 0))
        g = bpy.context.active_object
        g.scale = (1, 1, 1)
        assign(g, dark)

    join_all('stego_plate')
    bake_and_export('stego_plate')
    fourview('stego_plate', ortho_scale=3.2, look_z=1.0)
    print('DONE_stego_plate')


def build_ammonite():
    """Spiral ammonite shell with ribs/sutures."""
    reset()
    shell = fossil_bone((0.72, 0.62, 0.45), (0.42, 0.34, 0.24))
    rib_m = fossil_bone((0.62, 0.52, 0.36), (0.35, 0.28, 0.18))
    suture = flat((0.48, 0.38, 0.26), rough=0.55)

    # logarithmic spiral of torus-like segments
    a0, b_growth = 0.18, 0.14
    n_whorls = 28
    for i in range(n_whorls):
        t = i / n_whorls * 3.6 * math.pi  # ~1.8 turns
        r = a0 * math.exp(b_growth * t)
        x = r * math.cos(t)
        y = r * math.sin(t)
        rad = 0.08 + 0.055 * (i / n_whorls)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=rad, location=(x, y, 0.55))
        seg = bpy.context.active_object
        seg.scale = (1.0, 1.0, 0.85)
        assign(seg, shell)

    # outer aperture flare
    t = 3.6 * math.pi
    r = a0 * math.exp(b_growth * t)
    x, y = r * math.cos(t), r * math.sin(t)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.22, minor_radius=0.07, major_segments=24, minor_segments=10,
                                    location=(x, y, 0.55), rotation=(math.radians(90), 0, t + math.pi / 2))
    assign(bpy.context.active_object, shell)

    # radial ribs on outer whorl
    for i in range(16):
        t = 2.2 * math.pi + i / 16 * 1.4 * math.pi
        r = a0 * math.exp(b_growth * t)
        x, y = r * math.cos(t), r * math.sin(t)
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.025, depth=0.35,
                                           location=(x, y, 0.55),
                                           rotation=(math.radians(90), 0, t))
        rib = bpy.context.active_object
        rib.scale = (1, 1, 0.6)
        assign(rib, rib_m)

    # suture line accents (inner)
    for i in range(8):
        t = 0.8 * math.pi + i / 8 * 1.5 * math.pi
        r = a0 * math.exp(b_growth * t) * 0.95
        x, y = r * math.cos(t), r * math.sin(t)
        bpy.ops.mesh.primitive_torus_add(major_radius=0.06 + 0.02 * i / 8, minor_radius=0.012,
                                        major_segments=16, minor_segments=6,
                                        location=(x * 0.3, y * 0.3, 0.62))
        assign(bpy.context.active_object, suture)

    # umbilicus center
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.12, location=(0, 0, 0.55))
    assign(bpy.context.active_object, rib_m)

    join_all('ammonite')
    bake_and_export('ammonite')
    fourview('ammonite', ortho_scale=2.8, look_z=0.55)
    print('DONE_ammonite')


def build_trilobite():
    """Classic trilobite: cephalon / thorax / pygidium, trilobed."""
    reset()
    bone = fossil_bone((0.75, 0.66, 0.48), (0.45, 0.36, 0.25))
    axis_m = fossil_bone((0.65, 0.55, 0.38), (0.38, 0.30, 0.20))
    dark = flat((0.40, 0.32, 0.22), rough=0.55)

    # cephalon (head shield) — semicircular
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.7, location=(0.85, 0, 0.35))
    ceph = bpy.context.active_object
    ceph.scale = (0.85, 1.15, 0.35)
    assign(ceph, bone)

    # glabella (central cephalic lobe)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.28, location=(0.75, 0, 0.5))
    gl = bpy.context.active_object
    gl.scale = (1.1, 0.55, 0.55)
    assign(gl, axis_m)

    # genal spines
    for s in (-1, 1):
        bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=0.1, radius2=0.02, depth=0.55,
                                       location=(0.35, s * 0.95, 0.3),
                                       rotation=(0, math.radians(100), s * math.radians(35)))
        assign(bpy.context.active_object, bone)

    # eyes (raised crescent bumps)
    for s in (-1, 1):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.12, minor_radius=0.04, major_segments=16, minor_segments=8,
                                        location=(0.95, s * 0.45, 0.48),
                                        rotation=(math.radians(90), 0, s * math.radians(20)))
        assign(bpy.context.active_object, dark)

    # thorax segments (6)
    for i in range(6):
        x = 0.15 - i * 0.28
        w = 1.05 - i * 0.04
        # pleural lobe left/right
        for s in (-1, 1):
            bpy.ops.mesh.primitive_cube_add(size=1, location=(x, s * 0.42, 0.32))
            pl = bpy.context.active_object
            pl.scale = (0.12, 0.38 * w, 0.1)
            bev(pl, 0.03, 2)
            assign(pl, bone)
        # axial ring
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, 0, 0.42))
        ax = bpy.context.active_object
        ax.scale = (0.11, 0.22, 0.12)
        bev(ax, 0.025, 2)
        assign(ax, axis_m)

    # pygidium (tail shield)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=10, radius=0.45, location=(-1.55, 0, 0.32))
    pyg = bpy.context.active_object
    pyg.scale = (0.9, 0.95, 0.3)
    assign(pyg, bone)

    # pygidium axis
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.18, location=(-1.45, 0, 0.42))
    pa = bpy.context.active_object
    pa.scale = (1.2, 0.5, 0.5)
    assign(pa, axis_m)

    # faint pleural furrows on cephalon
    for s in (-1, 1):
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.02, depth=0.6,
                                           location=(0.85, s * 0.35, 0.42),
                                           rotation=(0, math.radians(90), s * 0.3))
        assign(bpy.context.active_object, dark)

    join_all('trilobite')
    bake_and_export('trilobite')
    fourview('trilobite', ortho_scale=3.4, look_z=0.4)
    print('DONE_trilobite')


def build_mammoth_tusk():
    """Curved woolly mammoth tusk (ivory)."""
    reset()
    iv = ivory()
    tip_m = ivory((0.88, 0.82, 0.68), (0.7, 0.6, 0.45))

    # build tapered curved tusk from sphere chain along a helix/arc
    n = 24
    for i in range(n):
        t = i / (n - 1)
        # spiral curve in XY, slight Z rise then fall
        ang = t * math.radians(200)  # ~200 deg curve
        r = 1.4 - 0.15 * t
        x = r * math.sin(ang)
        y = -r * math.cos(ang) + 1.4
        z = 0.35 + 0.9 * math.sin(t * math.pi) * 0.35 + t * 0.15
        rad = 0.22 * (1.0 - 0.72 * t) + 0.03
        bpy.ops.mesh.primitive_uv_sphere_add(segments=14, ring_count=8, radius=rad, location=(x, y, z))
        seg = bpy.context.active_object
        seg.scale = (1.0, 1.0, 0.95)
        assign(seg, iv if t < 0.85 else tip_m)

    # basal pulp cavity hint (darker open end)
    ang0 = 0
    r0 = 1.4
    x0 = r0 * math.sin(ang0)
    y0 = -r0 * math.cos(ang0) + 1.4
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.16, depth=0.12,
                                       location=(x0 - 0.05, y0, 0.4),
                                       rotation=(math.radians(90), 0, 0))
    base = bpy.context.active_object
    assign(base, flat((0.55, 0.45, 0.35), rough=0.55))

    # growth-line rings near base
    for k in range(3):
        t = 0.08 + k * 0.06
        ang = t * math.radians(200)
        r = 1.4 - 0.15 * t
        x = r * math.sin(ang)
        y = -r * math.cos(ang) + 1.4
        z = 0.35 + 0.9 * math.sin(t * math.pi) * 0.35 + t * 0.15
        rad = 0.22 * (1.0 - 0.72 * t) + 0.04
        bpy.ops.mesh.primitive_torus_add(major_radius=rad, minor_radius=0.012,
                                        major_segments=20, minor_segments=6,
                                        location=(x, y, z))
        assign(bpy.context.active_object, ivory((0.8, 0.72, 0.55), (0.65, 0.55, 0.4)))

    join_all('mammoth_tusk')
    bake_and_export('mammoth_tusk')
    fourview('mammoth_tusk', ortho_scale=3.6, look_z=0.7)
    print('DONE_mammoth_tusk')


def build_dig_brush():
    """Paleontology brush: wood handle + ferrule + soft bristles."""
    reset()
    w = wood((0.5, 0.32, 0.16))
    ferrule = steel((0.62, 0.58, 0.48))
    bristle = flat((0.55, 0.42, 0.28), rough=0.75)
    bristle_tip = flat((0.72, 0.62, 0.48), rough=0.8)

    # handle
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.12, depth=1.5, location=(0, 0, 1.0))
    h = bpy.context.active_object
    # slight taper
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(h.data)
    for v in bm.verts:
        t = (v.co.z + 0.75) / 1.5
        mul = 0.85 + 0.2 * t
        v.co.x *= mul
        v.co.y *= mul
    bmesh.update_edit_mesh(h.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    bev(h, 0.015, 2)
    assign(h, w)

    # handle end knob
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.13, location=(0, 0, 0.22))
    assign(bpy.context.active_object, w)

    # metal ferrule
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.145, depth=0.28, location=(0, 0, 1.82))
    assign(bpy.context.active_object, ferrule)
    # ferrule rings
    for z in (1.72, 1.92):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.148, minor_radius=0.012,
                                        major_segments=24, minor_segments=8, location=(0, 0, z))
        assign(bpy.context.active_object, steel((0.45, 0.42, 0.35)))

    # bristle bundle — many thin cylinders
    import random
    rng = random.Random(42)
    for i in range(48):
        ax = rng.uniform(-0.1, 0.1)
        ay = rng.uniform(-0.1, 0.1)
        # keep in circle
        if ax * ax + ay * ay > 0.01:
            nrm = math.sqrt(ax * ax + ay * ay)
            ax *= 0.095 / nrm
            ay *= 0.095 / nrm
        length = rng.uniform(0.55, 0.85)
        tip_z = 2.0 + length
        lean = rng.uniform(-0.08, 0.08)
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=5, radius=rng.uniform(0.008, 0.014), depth=length,
            location=(ax + lean * 0.3, ay, 2.0 + length / 2),
            rotation=(lean, lean * 0.5, rng.uniform(0, 6)))
        assign(bpy.context.active_object, bristle if i % 3 else bristle_tip)

    # bristle base plug
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.12, depth=0.08, location=(0, 0, 1.98))
    assign(bpy.context.active_object, flat((0.35, 0.28, 0.2), rough=0.6))

    join_all('dig_brush')
    bake_and_export('dig_brush')
    fourview('dig_brush', ortho_scale=3.4, look_z=1.4)
    print('DONE_dig_brush')


def build_dig_chisel():
    """Rock hammer / chisel pick: steel dual head + wood handle."""
    reset()
    w = wood((0.42, 0.26, 0.12))
    st = steel((0.5, 0.52, 0.55))
    st_dark = steel((0.35, 0.36, 0.38))

    # wood handle
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.11, depth=1.6, location=(0, 0, 0.95))
    h = bpy.context.active_object
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(h.data)
    for v in bm.verts:
        t = (v.co.z + 0.8) / 1.6
        mul = 0.9 + 0.15 * math.sin(t * math.pi)
        v.co.x *= mul
        v.co.y *= mul
    bmesh.update_edit_mesh(h.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    bev(h, 0.015, 2)
    assign(h, w)

    # butt end
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.12, location=(0, 0, 0.12))
    assign(bpy.context.active_object, w)

    # head eye / socket around top of handle
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 1.85))
    socket = bpy.context.active_object
    socket.scale = (0.22, 0.28, 0.22)
    bev(socket, 0.03, 2)
    assign(socket, st)

    # chisel / flat face (one side)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.45, 0, 1.85))
    ch = bpy.context.active_object
    ch.scale = (0.4, 0.12, 0.2)
    # taper to blade
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(ch.data)
    for v in bm.verts:
        if v.co.x > 0.3:
            v.co.z *= 0.35
            v.co.y *= 0.5
    bmesh.update_edit_mesh(ch.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    bev(ch, 0.02, 2)
    assign(ch, st)

    # pick point (other side)
    bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=0.14, radius2=0.01, depth=0.7,
                                   location=(-0.5, 0, 1.85), rotation=(0, math.radians(-90), 0))
    pick = bpy.context.active_object
    pick.scale = (0.7, 1.0, 1.0)
    assign(pick, st_dark)

    # cheek plates
    for s in (-1, 1):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, s * 0.2, 1.85))
        ck = bpy.context.active_object
        ck.scale = (0.28, 0.04, 0.18)
        bev(ck, 0.015, 2)
        assign(ck, st_dark)

    # wedge pin
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 2.05))
    pin = bpy.context.active_object
    pin.scale = (0.08, 0.1, 0.06)
    assign(pin, wood((0.55, 0.35, 0.18)))

    join_all('dig_chisel')
    bake_and_export('dig_chisel')
    fourview('dig_chisel', ortho_scale=3.4, look_z=1.1)
    print('DONE_dig_chisel')


def main():
    builders = {
        'trex_skull': build_trex_skull,
        'triceratops_skull': build_triceratops_skull,
        'stego_plate': build_stego_plate,
        'ammonite': build_ammonite,
        'trilobite': build_trilobite,
        'mammoth_tusk': build_mammoth_tusk,
        'dig_brush': build_dig_brush,
        'dig_chisel': build_dig_chisel,
    }
    only = os.environ.get('ONLY')
    names = [only] if only in builders else list(builders.keys())
    for n in names:
        print('BUILD', n)
        builders[n]()
    print('ALL_DONE')


if __name__ == '__main__':
    main()
