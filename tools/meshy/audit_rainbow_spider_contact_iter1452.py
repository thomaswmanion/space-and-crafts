import bpy, json, os
from mathutils import Vector
root = r'E:\Users\thoma\git\space-and-crafts-3'
blend = os.path.join(root, 'Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1451/RainbowSpider_RigCandidate.blend')
out = os.path.join(root, 'Logs/spider-walk-contact-iter1452.json')
bpy.ops.wm.open_mainfile(filepath=blend)
scene = bpy.context.scene
mesh = next(o for o in scene.objects if o.type=='MESH' and o.name.startswith('RainbowKingdomSpider'))
arm = next(o for o in scene.objects if o.type=='ARMATURE')
action = bpy.data.actions.get('Spider_Walk')
arm.animation_data.action = action
scene.frame_start, scene.frame_end = 1, 25
records=[]
for frame in range(1,26):
    scene.frame_set(frame); bpy.context.view_layer.update(); dg=bpy.context.evaluated_depsgraph_get()
    ev=mesh.evaluated_get(dg); em=ev.to_mesh()
    points=[ev.matrix_world @ v.co for v in em.vertices]
    lower=min(p.z for p in points)
    legs=[]
    for side in ('L','R'):
        for row in range(4):
            name=f'Leg_{side}_{row}_Foot'
            group=mesh.vertex_groups.get(name)
            if group is None: continue
            idx=group.index
            weighted=[]
            for v in mesh.data.vertices:
                if any(g.group==idx and g.weight>=0.2 for g in v.groups): weighted.append(v.index)
            zmin=min((points[i].z for i in weighted),default=None)
            pb=arm.pose.bones.get(f'Leg_{side}_{row}_IK')
            tip=arm.matrix_world @ pb.tail if pb else Vector((0,0,0))
            legs.append({'leg':f'{side}{row}','weightedVerts':len(weighted),'soleZ':round(zmin,5) if zmin is not None else None,'target':[round(x,5) for x in tip]})
    records.append({'frame':frame,'meshMinZ':round(lower,5),'legs':legs})
    ev.to_mesh_clear()
with open(out,'w') as f: json.dump({'source':blend,'supportPlaneZ':-0.62,'frames':records},f,indent=2)
print('AUDIT',out,'frames',len(records),'meshMinRange',min(x['meshMinZ'] for x in records),max(x['meshMinZ'] for x in records))
