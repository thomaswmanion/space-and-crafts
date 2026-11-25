import bpy
import os

path = os.path.abspath(
    "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/"
    "20261101_rainbow-spider-rig-candidate-iter1409/RainbowSpider_RigCandidate.blend"
)
bpy.ops.wm.open_mainfile(filepath=path)
print("ACTIONS", [(a.name, tuple(a.frame_range)) for a in bpy.data.actions])
for obj in bpy.context.scene.objects:
    if obj.type == "ARMATURE":
        active = obj.animation_data.action.name if obj.animation_data and obj.animation_data.action else None
        print("ARMATURE", obj.name, "ACTIVE", active)
