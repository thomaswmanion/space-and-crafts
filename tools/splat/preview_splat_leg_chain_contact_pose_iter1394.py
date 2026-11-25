import bpy, json, os, numpy as np
from mathutils import Quaternion
ROOT=r'E:\Users\thoma\git\space-and-crafts-3'
SRC=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1389\Splat_RotationOnly_Gait_Iter1389.blend'
OUT=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1394'
os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SRC)
scene=bpy.context.scene; scene.frame_set(25)
rig=next(o for o in scene.objects if o.type=='ARMATURE')
obj=next(o for o in scene.objects if o.type=='MESH' and 'RigBound' in o.name)
mesh=obj.data; groups={g.name:g.index for g in obj.vertex_groups}
def ids(side):
 return np.asarray([v.index for v in mesh.vertices if any(i.group in (groups[side+'Foot'],groups[side+'ToeBase']) and i.weight>=.25 for i in v.groups)],dtype=np.int32)
sets={s:ids(s) for s in ('Left','Right')}
def evaluated():
 bpy.context.view_layer.update(); ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get()); em=ev.to_mesh(preserve_all_data_layers=True,depsgraph=bpy.context.evaluated_depsgraph_get())
 try:
  c=np.empty(len(em.vertices)*3,dtype=np.float32); em.vertices.foreach_get('co',c); c=c.reshape(-1,3); M=np.array(ev.matrix_world,dtype=np.float64); p=c@M[:3,:3].T+M[:3,3]; return p
 finally: ev.to_mesh_clear()
base=evaluated(); base_metrics={s:float(base[v,2].min()) for s,v in sets.items()}
# At this stance sample the Meshy thigh is swung away from the support.
# Cancel its -0.20 rad swing with +0.20 and add slight knee flex.
rig.pose.bones['LeftUpLeg'].rotation_quaternion = rig.pose.bones['LeftUpLeg'].rotation_quaternion @ Quaternion((1,0,0),.20)
rig.pose.bones['LeftLeg'].rotation_quaternion = rig.pose.bones['LeftLeg'].rotation_quaternion @ Quaternion((1,0,0),-.05)
posed=evaluated(); posed_metrics={s:float(posed[v,2].min()) for s,v in sets.items()}
report={'iteration':1394,'source':'Iter1389 rotation-only gait','frame':25,'changes_radians':{'LeftUpLeg_local_x':.20,'LeftLeg_local_x':-.05},'support_z_m':0.0,'before_weighted_sole_min_m':base_metrics,'after_weighted_sole_min_m':posed_metrics,'weighted_samples':{s:int(len(v)) for s,v in sets.items()},'status':'pose preview pending visual review; candidate only'}
with open(os.path.join(OUT,'Splat_Iter1394_ContactPoseReport.json'),'w') as f: json.dump(report,f,indent=2)
scene.render.resolution_x=768; scene.render.resolution_y=768; scene.render.resolution_percentage=100; scene.render.filepath=os.path.join(OUT,'Splat_Iter1394_contact_frame25.png'); bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Splat_Iter1394_ContactPose.blend'))
print('ITER1394',json.dumps(report))
