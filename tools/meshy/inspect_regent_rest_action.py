import bpy, os
p=os.path.abspath('Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle/ChromaticRegent_AnimationBundle.blend')
bpy.ops.wm.open_mainfile(filepath=p)
for a in bpy.data.actions:
 if 'clip0' in a.name:
  print('ACTION',a.name)
  for layer in a.layers:
   for strip in layer.strips:
    for bag in strip.channelbags:
     for fc in bag.fcurves:
      if 'Hips' in fc.data_path:
       print(fc.data_path,fc.array_index,[tuple(k.co) for k in fc.keyframe_points[:3]])
