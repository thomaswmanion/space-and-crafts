import bpy, json, os
ROOT=r'E:\Users\thoma\git\space-and-crafts-3'
path=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1372\Splat_Meshy_Iter1365_RigBound_WalkGrounded_Iter1372.fbx'
log_path=ROOT+r'\Logs\splat-grounded-walk-fbx-audit-iter1373.json'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path,automatic_bone_orientation=False)
scene=bpy.context.scene
arms=[o for o in scene.objects if o.type=='ARMATURE']
meshes=[o for o in scene.objects if o.type=='MESH']
print('FBX',os.path.basename(path),'armatures',len(arms),'meshes',len(meshes),'actions',[(a.name,tuple(a.frame_range)) for a in bpy.data.actions])
if len(arms)!=1 or len(meshes)!=1:
    raise RuntimeError('Expected one armature and one skinned mesh in isolated candidate export.')
arm=arms[0];meshobj=meshes[0];meshobj.data.calc_loop_triangles()
action=arm.animation_data.action if arm.animation_data else None
print('ARM',arm.name,'scale',tuple(arm.scale),'bones',len(arm.data.bones),'action',action.name if action else None)
print('MESH',meshobj.name,'verts',len(meshobj.data.vertices),'tris',len(meshobj.data.loop_triangles),'groups',len(meshobj.vertex_groups),'uv_layers',len(meshobj.data.uv_layers),'materials',[s.material.name if s.material else None for s in meshobj.material_slots],'arm_mods',[(m.name,m.object.name if m.object else None) for m in meshobj.modifiers if m.type=='ARMATURE'])
idx={g.index:g.name for g in meshobj.vertex_groups}; arm.data.pose_position='REST';deps=bpy.context.evaluated_depsgraph_get();ev=meshobj.evaluated_get(deps);rest=ev.to_mesh();sole_indices={}
for side,sign in [('Left',1),('Right',-1)]:
 names={side+'Foot',side+'ToeBase'}; ids=[]
 for v in rest.vertices:
  q=meshobj.matrix_world@v.co;w=sum(a.weight for a in v.groups if idx.get(a.group) in names)
  if w>.1 and q.z<.24 and q.x*sign>.05:ids.append(v.index)
 sole_indices[side]=ids;print('SOLE',side,'count',len(ids))
ev.to_mesh_clear();arm.data.pose_position='POSE'
frames=[]
if action:
 for f in range(int(action.frame_range[0]),int(action.frame_range[1])+1):
  scene.frame_set(f);d=bpy.context.evaluated_depsgraph_get();ev=meshobj.evaluated_get(d);me=ev.to_mesh();pts=[meshobj.matrix_world@v.co for v in me.vertices];feet={s:min(pts[i].z for i in ids) for s,ids in sole_indices.items()};lower=min(feet.values());frames.append({'frame':f,'left':feet['Left'],'right':feet['Right'],'lower':lower,'mesh_min_z':min(p.z for p in pts),'mesh_max_z':max(p.z for p in pts)});ev.to_mesh_clear()
max_abs_lower=max(abs(x['lower']) for x in frames)
report={'iteration':1373,'export':path,'armatures':len(arms),'meshes':len(meshes),'bones':len(arm.data.bones),'triangles':len(meshobj.data.loop_triangles),'vertex_groups':len(meshobj.vertex_groups),'uv_layers':len(meshobj.data.uv_layers),'animation_action':action.name if action else None,'action_frames':list(action.frame_range) if action else None,'weighted_sole_vertex_count':{k:len(v) for k,v in sole_indices.items()},'whole_cycle_lower_sole_max_absolute_z_m':max_abs_lower,'whole_cycle_frames':frames,'scope_note':'Blender FBX re-import audit and offline skinned-mesh evaluation only; not Unity runtime, active prefab integration, or human play.'}
with open(log_path,'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print('ITER1373_REPORT',json.dumps({k:v for k,v in report.items() if k not in ('whole_cycle_frames',)},indent=2))
