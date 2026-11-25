import bpy
p='Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1407/ChromaticRegent_AnimationBundle.blend'
bpy.ops.wm.open_mainfile(filepath=p)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
act=next(a for a in bpy.data.actions if 'ChromaticRegent_Hit' in a.name)
slot=act.slots[0]; arm.animation_data_create(); arm.animation_data.action=act; arm.animation_data.action_slot=slot
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
for f in [1,6,12,18,24,30,36]:
 bpy.context.scene.frame_set(f); bpy.context.view_layer.update()
 zs=[(mesh.matrix_world @ __import__('mathutils').Vector(c)).z for c in mesh.bound_box]
 hip=arm.pose.bones.get('Hips')
 print('FRAME',f,'meshBottom',round(min(zs),3),'meshTop',round(max(zs),3),'hipsLocalZ',round(hip.location.z,3))


