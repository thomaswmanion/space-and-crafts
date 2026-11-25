import bpy
p=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Enemies\Pulpal\Meshy_Iter1994\Pulpal_Rigged.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=p)
m=next(o for o in bpy.context.scene.objects if o.type=='MESH')
for side in ('LeftFoot','RightFoot','LeftToeBase','RightToeBase','Hips','Head'):
 g=next((x for x in m.vertex_groups if x.name==side),None)
 if not g: continue
 ids=[]
 for v in m.data.vertices:
  for a in v.groups:
   if a.group==g.index and a.weight>=.25: ids.append(v.index); break
 pts=[m.matrix_world @ m.data.vertices[i].co for i in ids]
 if pts: print(side,'count',len(pts),'world_minmax',[(min(getattr(v,axis) for v in pts),max(getattr(v,axis) for v in pts)) for axis in ('x','y','z')])
print('MESH_BOUNDS',[(min((m.matrix_world@v.co)[i] for v in m.data.vertices),max((m.matrix_world@v.co)[i] for v in m.data.vertices)) for i in range(3)])
