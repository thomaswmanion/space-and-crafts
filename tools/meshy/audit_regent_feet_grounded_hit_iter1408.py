import bpy
p='Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1408/ChromaticRegent_AnimationBundle.blend'; bpy.ops.wm.open_mainfile(filepath=p)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'); act=next(a for a in bpy.data.actions if 'ChromaticRegent_Hit' in a.name); arm.animation_data_create(); arm.animation_data.action=act; arm.animation_data.action_slot=act.slots[0]
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH'); names={g.index:g.name for g in mesh.vertex_groups}; sides={side:{i for i,n in names.items() if n in (side+'Foot',side+'ToeBase')} for side in ('Left','Right')}
ids={side:[v.index for v in mesh.data.vertices if sum(g.weight for g in v.groups if g.group in groups)>=.3] for side,groups in sides.items()}
for f in [1,6,12,18,24,30,36]:
 bpy.context.scene.frame_set(f); bpy.context.view_layer.update(); ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get()); em=ev.to_mesh()
 vals={side:min((ev.matrix_world@em.vertices[i].co).z for i in idx) for side,idx in ids.items()}; print('FRAME',f, {k:round(v,6) for k,v in vals.items()})
 ev.to_mesh_clear()
