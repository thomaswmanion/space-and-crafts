import bpy, json, os, sys
from mathutils import Vector
ARGS = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
FBX = ARGS[0] if ARGS else r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Enemies\Pulpal\Meshy_Iter1994\Pulpal_IdleAttackHitDefeatRecover.fbx'
OUT = ARGS[1] if len(ARGS) > 1 else r'E:\Users\thoma\git\space-and-crafts-3\Logs\score-comparison\pulpal-meshy-animation-review-iter2180\report.json'
os.makedirs(os.path.dirname(OUT), exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=True)
scene=bpy.context.scene
arms=[o for o in scene.objects if o.type=='ARMATURE']
meshes=[o for o in scene.objects if o.type=='MESH']
if len(arms)!=1: raise RuntimeError(f'expected one armature, got {len(arms)}')
if not meshes: raise RuntimeError('no mesh objects')
arm=arms[0]
arm.animation_data_create()
all_actions=sorted(bpy.data.actions, key=lambda a:a.name)
slots={}
for action in all_actions:
    arm.animation_data.action=action
    if getattr(action,'slots',None): arm.animation_data.action_slot=action.slots[0]
    slots[action.name]=list(action.frame_range)
view=bpy.context.evaluated_depsgraph_get()
# Find groups with foot/toe in their names and form left/right pair buckets.
weighted={}
for obj in meshes:
    groups={g.name:g.index for g in obj.vertex_groups}
    names=[name for name in groups if ('foot' in name.lower() or 'toe' in name.lower())]
    if names:
        for side in ('left','right'):
            indices=[groups[n] for n in names if side in n.lower()]
            verts=[]
            if indices:
                for v in obj.data.vertices:
                    total=sum(a.weight for a in v.groups if a.group in indices)
                    if total>=0.25: verts.append(v.index)
                weighted[(obj.name,side)]=verts

def sample(action,t):
    arm.animation_data.action=action
    if getattr(action,'slots',None): arm.animation_data.action_slot=action.slots[0]
    f0,f1=action.frame_range
    frame=round(f0+(f1-f0)*t)
    scene.frame_set(frame); bpy.context.view_layer.update()
    rows=[]
    for obj in meshes:
        ev=obj.evaluated_get(view); baked=ev.to_mesh(); mw=ev.matrix_world.copy()
        pts=[mw@v.co for v in baked.vertices]
        if not pts: ev.to_mesh_clear(); continue
        row={'mesh':obj.name,'vertices':len(baked.vertices),'min_z':min(p.z for p in pts),'max_z':max(p.z for p in pts),'height':max(p.z for p in pts)-min(p.z for p in pts)}
        for side in ('left','right'):
            inds=weighted.get((obj.name,side),[])
            if inds: row[side+'_foot_bottom_z']=min(pts[i].z for i in inds)
        rows.append(row); ev.to_mesh_clear()
    return frame,rows
sample_actions=[]
for action in all_actions:
    for t in (0.0,0.2,0.4,0.6,0.8):
        frame,rows=sample(action,t)
        sample_actions.append({'action':action.name,'normalized_time':t,'frame':frame,'meshes':rows})
# floor calibration: lowest weighted boot in idle at t=.2, else whole mesh bound
idle=next((a for a in all_actions if 'idle' in a.name.lower()),all_actions[0] if all_actions else None)
ref=None
if idle:
    _,rows=sample(idle,0.2)
    vals=[r[k] for r in rows for k in ('left_foot_bottom_z','right_foot_bottom_z') if k in r]
    ref=min(vals) if vals else min(r['min_z'] for r in rows)
report={'source':FBX,'coordinate_system':'Imported FBX is Z-up; contact measurements use world Z.','armatures':[{'name':a.name,'bones':len(a.data.bones),'bone_names':[b.name for b in a.data.bones]} for a in arms],'meshes':[{'name':o.name,'vertices':len(o.data.vertices),'polygons':len(o.data.polygons),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'vertex_groups':len(o.vertex_groups)} for o in meshes],'actions':slots,'weighted_foot_vertices':{f'{k[0]}:{k[1]}':len(v) for k,v in weighted.items()},'idle_reference_floor_z':ref,'samples':sample_actions,'limitations':['Offline Blender FBX import/evaluation only; no Unity Animator transitions, gameplay AI, collider, or platform physics.','Reference floor is a weighted foot-vertex proxy from Idle t=0.20; it does not prove actual boot/collider contact.','This report does not establish quality at gameplay camera scale or human play.','This report supersedes the initial Iteration 2180 report written with an incorrect Y-up assumption.']}
with open(OUT,'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
print(json.dumps({'armatures':len(arms),'bones':len(arm.data.bones),'meshes':len(meshes),'actions':slots,'weighted_foot_vertices':report['weighted_foot_vertices'],'reference_floor_z':ref,'samples':len(sample_actions),'report':OUT}))
