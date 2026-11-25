from pathlib import Path
import bpy
from collections import defaultdict
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
model=root/'Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter2000_Meshy71/Splat_Iter2000_Remeshed_Rigged.glb'
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=str(model))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name=='char1')
groups={g.index:g.name for g in obj.vertex_groups}; rows=defaultdict(list); weight_sum=defaultdict(float)
for v in obj.data.vertices:
    for inf in v.groups:
        n=groups.get(inf.group,str(inf.group)); weight_sum[n]+=inf.weight
        if inf.weight>=0.35: rows[n].append(obj.matrix_world@v.co)
for name,pts in rows.items():
    if len(pts)<100: continue
    vals=[[p[i] for p in pts] for i in range(3)]
    bbox=[(min(a),max(a)) for a in vals]
    center=sum(pts,Vector())/len(pts)
    print('GROUP',name,'n>=.35',len(pts),'wSum',round(weight_sum[name],1),'center',tuple(round(v,4) for v in center),'bounds',[[round(v,4) for v in pair] for pair in bbox])
