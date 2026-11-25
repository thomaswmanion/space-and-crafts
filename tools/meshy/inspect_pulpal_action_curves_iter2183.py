import bpy
p=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Enemies\Pulpal\Meshy_Iter1994\Pulpal_PaperPressAttack.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=p)
a=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
act=next(iter(bpy.data.actions));print('ACTION',act.name,'slots',len(act.slots),'layers',len(act.layers))
for slot in act.slots:
 print('SLOT',slot.identifier)
 for layer in act.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    if bag.slot==slot:
     for fc in bag.fcurves:
      if any(k in fc.data_path for k in ('LeftArm','RightArm','Hips','Spine')):
       print(fc.data_path,fc.array_index,'keys',len(fc.keyframe_points),'range',fc.evaluate(1),fc.evaluate(45),fc.evaluate(90))
