import bpy, json, os
root=r'E:\Users\thoma\git\space-and-crafts-3'
blend=os.path.join(root,'Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1459/RainbowSpider_RigCandidate.blend')
out=os.path.join(root,'Logs/spider-walk-contact-iter1459.json')
bpy.ops.wm.open_mainfile(filepath=blend)
scene=bpy.context.scene; mesh=next(o for o in scene.objects if o.type=='MESH' and o.name.startswith('RainbowKingdomSpider')); arm=next(o for o in scene.objects if o.type=='ARMATURE'); arm.animation_data.action=bpy.data.actions.get('Spider_Walk')
groups={g.name:g.index for g in mesh.vertex_groups}; supports={}
for side in ('L','R'):
 for row in range(4):
  name=f'Leg_{side}_{row}_Foot'; gi=groups.get(name)
  supports[name]=[v.index for v in mesh.data.vertices if gi is not None and any(g.group==gi and g.weight>=0.2 for g in v.groups)]
records=[]
for frame in range(1,26):
 scene.frame_set(frame); bpy.context.view_layer.update(); dg=bpy.context.evaluated_depsgraph_get(); ev=mesh.evaluated_get(dg); em=ev.to_mesh(); pts=[ev.matrix_world @ v.co for v in em.vertices]
 legdata=[]
 for name,indices in supports.items():
  z=min((pts[i].z for i in indices),default=None); legdata.append({'leg':name,'verts':len(indices),'minZ':round(z,5) if z is not None else None})
 records.append({'frame':frame,'wholeMinZ':round(min(p.z for p in pts),5),'feet':legdata}); ev.to_mesh_clear()
with open(out,'w') as f: json.dump({'source':blend,'frames':records},f,indent=2)
print('CONTACT_SAMPLE',out,'frames',len(records),'wholeZrange',min(r['wholeMinZ'] for r in records),max(r['wholeMinZ'] for r in records),'supportCounts',[(n,len(i)) for n,i in supports.items()])

