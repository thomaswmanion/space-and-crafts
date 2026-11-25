import bpy, os
p='Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1408/ChromaticRegent_AnimationBundle.blend'; bpy.ops.wm.open_mainfile(filepath=p)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'); act=next(a for a in bpy.data.actions if 'ChromaticRegent_Hit' in a.name); arm.animation_data_create(); arm.animation_data.action=act; arm.animation_data.action_slot=act.slots[0]
scene=bpy.context.scene; scene.render.resolution_percentage=60
out='Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1408/HitFrames'; os.makedirs(out,exist_ok=True)
for frame in [1,9,18,27,36]:
 scene.frame_set(frame); bpy.context.view_layer.update(); scene.render.filepath=os.path.abspath(os.path.join(out,f'hit_{frame:02d}.png')); bpy.ops.render.render(write_still=True)
