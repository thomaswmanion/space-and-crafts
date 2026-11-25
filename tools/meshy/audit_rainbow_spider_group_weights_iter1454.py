import bpy, json, os
root=r'E:\Users\thoma\git\space-and-crafts-3'
blend=os.path.join(root,'Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1453/RainbowSpider_RigCandidate.blend')
bpy.ops.wm.open_mainfile(filepath=blend)
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('RainbowKingdomSpider'))
counts={g.name:0 for g in mesh.vertex_groups}
for v in mesh.data.vertices:
 for g in v.groups:
  if g.weight>0.001: counts[mesh.vertex_groups[g.group].name]+=1
with open(os.path.join(root,'Logs/spider-rig-weight-groups-iter1454.json'),'w') as f: json.dump(counts,f,indent=2,sort_keys=True)
print('GROUPS',len(counts),'NONEMPTY',sum(1 for c in counts.values() if c),'REPORT',os.path.join(root,'Logs/spider-rig-weight-groups-iter1454.json'))

