"""Candidate-only one-axis stance correction on Splat's existing Meshy rig."""
import bpy, json, os, numpy as np
from mathutils import Quaternion
ROOT=r'E:\Users\thoma\git\space-and-crafts-3'
SRC=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1389\Splat_RotationOnly_Gait_Iter1389.blend'
OUT=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1395'
os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SRC)
scene=bpy.context.scene; rig=next(o for o in scene.objects if o.type=='ARMATURE'); obj=next(o for o in scene.objects if o.type=='MESH' and 'RigBound' in o.name)
mesh=obj.data; groups={g.name:g.index for g in obj.vertex_groups}; deps=bpy.context.evaluated_depsgraph_get()
sets={s:np.asarray([v.index for v in mesh.vertices if any(i.group in (groups[s+'Foot'],groups[s+'ToeBase']) and i.weight>=.25 for i in v.groups)],dtype=np.int32) for s in ('Left','Right')}
def evaluated_points():
 bpy.context.view_layer.update(); ev=obj.evaluated_get(deps); em=ev.to_mesh(preserve_all_data_layers=True,depsgraph=deps)
 try:
  c=np.empty(len(em.vertices)*3,dtype=np.float32); em.vertices.foreach_get('co',c); c=c.reshape(-1,3); m=np.asarray(ev.matrix_world,dtype=np.float64); return c@m[:3,:3].T+m[:3,3]
 finally: ev.to_mesh_clear()
def sole_metrics():
 p=evaluated_points(); result={}
 for side,idx in sets.items():
  local=idx[np.argmin(p[idx,2])]; result[side]={'z':float(p[local,2]),'point':p[local].tolist(),'vertex':int(local)}
 return result
samples=[]
for frame in range(1,34):
 scene.frame_set(frame); baseline=sole_metrics(); side=min(baseline,key=lambda s:baseline[s]['z']); bone=rig.pose.bones[side+'UpLeg']; base=bone.rotation_quaternion.copy()
 def set_delta(d): bone.rotation_quaternion=base@Quaternion((1,0,0),float(d))
 def z_at(d): set_delta(d); return sole_metrics()[side]['z']
 lo,hi=-.4,.4; zl,zh=z_at(lo),z_at(hi)
 if zl*zh>0: raise RuntimeError(f'No contact bracket at frame {frame}: {side} {zl}..{zh}')
 for _ in range(20):
  mid=(lo+hi)*.5; zm=z_at(mid)
  if zl*zm<=0: hi=mid; zh=zm
  else: lo=mid; zl=zm
 delta=(lo+hi)*.5; set_delta(delta); bone.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=side+'UpLeg')
 after=sole_metrics(); samples.append({'frame':frame,'stance':side,'before':baseline,'rotation_correction_rad':float(delta),'after':after})
# Dense one-frame samples preserve the numerically fitted support path in this isolated Blender action.
action=rig.animation_data.action
for layer in action.layers:
 for strip in layer.strips:
  for slot in action.slots:
   bag=strip.channelbag(slot)
   if bag:
    for fc in bag.fcurves:
     for k in fc.keyframe_points: k.interpolation='LINEAR'
report={'iteration':1395,'action':action.name,'source':'Iter1389 rotation-only gait on Meshy 24-bone rig','support_z_m':0.0,'sampled_frames':33,'selection':'lower of left/right weighted sole minima (foot/toe weights >= 0.25); correct that side by one local UpLeg X rotation','contact_tolerance_m':0.002,'max_stance_error_m':max(abs(s['after'][s['stance']]['z']) for s in samples),'max_swing_penetration_m':max(0.0,max(-s['after'][('Right' if s['stance']=='Left' else 'Left')]['z'] for s in samples)),'frame_data':samples,'limitations':'Offline Blender candidate only; one flat support plane, no runtime motor, no varied-platform or human-play verification. The fitted thigh-angle path may slide the foot in horizontal space and body/joint deformation requires visual inspection.'}
with open(os.path.join(OUT,'Splat_Iter1395_FootSupportGaitReport.json'),'w') as f: json.dump(report,f,indent=2)
scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=768; scene.render.resolution_y=768; scene.render.resolution_percentage=100
for frame in (1,5,9,13,17,21,25,29,33):
 scene.frame_set(frame); scene.render.filepath=os.path.join(OUT,f'Splat_Iter1395_gait_{frame:02d}.png'); bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Splat_FootSupportGait_Iter1395.blend'))
print('ITER1395_SUMMARY',json.dumps({k:v for k,v in report.items() if k!='frame_data'}))
