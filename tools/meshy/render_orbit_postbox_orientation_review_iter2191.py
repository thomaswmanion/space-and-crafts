import bpy, math, os, sys
from mathutils import Vector

def arg_after(flag):
    args = sys.argv
    return args[args.index(flag) + 1]

source = os.path.abspath(arg_after("--source"))
output = os.path.abspath(arg_after("--output"))
os.makedirs(output, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=source)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
if not meshes:
    raise RuntimeError("No mesh in source GLB")
mins = Vector((1e9, 1e9, 1e9)); maxs = Vector((-1e9, -1e9, -1e9))
for obj in meshes:
    for corner in obj.bound_box:
        co = obj.matrix_world @ Vector(corner)
        mins.x=min(mins.x,co.x); mins.y=min(mins.y,co.y); mins.z=min(mins.z,co.z)
        maxs.x=max(maxs.x,co.x); maxs.y=max(maxs.y,co.y); maxs.z=max(maxs.z,co.z)
center=(mins+maxs)*0.5
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='Standard'
scene.view_settings.look='Medium High Contrast'
scene.world=bpy.data.worlds.new('ReviewWorld')
scene.world.color=(0.22,0.22,0.22)

def area(name, loc, power, size):
    data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=loc
    obj.rotation_euler=(Vector(center)-obj.location).to_track_quat('-Z','Y').to_euler()
    return obj
area('Key',(2.5,-3.5,4.0),650,4.0)
area('Fill',(-3.0,-1.5,2.0),350,3.0)
area('Rim',(0.5,3.0,3.5),500,3.0)
camdata=bpy.data.cameras.new('ReviewCamera'); camdata.type='ORTHO'; camdata.ortho_scale=max(maxs.x-mins.x,maxs.y-mins.y,maxs.z-mins.z)*1.45
cam=bpy.data.objects.new('ReviewCamera',camdata); scene.collection.objects.link(cam); scene.camera=cam
views=[('front_minusY',(0,-1,0)),('back_plusY',(0,1,0)),('side_plusX',(1,0,0)),('side_minusX',(-1,0,0))]
for name,direction in views:
    cam.location=center+Vector(direction)*5
    cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Z').to_euler()
    scene.render.filepath=os.path.join(output,name+'.png')
    bpy.ops.render.render(write_still=True)
    print('RENDERED_VIEW='+name)
