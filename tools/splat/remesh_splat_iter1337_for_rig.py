"""Create a non-destructive, textured GLB derivative below Meshy's rig limit.

The source Meshy export is intentionally left untouched. This script targets
295k triangles to leave margin below Meshy's 300k rigging cap, then exports a
new GLB with its materials and UVs. Run with Blender 5.x in background mode.
"""

from pathlib import Path
import bpy


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/Splat_Meshy_Iter1337.glb"
OUTPUT = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/Splat_Meshy_Iter1337_RigInput.glb"
TARGET_TRIANGLES = 295_000
RATIO = 0.65


def triangle_count(objects):
    return sum(len(obj.data.polygons) for obj in objects if obj.type == "MESH")


if not SOURCE.is_file():
    raise FileNotFoundError(SOURCE)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if not meshes:
    raise RuntimeError("Meshy GLB imported without any mesh objects")

before = triangle_count(meshes)
if before <= 300_000:
    raise RuntimeError(f"Source unexpectedly has only {before} triangles; do not needlessly decimate")

for obj in meshes:
    modifier = obj.modifiers.new(name="RigLimitDecimation", type="DECIMATE")
    modifier.decimate_type = "COLLAPSE"
    modifier.ratio = RATIO
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)

after = triangle_count(meshes)
if after > 300_000:
    raise RuntimeError(f"Decimated derivative still exceeds the rig limit: {after} triangles")
if after > TARGET_TRIANGLES:
    # Apply a second small reduction only if the first modifier left too little
    # safety margin. The source remains intact, so this is always reversible.
    ratio = max(0.01, TARGET_TRIANGLES / after * 0.99)
    for obj in meshes:
        modifier = obj.modifiers.new(name="RigLimitSafetyMargin", type="DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = ratio
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    after = triangle_count(meshes)
    if after > 300_000:
        raise RuntimeError(f"Safety pass still exceeds the rig limit: {after} triangles")

for obj in bpy.context.scene.objects:
    obj.select_set(obj.type in {"MESH", "EMPTY"})

OUTPUT.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.gltf(
    filepath=str(OUTPUT),
    export_format="GLB",
    use_selection=True,
    export_materials="EXPORT",
    export_image_format="AUTO",
    export_texcoords=True,
    export_normals=True,
    export_animations=False,
)

print(
    "SPLAT_RIG_INPUT "
    f"source_triangles={before} output_triangles={after} "
    f"mesh_objects={len(meshes)} output={OUTPUT}"
)
