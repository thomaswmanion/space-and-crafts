import bpy, json, os, numpy as np
from mathutils import Vector
ROOT=r'E:\Users\thoma\git\space-and-crafts-3'
IN_BLEND=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1372\Splat_Meshy_Iter1365_RigBound_WalkGrounded_Iter1372.blend'
TEXTURE=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\Splat_300k_Remesh_Iter1365_textures\base_color.png'
OUT_DIR=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1374'
os.makedirs(OUT_DIR,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=IN_BLEND)
scene=bpy.context.scene; arm=next(o for o in scene.objects if o.type=='ARMATURE'); mesh=next(o for o in scene.objects if o.type=='MESH' and 'RigBound' in o.name)
# Use the UV color map plus a soft central ellipsoid to isolate the approved
# white body and its painted face from purple limbs and the cap.
img=bpy.data.images.load(TEXTURE,check_existing=True); w,h=img.size; pixels=np.asarray(img.pixels[:],dtype=np.float32).reshape((h,w,4))
uv_layer=mesh.data.uv_layers.active
uvs=np.empty((len(uv_layer.data),2),dtype=np.float32); uv_layer.data.foreach_get('uv',uvs.ravel())
loop_verts=np.empty(len(mesh.data.loops),dtype=np.int32); mesh.data.loops.foreach_get('vertex_index',loop_verts)
px=np.clip(np.rint(uvs[:,0]*(w-1)).astype(np.int32),0,w-1); py=np.clip(np.rint(uvs[:,1]*(h-1)).astype(np.int32),0,h-1)
loop_colors=pixels[py,px,:3]
color_sums=np.zeros((len(mesh.data.vertices),3),dtype=np.float64); color_counts=np.zeros(len(mesh.data.vertices),dtype=np.int32)
np.add.at(color_sums,loop_verts,loop_colors); np.add.at(color_counts,loop_verts,1)
colors=color_sums/np.maximum(color_counts[:,None],1)
world_positions=np.array([(mesh.matrix_world@v.co)[:] for v in mesh.data.vertices],dtype=np.float64)
x,y,z=world_positions.T
minimum=colors.min(axis=1); chroma=colors.max(axis=1)-minimum
# Smooth mask from painted off-white skin; recover the dark/purple face marks
# in their known frontal region so eyes and smile remain attached to the ball.
white=np.clip((minimum-.50)/.34,0,1)*np.clip((.34-chroma)/.22,0,1)
core=np.sqrt((x/.66)**2+(y/.56)**2+((z-.95)/.66)**2)
radial=np.clip((1.36-core)/.40,0,1)
spatial=(np.abs(x)<.72)&(np.abs(y)<.62)&(z>.30)&(z<1.60)
body_t=0.92*radial*white*spatial
face=(np.abs(x)<.44)&(y<-.26)&(z>.70)&(z<1.38)&(core<1.35)
body_t=np.maximum(body_t,np.where(face,0.88*radial,0.0))
body_t=np.clip(body_t,0,0.92)
selected=np.flatnonzero(body_t>0.05)
if len(selected)<15000:
 raise RuntimeError(f'Only {len(selected)} body vertices matched; refusing to alter skin weights.')
hips_group=mesh.vertex_groups.get('Hips')
if hips_group is None: raise RuntimeError('Hips group missing.')
idx_by_name={g.name:g.index for g in mesh.vertex_groups}; hips_idx=hips_group.index
old_group_counts={g.name:0 for g in mesh.vertex_groups}; new_group_counts={g.name:0 for g in mesh.vertex_groups}
# Pull sparse current weights from the mesh, blend the torso/face toward Hips,
# then quantize normalized influence sums to 8-bit weights in efficient batches.
groups_by_index={g.index:g.name for g in mesh.vertex_groups}
assign={g.name:{} for g in mesh.vertex_groups}
for i in selected:
 v=mesh.data.vertices[int(i)]; old={a.group:a.weight for a in v.groups}
 for gi,weight in old.items(): old_group_counts[groups_by_index[gi]]+=1
 t=float(body_t[i]); blended={gi:weight*(1-t) for gi,weight in old.items()}
 blended[hips_idx]=blended.get(hips_idx,0.0)+t
 total=sum(blended.values())
 if total<=1e-8: raise RuntimeError(f'Invalid weight sum at vertex {i}.')
 normalized={gi:weight/total for gi,weight in blended.items() if weight>1e-5}
 raw={gi:normalized[gi]*255.0 for gi in normalized}
 units={gi:int(np.floor(value)) for gi,value in raw.items()}
 remain=255-sum(units.values())
 order=sorted(raw,key=lambda gi:raw[gi]-units[gi],reverse=True)
 for gi in order[:max(0,remain)]: units[gi]+=1
 for gi,amount in units.items():
  if amount<=0: continue
  name=groups_by_index[gi]; assign[name].setdefault(amount,[]).append(int(i)); new_group_counts[name]+=1
for group in mesh.vertex_groups:
 for amount,indices in assign[group.name].items(): group.add(indices,amount/255.0,'REPLACE')
# Render and save as a separate candidate; keep existing approved outputs intact.
mesh.name='ART_SplatBody_Iter1365_RigidCore_Iter1374'
scene.render.engine='CYCLES';scene.cycles.samples=14;scene.render.resolution_x=768;scene.render.resolution_y=768;scene.render.resolution_percentage=100
render_frames=[1,9,17,25]
for frame in render_frames:
 scene.frame_set(frame);scene.render.filepath=os.path.join(OUT_DIR,f'Splat_Iter1374_walk_{frame:02d}.png');bpy.ops.render.render(write_still=True)
report={'iteration':1374,'source':'Iter1372 grounded walk candidate','body_vertices_selected':len(selected),'face_vertices_recovered':int(face.sum()),'body_weight_blend_mean':float(body_t[selected].mean()),'body_weight_blend_p90':float(np.quantile(body_t[selected],.90)),'changed_group_vertex_counts':new_group_counts,'render_frames':render_frames,'limitations':'Offline candidate weight edit only. Central white sphere and face are biased toward Hips to reduce torso squash; purple limb and cap weights are preserved where they are outside the masked body/face region. The four Blender walk renders are visual previews, not Unity gameplay or human-play evidence.'}
with open(os.path.join(OUT_DIR,'Splat_Iter1374_BodyWeightReport.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print('ITER1374_REPORT',json.dumps(report,indent=2))
blend=os.path.join(OUT_DIR,'Splat_Meshy_Iter1365_RigBound_RigidCore_Iter1374.blend');bpy.ops.wm.save_as_mainfile(filepath=blend)
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm
fbx=os.path.join(OUT_DIR,'Splat_Meshy_Iter1365_RigBound_RigidCore_Iter1374.fbx')
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
print('ITER1374_OUTPUT',blend,fbx)
