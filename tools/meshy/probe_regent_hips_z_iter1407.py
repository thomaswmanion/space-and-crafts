import bpy
from mathutils import Vector
p='Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1407/ChromaticRegent_AnimationBundle.blend'; bpy.ops.wm.open_mainfile(filepath=p)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'); act=next(a for a in bpy.data.actions if 'ChromaticRegent_Hit' in a.name); arm.animation_data_create(); arm.animation_data.action=act; arm.animation_data.action_slot=act.slots[0]
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH'); groups={g.index:g.name for g in mesh.vertex_groups}; footgroups={i for i,n in groups.items() if n in ('LeftFoot','LeftToeBase','RightFoot','RightToeBase')}; idx={v.index for v in mesh.data.vertices if any(g.group in footgroups and g.weight>.2 for g in v.groups)}
def footmin():
 ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get()); em=ev.to_mesh(); z=min((ev.matrix_world@em.vertices[i].co).z for i in idx); ev.to_mesh_clear(); return z
bpy.context.scene.frame_set(24); bpy.context.view_layer.update(); hip=arm.pose.bones['Hips']; z0=footmin(); hip.location.z+=.1; bpy.context.view_layer.update(); z1=footmin(); print('FRAME24',z0,z1,'delta',z1-z0)
