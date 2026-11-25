import bpy, os
root=os.path.abspath('Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/MeshySpenddown/20261031_223817_rainbow-kingdom-chromatic-rege_01a1952f/exports')
bpy.ops.import_scene.gltf(filepath=os.path.join(root,'hit-reaction/animation_glb.glb'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'); act=arm.animation_data.action
print('SLOTS',[(s.identifier,s.target_id_type) for s in act.slots]); print('LAYERS',len(act.layers))
for li,layer in enumerate(act.layers):
 print('LAYER',li,layer.name,'strips',len(layer.strips))
 for strip in layer.strips:
  print('STRIP',strip.type,'channelbags',len(strip.channelbags))
  for bag in strip.channelbags:
   print('BAG',bag.slot.identifier if bag.slot else None,'curves',len(bag.fcurves))
   for fc in bag.fcurves:
    grp=fc.group.name if fc.group else ''
    if grp in ('Hips','Spine','Spine02','Head','LeftArm','RightArm','LeftForeArm','RightForeArm','LeftUpLeg','RightUpLeg','LeftLeg','RightLeg'):
     vals=[kp.co.y for kp in fc.keyframe_points]
     print(grp,fc.data_path,fc.array_index,'n',len(vals),'range',(round(min(vals),3),round(max(vals),3)))

