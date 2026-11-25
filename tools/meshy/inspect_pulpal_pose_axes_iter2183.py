import bpy
from mathutils import Vector
p=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Enemies\Pulpal\Meshy_Iter1994\Pulpal_Rigged.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=p);a=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
for n in ['LeftArm','RightArm','LeftForeArm','RightForeArm','Spine','Spine01','Spine02','Hips','LeftUpLeg','LeftLeg','RightUpLeg','RightLeg']:
 pb=a.pose.bones[n]; m=pb.matrix.to_3x3();print(n,'head=',tuple(round(v,3) for v in pb.head),'tail=',tuple(round(v,3) for v in pb.tail),'basis=',[tuple(round(v,3) for v in m.col[i]) for i in range(3)],'rotation_mode=',pb.rotation_mode,'rot=',tuple(round(v,3) for v in pb.rotation_quaternion))
