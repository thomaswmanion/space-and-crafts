import bpy, math, os
from mathutils import Vector
src=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Enemies\Pulpal\Meshy_Iter1994\Pulpal_Rigged.fbx'
out=r'E:\Users\thoma\git\space-and-crafts-3\Logs\score-comparison\pulpal-meshy-animation-review-iter2180\authored-ik-trial-iter2184\Pulpal_LeafLanePress_IK_Iter2184.fbx'
os.makedirs(os.path.dirname(out),exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=src,use_anim=False)
scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=90;scene.render.fps=30
arm=next(o for o in scene.objects if o.type=='ARMATURE');arm.animation_data_create()
# world-space rest controls, preserving both sole anchors throughout the motion
rest={}
for side in ('Left','Right'):
 foot=arm.pose.bones[side+'Foot']; hand=arm.pose.bones[side+'Hand']
 rest[side+'Foot']=arm.matrix_world@foot.tail
 rest[side+'Hand']=arm.matrix_world@hand.tail

def empty(name,loc):
 o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);o.empty_display_type='SPHERE';o.empty_display_size=.04;o.hide_render=True;o.location=loc;return o
controls={}
for side in ('Left','Right'):
 polex=.22 if side=='Left' else -.22
 controls[side+'Foot']=empty('IK_'+side+'FootTarget',rest[side+'Foot'])
 controls[side+'KneePole']=empty('IK_'+side+'KneePole',(polex,-.42,.38))
 controls[side+'Hand']=empty('IK_'+side+'HandTarget',rest[side+'Hand'])
 controls[side+'ElbowPole']=empty('IK_'+side+'ElbowPole',(polex*2.3,-.68,.95))
# Add IK constraints to distal bones; feet use a three-bone chain and are pinned to world-space targets.
for side in ('Left','Right'):
 for end,target,pole,chain in ((side+'Foot',side+'Foot',side+'KneePole',3),(side+'Hand',side+'Hand',side+'ElbowPole',3)):
  pb=arm.pose.bones[end];c=pb.constraints.new('IK');c.name='ReviewOnly_'+end+'_IK';c.target=controls[target];c.pole_target=controls[pole];c.chain_count=chain;c.iterations=128;c.use_stretch=False;c.pole_angle=0.0
# Motion controls in world coordinates. The long crouch lowers the torso enough for the arms to reach forward while foot IK holds the boots.
poses={
 1:{'root':0.0,'handL':rest['LeftHand'],'handR':rest['RightHand']},
 14:{'root':0.0,'handL':(.58,-.26,1.24),'handR':(-.58,-.26,1.24)},
 24:{'root':0.0,'handL':(.46,-.34,1.34),'handR':(-.46,-.34,1.34)},
 31:{'root':-.62,'handL':(.24,-.30,.12),'handR':(-.24,-.30,.12)},
 38:{'root':-.62,'handL':(.24,-.30,.12),'handR':(-.24,-.30,.12)},
 48:{'root':-.34,'handL':(.23,-.26,.36),'handR':(-.30,-.18,.70)},
 62:{'root':0.0,'handL':rest['LeftHand'],'handR':rest['RightHand']},
 90:{'root':0.0,'handL':rest['LeftHand'],'handR':rest['RightHand']}}
for frame,v in poses.items():
 scene.frame_set(frame);arm.location.z=v['root'];arm.keyframe_insert(data_path='location',frame=frame,group='Root')
 for side,key in (('Left','handL'),('Right','handR')):
  controls[side+'Hand'].location=v[key];controls[side+'Hand'].keyframe_insert(data_path='location',frame=frame,group=side+'HandTarget')
 for side in ('Left','Right'):
  controls[side+'Foot'].keyframe_insert(data_path='location',frame=frame,group=side+'FootTarget')
# Bake the IK solution into bone transforms at every frame, then remove constraints and control empties.
scene.frame_set(1);bpy.context.view_layer.update();bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.object.mode_set(mode='POSE')
bpy.ops.nla.bake(frame_start=1,frame_end=90,step=1,only_selected=False,visual_keying=True,clear_constraints=True,clear_parents=False,use_current_action=True,bake_types={'POSE'})
bpy.ops.object.mode_set(mode='OBJECT')
for o in list(controls.values()): bpy.data.objects.remove(o,do_unlink=True)
# Save review project and export the baked FBX outside Assets; never touch Meshy originals.
blend=out.replace('.fbx','.blend');bpy.ops.wm.save_as_mainfile(filepath=blend)
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects:
 if o.type in ('ARMATURE','MESH'):o.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=out,use_selection=True,object_types={'ARMATURE','MESH'},bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,add_leaf_bones=False,apply_unit_scale=True,axis_forward='-Z',axis_up='Y')
print('IK_AUTHORED',out,'bones=',len(arm.data.bones),'frames=90','root_drop=-0.62m','foot_targets=world-pinned')
