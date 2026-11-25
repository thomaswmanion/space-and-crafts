from pathlib import Path
from collections import defaultdict
import math
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/rigged_character.glb"
OUT_DIR = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1357"
OUT = OUT_DIR / "Splat_Meshy_Iter1337_BodyRooted.glb"
FBX_OUT = OUT.with_suffix(".fbx")
OUT_DIR.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(MODEL))
scene = bpy.context.scene
mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH" and obj.name == "char1")
armature = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh = mesh_obj.data
groups = {group.name: group for group in mesh_obj.vertex_groups}
hips = groups.get("Hips")
head = groups.get("Head")
face_group = groups.get("headfront") or head
if hips is None or head is None or face_group is None:
    raise RuntimeError("Expected Hips, Head, and headfront groups in the Meshy rig")

face_ids = []
cap_ids = []
body_blend = []
def replace_weights(vertex_id, assignments):
    vertex = mesh.vertices[vertex_id]
    for influence in list(vertex.groups):
        mesh_obj.vertex_groups[influence.group].remove([vertex_id])
    for name, weight in assignments.items():
        if weight > 0.0001:
            groups[name].add([vertex_id], float(weight), "REPLACE")

for vertex in mesh.vertices:
    point = mesh_obj.matrix_world @ vertex.co
    x, y, z = point.x, point.y, point.z
    # Fragmented front-face and backwards-cap pieces remain seated on their intended controls.
    is_face = y < -0.48 and z > 0.82 and abs(x) < 0.43
    is_cap = not is_face and z > 1.48 and abs(x) < 0.34 and y > -0.42
    if is_face:
        replace_weights(vertex.index, {"headfront": 1.0})
        face_ids.append(vertex.index)
        continue
    if is_cap:
        replace_weights(vertex.index, {"Head": 1.0})
        cap_ids.append(vertex.index)
        continue

    # Keep the center of Splat's round body rigid and smoothly blend to the original
    # limb weights near the silhouette. This prevents shoulder/leg motion from pinching
    # the entire torso while preserving the existing limb transition at the attachment.
    radius = math.sqrt(x * x + y * y + (z - 0.75) * (z - 0.75))
    excluded_limb = (abs(x) > 0.78 and 0.24 < z < 1.58) or (z < 0.32 and abs(x) > 0.16)
    if radius < 0.94 and not excluded_limb:
        body_weight = max(0.0, min(1.0, (0.94 - radius) / 0.28))
        if body_weight > 0.02:
            old = defaultdict(float)
            for influence in vertex.groups:
                name = next((n for n, group in groups.items() if group.index == influence.group), None)
                if name:
                    old[name] += influence.weight * (1.0 - body_weight)
            old["Hips"] += body_weight
            total = sum(old.values()) or 1.0
            replace_weights(vertex.index, {name: weight / total for name, weight in old.items()})
            body_blend.append((vertex.index, body_weight))

armature.data.pose_position = "REST"
if armature.animation_data:
    armature.animation_data_clear()
bpy.ops.object.select_all(action="DESELECT")
armature.select_set(True)
mesh_obj.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.export_scene.gltf(
    filepath=str(OUT), export_format="GLB", use_selection=True, export_animations=False,
    export_skins=True, export_materials="EXPORT", export_texcoords=True, export_normals=True,
    export_apply=False, export_image_format="AUTO")
bpy.ops.export_scene.fbx(
    filepath=str(FBX_OUT), use_selection=True, object_types={"ARMATURE", "MESH"},
    apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=False,
    axis_forward="-Z", axis_up="Y", add_leaf_bones=False, use_mesh_modifiers=False,
    bake_anim=False, path_mode="COPY", embed_textures=True)
print("BODY_WEIGHT_REPAIR mesh", len(mesh.vertices), "vertices", len(mesh.polygons), "faces")
print("BODY_WEIGHT_REPAIR rigid_face_vertices", len(face_ids), "rigid_cap_vertices", len(cap_ids),
      "body_blended_vertices", len(body_blend), "avg_body_blend", round(sum(w for _, w in body_blend) / max(1, len(body_blend)), 3))
print("BODY_WEIGHT_REPAIR outputs", OUT.name, FBX_OUT.name)
