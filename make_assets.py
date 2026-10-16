import bpy, bmesh, sys, os, math
out = sys.argv[sys.argv.index('--')+1]
R=1024
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.samples=4; sc.cycles.device='CPU'
def mat(name, build):
    m=bpy.data.materials.new(name); m.use_nodes=True; nt=m.node_tree; b=nt.nodes['Principled BSDF']; build(nt,b); return m
def col(c): return (c[0],c[1],c[2],1)
def flat(c):
    return mat('m',lambda nt,b: b.inputs['Base Color'].__setattr__('default_value',col(c)))
def noise_mix(c1,c2,scale=5,det=4,kind='noise',ramp=(0.4,0.6),coord='Object'):
    def f(nt,b):
        tc=nt.nodes.new('ShaderNodeTexCoord')
        if kind=='noise': t=nt.nodes.new('ShaderNodeTexNoise'); t.inputs['Detail'].default_value=det
        elif kind=='wave': t=nt.nodes.new('ShaderNodeTexWave'); t.inputs['Distortion'].default_value=6; t.inputs['Detail'].default_value=det
        elif kind=='voronoi': t=nt.nodes.new('ShaderNodeTexVoronoi')
        elif kind=='checker': t=nt.nodes.new('ShaderNodeTexChecker'); t.inputs['Color1'].default_value=col(c1); t.inputs['Color2'].default_value=col(c2)
        t.inputs['Scale'].default_value=scale; nt.links.new(tc.outputs[coord],t.inputs['Vector'])
        if kind=='checker': nt.links.new(t.outputs['Color'],b.inputs['Base Color']); return
        r=nt.nodes.new('ShaderNodeValToRGB'); e=r.color_ramp.elements; e[0].position,e[1].position=ramp; e[0].color=col(c1); e[1].color=col(c2)
        nt.links.new(t.outputs[0 if kind!='voronoi' else 'Distance'],r.inputs['Fac']); nt.links.new(r.outputs['Color'],b.inputs['Base Color'])
    return mat('n',f)
def assign(o,m): o.data.materials.clear(); o.data.materials.append(m)
def bev(o,w=0.03,s=3):
    m=o.modifiers.new('b','BEVEL'); m.width=w; m.segments=s; m.limit_method='ANGLE'
def finish(name):
    objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for o in objs:
        bpy.context.view_layer.objects.active=o
        for md in list(o.modifiers): bpy.ops.object.modifier_apply(modifier=md.name)
    bpy.ops.object.select_all(action='SELECT'); bpy.context.view_layer.objects.active=objs[0]; bpy.ops.object.join()
    o=bpy.context.active_object; bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.object.shade_auto_smooth(angle=math.radians(40)) if hasattr(bpy.ops.object,'shade_auto_smooth') else bpy.ops.object.shade_smooth()
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.01); bpy.ops.object.mode_set(mode='OBJECT')
    img=bpy.data.images.new(name+'_base',R,R); 
    for m in o.data.materials:
        n=m.node_tree.nodes.new('ShaderNodeTexImage'); n.image=img; m.node_tree.nodes.active=n
    sc=bpy.context.scene; sc.render.bake.use_pass_direct=False; sc.render.bake.use_pass_indirect=False; sc.render.bake.margin=8
    bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'})
    img.filepath_raw=os.path.join(out,name+'_base.png'); img.file_format='PNG'; img.save()
    bpy.ops.export_scene.fbx(filepath=os.path.join(out,name+'.fbx'), use_selection=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', bake_space_transform=True)
WHITE=(0.95,0.95,0.92); BLUE=(0.12,0.3,0.7)
# 1 plate: dish + rim torus + painted ring pattern
reset()
bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=1.35,depth=0.12); p=bpy.context.active_object
bpy.ops.object.mode_set(mode='EDIT'); bm=bmesh.from_edit_mesh(p.data)
for v in bm.verts:
    if v.co.z<0: v.co.x*=0.62; v.co.y*=0.62
bmesh.update_edit_mesh(p.data); bpy.ops.object.mode_set(mode='OBJECT'); bev(p,0.03)
assign(p,noise_mix(WHITE,BLUE,scale=9,kind='wave',ramp=(0.48,0.52),coord='Object'))
bpy.ops.mesh.primitive_torus_add(major_radius=1.35,minor_radius=0.06,major_segments=96,minor_segments=12,location=(0,0,0.06)); assign(bpy.context.active_object,flat(BLUE))
bpy.ops.mesh.primitive_torus_add(major_radius=0.8,minor_radius=0.015,major_segments=96,minor_segments=8,location=(0,0,0.06)); assign(bpy.context.active_object,flat((0.85,0.65,0.15)))
finish('plate')
# 2 crate: planks + frame + nails
reset(); W=(0.78,0.52,0.28); D=(0.45,0.27,0.12)
wood=noise_mix(W,D,scale=3,det=6,kind='wave',ramp=(0.3,0.8)); dark=noise_mix(D,(0.3,0.17,0.07),scale=4,kind='wave')
for side in range(4):
    a=side*math.pi/2
    for i in range(4):
        z=-0.6+i*0.4
        bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.active_object; o.scale=(1.5,0.08,0.36)
        o.rotation_euler=(0,0,a); o.location=(math.sin(a)*0.75,-math.cos(a)*0.75,z+0.2-0.2); bev(o,0.025); assign(o,wood)
for sx in (-1,1):
    for sy in (-1,1):
        bpy.ops.mesh.primitive_cube_add(size=1,location=(sx*0.76,sy*0.76,0)); o=bpy.context.active_object; o.scale=(0.16,0.16,1.62); bev(o,0.03); assign(o,dark)
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,0.78)); o=bpy.context.active_object; o.scale=(1.5,1.5,0.08); bev(o,0.02); assign(o,wood)
for side in range(4):
    a=side*math.pi/2
    for sx in (-0.6,0.6):
        for z in (-0.6,-0.2,0.2,0.6):
            x=sx*math.cos(a)+math.sin(a)*0.8; y=sx*math.sin(a)-math.cos(a)*0.8
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.035,segments=10,ring_count=6,location=(x,y,z)); assign(bpy.context.active_object,flat((0.55,0.55,0.6)))
finish('crate')
# 3 ball: soccer-ish panels via voronoi + seams
reset(); bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=5,radius=1); b=bpy.context.active_object
assign(b,noise_mix((0.95,0.95,0.95),(0.9,0.2,0.25),scale=2.2,kind='voronoi',ramp=(0.35,0.37)))
bpy.ops.object.shade_smooth(); finish('ball')
# 4 vase: lathe body + handles + foot + glaze bands
reset(); bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=1,depth=2.2,end_fill_type='NGON'); v=bpy.context.active_object
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.subdivide(number_cuts=24); bm=bmesh.from_edit_mesh(v.data)
for vt in bm.verts:
    t=(vt.co.z+1.1)/2.2; r=0.28+0.52*math.sin(min(t,0.92)*math.pi*1.05)+(0.9*(t-0.86) if t>0.86 else 0)
    l=math.hypot(vt.co.x,vt.co.y)
    if l>1e-4: vt.co.x*=r/l; vt.co.y*=r/l
bmesh.update_edit_mesh(v.data); bpy.ops.object.mode_set(mode='OBJECT')
def vasemat(nt,b):
    tc=nt.nodes.new('ShaderNodeTexCoord'); sep=nt.nodes.new('ShaderNodeSeparateXYZ'); nt.links.new(tc.outputs['Object'],sep.inputs[0])
    w=nt.nodes.new('ShaderNodeTexWave'); w.wave_type='BANDS'; w.bands_direction='Z'; w.inputs['Scale'].default_value=1.4; w.inputs['Distortion'].default_value=1.5; nt.links.new(tc.outputs['Object'],w.inputs['Vector'])
    r=nt.nodes.new('ShaderNodeValToRGB'); e=r.color_ramp.elements; e[0].color=col((0.08,0.45,0.55)); e[1].color=col((0.95,0.85,0.55)); e.new(0.5).color=col((0.85,0.4,0.2)); e[0].position=0.2;e[1].position=0.85
    nt.links.new(w.outputs[0],r.inputs[0]); nt.links.new(r.outputs[0],b.inputs['Base Color'])
assign(v,mat('vase',vasemat))
for s in (-1,1):
    bpy.ops.mesh.primitive_torus_add(major_radius=0.3,minor_radius=0.06,location=(s*0.62,0,0.65),rotation=(math.pi/2,0,0)); assign(bpy.context.active_object,flat((0.9,0.75,0.3)))
bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=0.45,depth=0.12,location=(0,0,-1.1)); bev(bpy.context.active_object,0.03); assign(bpy.context.active_object,flat((0.08,0.35,0.45)))
bpy.ops.mesh.primitive_torus_add(major_radius=0.55,minor_radius=0.04,location=(0,0,1.1)); assign(bpy.context.active_object,flat((0.9,0.75,0.3)))
finish('vase')
# 5 duck: body, head, beak, eyes, wings, tail
reset(); Y=(1,0.8,0.12)
bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=24,radius=1); o=bpy.context.active_object; o.scale=(1.2,1,0.82); assign(o,noise_mix(Y,(1,0.68,0.05),scale=3,kind='noise'))
bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,radius=0.58,location=(0.78,0,0.85)); assign(bpy.context.active_object,flat(Y))
bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,radius=0.25,location=(1.3,0,0.72)); o=bpy.context.active_object; o.scale=(1.3,1,0.45); assign(o,flat((1,0.45,0.05)))
for s in (-1,1):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=0.11,location=(1.18,s*0.25,1.0)); assign(bpy.context.active_object,flat((0.02,0.02,0.03)))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=0.04,location=(1.27,s*0.27,1.05)); assign(bpy.context.active_object,flat((1,1,1)))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,radius=0.5,location=(-0.1,s*0.85,0.15)); o=bpy.context.active_object; o.scale=(1.1,0.3,0.6); assign(o,flat((1,0.7,0.08)))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=0.1,location=(1.05,s*0.38,0.75)); o=bpy.context.active_object; o.scale=(1,0.4,0.7); assign(o,flat((1,0.45,0.45)))
bpy.ops.mesh.primitive_cone_add(radius1=0.3,depth=0.5,location=(-1.25,0,0.45),rotation=(0,-2.2,0)); assign(bpy.context.active_object,flat(Y))
finish('duck')
