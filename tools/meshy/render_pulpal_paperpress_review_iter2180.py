import bpy, os, math, sys
from mathutils import Vector
ARGS=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
fbx=ARGS[0] if ARGS else r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Enemies\Pulpal\Meshy_Iter1994\Pulpal_PaperPressAttack.fbx'
action_match=ARGS[1] if len(ARGS)>1 else 'retarget_clip'
out=ARGS[2] if len(ARGS)>2 else r'E:\Users\thoma\git\space-and-crafts-3\Logs\score-comparison\pulpal-meshy-animation-review-iter2180\custom-paper-press-frames-zup'
os.makedirs(out,exist_ok=True)
if fbx.lower().endswith('.blend'):
    bpy.ops.wm.open_mainfile(filepath=fbx)
else:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx,use_anim=True)
scene=bpy.context.scene
arm=next(o for o in scene.objects if o.type=='ARMATURE')
mesh=next(o for o in scene.objects if o.type=='MESH')
a=next(a for a in bpy.data.actions if action_match.lower() in a.name.lower())
arm.animation_data_create(); arm.animation_data.action=a
if getattr(a,'slots',None): arm.animation_data.action_slot=a.slots[0]
# measure union bounds for stable framing
coords=[]
for t in (0,.2,.4,.6,.8):
 f0,f1=a.frame_range; scene.frame_set(round(f0+(f1-f0)*t)); bpy.context.view_layer.update()
 ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get()); evmesh=ev.to_mesh(); wm=ev.matrix_world.copy(); coords.extend([wm@v.co for v in evmesh.vertices]); ev.to_mesh_clear()
lo=Vector((min(p.x for p in coords),min(p.y for p in coords),min(p.z for p in coords))); hi=Vector((max(p.x for p in coords),max(p.y for p in coords),max(p.z for p in coords))); center=(lo+hi)/2
camd=bpy.data.cameras.new('ReviewCam'); cam=bpy.data.objects.new('ReviewCam',camd); scene.collection.objects.link(cam); scene.camera=cam
camd.type='ORTHO'; camd.ortho_scale=max(hi.z-lo.z,hi.x-lo.x)*1.65; cam.location=center+Vector((0,-5,.05)); cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Z').to_euler(); camd.lens=50
world=bpy.data.worlds.new('ReviewWorld'); scene.world=world; world.use_nodes=True; world.node_tree.nodes['Background'].inputs['Color'].default_value=(.11,.14,.19,1); world.node_tree.nodes['Background'].inputs['Strength'].default_value=.8
for name,loc,power,color in [('Key',(3,4,4),600,(1,.84,.69)),('Fill',(-3,2,3),360,(.66,.8,1)),('Rim',(0,3,-3),480,(.87,.92,1))]:
 ld=bpy.data.lights.new(name,'AREA'); lo_obj=bpy.data.objects.new(name,ld); scene.collection.objects.link(lo_obj); lo_obj.location=center+Vector(loc); ld.energy=power; ld.shape='DISK'; ld.size=4; lo_obj.rotation_euler=(center-lo_obj.location).to_track_quat('-Z','Y').to_euler()
# Neutral grounded plane at the lowest Idle foot-weighted point, estimated in audit
plane=bpy.data.meshes.new('ReferenceFloorMesh'); plane.from_pydata([(-4,-4,.0106),(4,-4,.0106),(4,4,.0106),(-4,4,.0106)],[],[(0,1,2,3)]); plane.update(); po=bpy.data.objects.new('IdleFootProxyReferencePlane_ZUp',plane); scene.collection.objects.link(po)
mat=bpy.data.materials.new('matte slate'); mat.diffuse_color=(.18,.21,.25,1); po.data.materials.append(mat)
scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=512; scene.render.resolution_y=512; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False
scene.camera.data.lens=50
for i,t in enumerate((0,.2,.4,.6,.8)):
 f0,f1=a.frame_range; scene.frame_set(round(f0+(f1-f0)*t)); bpy.context.view_layer.update(); scene.render.filepath=os.path.join(out,f'paperpress_t{t:.1f}.png'); bpy.ops.render.render(write_still=True)
print('RENDERED',out,'samples=5','action=',a.name)

