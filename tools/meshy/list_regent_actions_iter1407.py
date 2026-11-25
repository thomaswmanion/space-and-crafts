import bpy
p='Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/20261101_chromatic-regent-animation-bundle-iter1407/ChromaticRegent_AnimationBundle.blend'; bpy.ops.wm.open_mainfile(filepath=p)
print([a.name for a in bpy.data.actions])
