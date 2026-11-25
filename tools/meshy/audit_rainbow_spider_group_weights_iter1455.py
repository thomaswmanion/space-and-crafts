import bpy, json, os
root=r'E:\Users\thoma\git\space-and-crafts-3'
blend=os.path.join(root,r'Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1455/RainbowSpider_RigCandidate.blend')
bpy.ops.wm.open_mainfile(filepath=blend)
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('RainbowKingdomSpider'))
counts={g.name:0 for g in mesh.vertex_groups}; solid={g.name:0 for g in mesh.vertex_groups}
for v in mesh.data.vertices:
 for g in v.groups:
  name=mesh.vertex_groups[g.group].name
  if g.weight>0.001: counts[name]+=1
  if g.weight>=0.2: solid[name]+=1
out=os.path.join(root,'Logs/spider-rig-weight-groups-iter1455.json')
with open(out,'w') as f: json.dump({'any':counts,'at_least_0_2':solid},f,indent=2,sort_keys=True)
print('WEIGHTS',[(n,c,solid[n]) for n,c in counts.items() if n.endswith('_Foot')])
