"""Render the isolated Iter1444 spider surface candidate for visual review."""
import bpy
import os

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BLEND = os.path.join(
    ROOT,
    "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/"
    "20261101_rainbow-spider-rig-candidate-iter1444/RainbowSpider_RigCandidate_Iter1444.blend",
)
OUTPUT = os.path.join(
    ROOT,
    "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/"
    "20261101_rainbow-spider-rig-candidate-iter1444/Spider_Walk_Iter1444.png",
)
bpy.ops.wm.open_mainfile(filepath=BLEND)
scene = bpy.context.scene
candidate = next(obj for obj in scene.objects
                 if obj.type == "MESH" and "FusedSurface_Iter1444" in obj.name)
print("RENDER_MESH", candidate.name, len(candidate.data.vertices),
      len(candidate.data.uv_layers), len(candidate.vertex_groups))
scene.frame_set(9)
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.filepath = OUTPUT
bpy.ops.render.render(write_still=True)
print("SAVED", OUTPUT)
