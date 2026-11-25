import bpy
from mathutils import Vector
p='Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1408/ChromaticRegent_AnimationBundle.blend'; bpy.ops.wm.open_mainfile(filepath=p)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'); act=next(a for a in bpy.data.actions if 'ChromaticRegent_Hit' in a.name); arm.animation_data_create(); arm.animation_data.action=act; arm.animation_data.action_slot=act.slots[0]
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH'); groups={g.index:g.name for g in mesh.vertex_groups}
for f in [1,6,12,18,24,30,36]:
 bpy.context.scene.frame_set(f); bpy.context.view_layer.update(); ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get()); em=ev.to_mesh()
 ranked=sorted(((ev.matrix_world@v.co).z,i) for i,v in enumerate(em.vertices))[:5]
 rows=[]
 for z,i in ranked:
  v=mesh.data.vertices[i]; ws=sorted(((groups[g.group],g.weight) for g in v.groups if g.group in groups),key=lambda p:-p[1])[:3]
  rows.append((i,round(z,3),[(n,round(w,2)) for n,w in ws]))
 print('FRAME',f,rows)
 ev.to_mesh_clear()

