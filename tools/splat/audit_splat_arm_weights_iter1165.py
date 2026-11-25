import bpy
obj=bpy.data.objects['ART_SplatBody_Iter378']
arm=bpy.data.objects['Splat_SkinRig']
for name in ('Bone_ArmL','Bone_ArmTipL','Bone_ArmR','Bone_ArmTipR','Bone_LegL','Bone_LegR'):
 bone=arm.data.bones[name]; idx=next(g.index for g in obj.vertex_groups if g.name==name)
 values=[]
 for v in obj.data.vertices:
  for g in v.groups:
   if g.group==idx and g.weight>0.05: values.append((v.co.copy(),g.weight))
 if values:
  mins=[min(p[i] for p,w in values) for i in range(3)]; maxs=[max(p[i] for p,w in values) for i in range(3)]
  print(name,'bone',tuple(round(x,3) for x in bone.head_local),tuple(round(x,3) for x in bone.tail_local),'verts',len(values),'bbox',mins,maxs,'weight range',min(w for p,w in values),max(w for p,w in values))