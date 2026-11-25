import bpy, json, os, shutil
from collections import defaultdict
from mathutils import Vector
ROOT=r'E:\Users\thoma\git\space-and-crafts-3'
IN_BLEND=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1370\Splat_Meshy_Iter1365_RigBound_Iter1370.blend'
OUT_DIR=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1372'
os.makedirs(OUT_DIR,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=IN_BLEND)
scene=bpy.context.scene
arm=next(o for o in scene.objects if o.type=='ARMATURE')
meshobj=next(o for o in scene.objects if o.type=='MESH' and 'RigBound' in o.name)
action=arm.animation_data.action
scene.frame_start=int(action.frame_range[0]);scene.frame_end=int(action.frame_range[1])
# Identify representative sole vertices from the actual bound pose and weights.
arm.animation_data.action=None
arm.data.pose_position='REST'
deps=bpy.context.evaluated_depsgraph_get(); ev=meshobj.evaluated_get(deps); rest=ev.to_mesh()
group_index={g.index:g.name for g in meshobj.vertex_groups}
sole_indices={}
for side,sign in [('Left',1),('Right',-1)]:
    names={side+'Foot',side+'ToeBase'}
    ids=[]
    for v in rest.vertices:
        p=meshobj.matrix_world@v.co
        weight=sum(item.weight for item in v.groups if group_index.get(item.group) in names)
        if weight>0.10 and p.z<0.24 and p.x*sign>0.05:
            ids.append(v.index)
    if not ids:
        raise RuntimeError(f'No {side} sole vertices found; refusing to edit walk clip.')
    sole_indices[side]=ids
    print('SOLE_SET',side,'count',len(ids))
ev.to_mesh_clear()
arm.animation_data.action=action
arm.data.pose_position='POSE'
hips=arm.pose.bones.get('Hips')
if hips is None:
    raise RuntimeError('Hips bone unavailable; refusing to edit walk clip.')
# Measure the current lower planted foot on each keyed pose.
frame_metrics={}
for frame in range(scene.frame_start,scene.frame_end+1):
    scene.frame_set(frame)
    deps=bpy.context.evaluated_depsgraph_get(); ev=meshobj.evaluated_get(deps); me=ev.to_mesh()
    points=[meshobj.matrix_world@v.co for v in me.vertices]
    feet={side:min(points[i].z for i in ids) for side,ids in sole_indices.items()}
    difference=abs(feet['Left']-feet['Right'])
    contact=(feet['Left']+feet['Right'])*0.5 if difference<=0.025 else min(feet.values())
    frame_metrics[frame]={'left_before':feet['Left'],'right_before':feet['Right'],'contact_before':contact}
    ev.to_mesh_clear()
# Key the Hips' vertical offset at every source frame so the lower planted sole
# reaches z=0. In double-support frames average the two near-ground soles.
slot=next(iter(action.slots))
channelbag=action.layers[0].strips[0].channelbag(slot)
hips_path='pose.bones["Hips"].location'
# Bone-local Y follows the Hips bone's longitudinal axis here. Derive which
# local translation channel moves most directly along world up from the live
# pose basis instead of assuming local Z is vertical.
basis_world=arm.matrix_world @ hips.matrix
vertical_axis=max(range(3),key=lambda axis:abs(basis_world.to_3x3().col[axis].z))
world_per_pose_unit=abs(basis_world.to_3x3().col[vertical_axis].z)
hips_vertical=next((fc for fc in channelbag.fcurves if fc.data_path==hips_path and fc.array_index==vertical_axis),None)
if hips_vertical is None:
    raise RuntimeError('Hips vertical location curve missing; refusing to edit walk clip.')
if world_per_pose_unit<=0.005:
    raise RuntimeError('Hips local basis has no stable world-up axis; refusing to edit walk clip.')
for frame,values in frame_metrics.items():
    key=next((k for k in hips_vertical.keyframe_points if abs(k.co.x-frame)<1e-4),None)
    if key is None:
        raise RuntimeError(f'Missing Hips key at source frame {frame}.')
    key.co.y -= values['contact_before']/world_per_pose_unit
    key.interpolation='LINEAR'
hips_vertical.update()
action.name='Splat_Walk_Grounded_Iter1372'
# Verify every keyed frame against the same weighted sole set.
max_residual=0.0
max_left=0.0
max_right=0.0
for frame in range(scene.frame_start,scene.frame_end+1):
    scene.frame_set(frame)
    deps=bpy.context.evaluated_depsgraph_get(); ev=meshobj.evaluated_get(deps); me=ev.to_mesh()
    points=[meshobj.matrix_world@v.co for v in me.vertices]
    feet={side:min(points[i].z for i in ids) for side,ids in sole_indices.items()}
    frame_metrics[frame].update({'left_after':feet['Left'],'right_after':feet['Right'],'min_after':min(feet.values())})
    max_left=max(max_left,abs(feet['Left']))
    max_right=max(max_right,abs(feet['Right']))
    # A raised swing foot is allowed. Grounding check is for the lower foot.
    max_residual=max(max_residual,abs(min(feet.values())))
    ev.to_mesh_clear()
report={'iteration':1372,'candidate_mesh':meshobj.name,'walk_action':action.name,'frame_range':[scene.frame_start,scene.frame_end],'rest_pose_sole_vertices':{k:len(v) for k,v in sole_indices.items()},'world_up_Hips_location_axis':vertical_axis,'world_per_Hips_location_unit':world_per_pose_unit,'max_lower_sole_residual_m':max_residual,'max_left_sole_abs_height_m':max_left,'max_right_sole_abs_height_m':max_right,'frames':{str(k):v for k,v in frame_metrics.items()},'limitations':'Offline Blender candidate only. This edits only the existing Hips vertical channel to ground the lower weighted sole; it does not add true per-foot IK, does not alter the active Splat prefab/controller, and does not establish varied-platform, Unity-runtime, jump/landing, or human-play results.'}
report_path=os.path.join(OUT_DIR,'Splat_Iter1372_WalkContactReport.json')
with open(report_path,'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print('ITER1372_CONTACT_REPORT',json.dumps(report,indent=2))
# Reuse existing studio scene for a low-resolution eight-pose contact review.
scene.render.engine='CYCLES';scene.cycles.samples=10;scene.render.resolution_x=768;scene.render.resolution_y=768;scene.render.resolution_percentage=100
for frame in [1,5,9,13,17,21,25,29]:
    scene.frame_set(frame)
    scene.render.filepath=os.path.join(OUT_DIR,f'Splat_Iter1372_walk_{frame:02d}.png')
    bpy.ops.render.render(write_still=True)
# Save native edit and export a separate rigged FBX candidate with the grounded walk.
blend_path=os.path.join(OUT_DIR,'Splat_Meshy_Iter1365_RigBound_WalkGrounded_Iter1372.blend')
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
bpy.ops.object.select_all(action='DESELECT');meshobj.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm
fbx_path=os.path.join(OUT_DIR,'Splat_Meshy_Iter1365_RigBound_WalkGrounded_Iter1372.fbx')
bpy.ops.export_scene.fbx(filepath=fbx_path,use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
print('ITER1372_OUTPUT',blend_path,fbx_path,report_path)
