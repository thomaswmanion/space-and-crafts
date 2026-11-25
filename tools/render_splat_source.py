import bpy, math
from mathutils import Vector
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Commissioned\Hero\Splat_Hero.glb')
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
mesh.location.z -= mesh.dimensions.z*0.5
bpy.context.scene.render.engine='BLENDER_EEVEE'
bpy.context.scene.render.resolution_x=960; bpy.context.scene.render.resolution_y=960; bpy.context.scene.render.resolution_percentage=100
bpy.context.scene.render.image_settings.file_format='PNG'
bpy.context.scene.render.film_transparent=False
bpy.context.scene.world.color=(0.16,0.18,0.24)
camd=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camd); bpy.context.collection.objects.link(cam)
cam.location=(0,-4,0); cam.rotation_euler=(math.radians(90),0,0); camd.type='ORTHO'; camd.ortho_scale=1.55; bpy.context.scene.camera=cam
for name,loc,power,size in [('Key',(2,-3,3),800,3),('Fill',(-2,-2,1.5),450,3),('Rim',(0,2,2),600,2)]:
    d=bpy.data.lights.new(name,'AREA'); o=bpy.data.objects.new(name,d); bpy.context.collection.objects.link(o); o.location=loc; d.energy=power; d.shape='DISK'; d.size=size; o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.context.scene.render.filepath=r'E:\Users\thoma\git\space-and-crafts-3\Logs\splathero-glb-blender-preview.png'
bpy.ops.render.render(write_still=True)
print('SAVED',bpy.context.scene.render.filepath,'DIMENSIONS',tuple(mesh.dimensions))

