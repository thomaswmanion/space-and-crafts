import bpy
from mathutils import Vector
p=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Enemies\Pulpal\Meshy_Iter1994\Pulpal_Rigged.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=p);a=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
print('ARMATURE world loc/rot/scale',tuple(a.matrix_world.translation),tuple(a.rotation_euler),tuple(a.scale))
for n in ('Hips','LeftShoulder','LeftArm','LeftForeArm','LeftHand','LeftUpLeg','LeftLeg','LeftFoot','RightShoulder','RightArm','RightForeArm','RightHand','RightUpLeg','RightLeg','RightFoot'):
 pb=a.pose.bones[n];h=a.matrix_world@pb.head;t=a.matrix_world@pb.tail;print(n,'head',tuple(round(v,3) for v in h),'tail',tuple(round(v,3) for v in t))
