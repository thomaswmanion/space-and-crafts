"""Create an isolated fused-surface derivative of the existing paid spider rig.

This keeps the source GLB and Iter1409 rig untouched. The result is a local
candidate only; it is not imported into Unity or gameplay.
"""
import bpy
import os


ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
SOURCE = os.path.join(
    ROOT,
    "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/"
    "20261101_rainbow-spider-rig-candidate-iter1409/RainbowSpider_RigCandidate.blend",
)
OUTPUT = os.path.join(
    ROOT,
    "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/"
    "20261101_rainbow-spider-rig-candidate-iter1444/RainbowSpider_RigCandidate_Iter1444.blend",
)

bpy.ops.wm.open_mainfile(filepath=SOURCE)
mesh = next((obj for obj in bpy.context.scene.objects
             if obj.type == "MESH" and obj.vertex_groups), None)
if mesh is None:
    raise RuntimeError("No weighted spider mesh found in Iter1409 blend")

print("SOURCE", mesh.name, len(mesh.data.vertices), len(mesh.data.polygons),
      "materials", len(mesh.data.materials), "uv", len(mesh.data.uv_layers),
      "groups", len(mesh.vertex_groups))

candidate = mesh.copy()
candidate.data = mesh.data.copy()
candidate.name = "RainbowKingdomSpider_FusedSurface_Iter1444"
bpy.context.collection.objects.link(candidate)
mesh.hide_render = True
mesh.hide_viewport = True
mesh.hide_set(True)

bpy.ops.object.select_all(action="DESELECT")
candidate.select_set(True)
bpy.context.view_layer.objects.active = candidate
remesh = candidate.modifiers.new("Fused surface preview", "REMESH")
remesh.mode = "VOXEL"
remesh.voxel_size = 0.008
remesh.use_remove_disconnected = False
bpy.ops.object.modifier_apply(modifier=remesh.name)

print("CANDIDATE", candidate.name, len(candidate.data.vertices), len(candidate.data.polygons),
      "materials", len(candidate.data.materials), "uv", len(candidate.data.uv_layers),
      "groups", len(candidate.vertex_groups),
      "armature_modifiers", sum(mod.type == "ARMATURE" for mod in candidate.modifiers))

os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT)
print("SAVED", OUTPUT)
