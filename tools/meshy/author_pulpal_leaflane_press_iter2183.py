import bpy, os
from mathutils import Quaternion
src=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Enemies\Pulpal\Meshy_Iter1994\Pulpal_Rigged.fbx'
out=r'E:\Users\thoma\git\space-and-crafts-3\Logs\score-comparison\pulpal-meshy-animation-review-iter2180\authored-trial-iter2183-rejected\Pulpal_LeafLanePress_Authored_Iter2183.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=src,use_anim=False)
scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE');arm.animation_data_create()
scene.render.fps=30;scene.frame_start=1;scene.frame_end=90
act=bpy.data.actions.new('Pulpal_LeafLanePress_Authored_Iter2183')
arm.animation_data.action=act
names=('LeftArm','RightArm','LeftForeArm','RightForeArm','Spine','Spine01','Spine02')
for name in names: arm.pose.bones[name].rotation_mode='QUATERNION'
# Each pose defines rotations in local bone space. The action deliberately uses no scale curves or root translation.
poses={
 1:{},
 14:{'LeftArm':.42,'RightArm':.42,'LeftForeArm':.10,'RightForeArm':.10,'Spine':-.02,'Spine01':-.02,'Spine02':-.02},
 24:{'LeftArm':.92,'RightArm':.92,'LeftForeArm':.22,'RightForeArm':.22,'Spine':-.04,'Spine01':-.04,'Spine02':-.04},
 31:{'LeftArm':.10,'RightArm':.10,'LeftForeArm':-.68,'RightForeArm':-.68,'Spine':.05,'Spine01':.05,'Spine02':.05},
 37:{'LeftArm':.08,'RightArm':.08,'LeftForeArm':-.74,'RightForeArm':-.74,'Spine':.06,'Spine01':.06,'Spine02':.06},
 48:{'LeftArm':.40,'RightArm':.06,'LeftForeArm':-.42,'RightForeArm':.10,'Spine':.02,'Spine01':.02,'Spine02':.02},
 62:{},90:{}}
for frame,values in poses.items():
 scene.frame_set(frame)
 for name in names:
  pb=arm.pose.bones[name]
  pb.rotation_quaternion=Quaternion((1,0,0),values.get(name,0.0))
  pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=name)
for fc in act.fcurves if hasattr(act,'fcurves') else []:
 for kp in fc.keyframe_points: kp.interpolation='BEZIER'
# Candid metadata lives on the animation itself; no production material/scene is altered.
blend=out.replace('.fbx','.blend');bpy.ops.wm.save_as_mainfile(filepath=blend)
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.context.scene.objects:
 if obj.type in ('ARMATURE','MESH'): obj.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=out,use_selection=True,object_types={'ARMATURE','MESH'},bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,add_leaf_bones=False,apply_unit_scale=True,axis_forward='-Z',axis_up='Y')
print('AUTHORED',out,'clip=',act.name,'keys=',sorted(poses),'bones=',len(arm.data.bones))

