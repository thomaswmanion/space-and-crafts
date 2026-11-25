import bpy, os
p=os.path.abspath("Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate/RainbowSpider_RigCandidate.blend")
bpy.ops.wm.open_mainfile(filepath=p)
print("ACTIONS", [(a.name,a.frame_range[:]) for a in bpy.data.actions])
for o in bpy.context.scene.objects:
 if o.type=="ARMATURE": print("ARM",o.name,o.animation_data and o.animation_data.action and o.animation_data.action.name)