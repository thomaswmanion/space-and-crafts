from pathlib import Path
from collections import defaultdict
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/rigged_character.glb"
OUT = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1347/Splat_Meshy_Iter1337_FaceSeated.glb"
FBX_OUT = OUT.with_suffix(".fbx")
OUT.parent.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(MODEL))
scene = bpy.context.scene
mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH" and obj.name == "char1")
armature = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh = mesh_obj.data
n = len(mesh.vertices)
parent = list(range(n))
rank = bytearray(n)

def find(i):
    while parent[i] != i:
        parent[i] = parent[parent[i]]
        i = parent[i]
    return i

def union(a, b):
    ra, rb = find(a), find(b)
    if ra == rb:
        return
    if rank[ra] < rank[rb]:
        ra, rb = rb, ra
    parent[rb] = ra
    if rank[ra] == rank[rb]:
        rank[ra] += 1

for edge in mesh.edges:
    union(edge.vertices[0], edge.vertices[1])
components = defaultdict(list)
for vertex in mesh.vertices:
    components[find(vertex.index)].append(vertex.index)

groups = {group.name: group for group in mesh_obj.vertex_groups}
face_group = groups.get("headfront") or groups.get("Head")
cap_group = groups.get("Head") or face_group
if face_group is None:
    raise RuntimeError("No Head/headfront weight group in imported Meshy rig")
reweighted_face = []
reweighted_cap = []
matrix = mesh_obj.matrix_world
component_report = []
for vertex_ids in components.values():
    pts = [matrix @ mesh.vertices[index].co for index in vertex_ids]
    center = sum(pts, Vector()) / len(pts)
    min_z = min(point.z for point in pts)
    max_z = max(point.z for point in pts)
    # The Meshy auto-rigger assigned much of the facial surface to shoulder
    # bones. Keep the front-face islands on the head-front control instead.
    face = center.y < -0.48 and center.z > 0.82 and abs(center.x) < 0.43
    # The backwards-cap crown is above the sphere and centered between arms.
    cap = not face and center.z > 1.48 and abs(center.x) < 0.34 and center.y > -0.42
    if not (face or cap):
        continue

    old_weights = defaultdict(float)
    for index in vertex_ids:
        vertex = mesh.vertices[index]
        for element in vertex.groups:
            old_name = next((name for name, group in groups.items() if group.index == element.group), str(element.group))
            old_weights[old_name] += element.weight
        for element in list(vertex.groups):
            mesh_obj.vertex_groups[element.group].remove([index])
    target = face_group if face else cap_group
    target.add(vertex_ids, 1.0, "REPLACE")
    (reweighted_face if face else reweighted_cap).extend(vertex_ids)
    component_report.append(("face" if face else "cap", len(vertex_ids), tuple(round(v, 3) for v in center),
                            sorted(old_weights.items(), key=lambda item: item[1], reverse=True)[:3]))

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
print("WEIGHT_REPAIR output", OUT.name, "components", len(components),
      "face_components", len(component_report) - sum(1 for row in component_report if row[0] == "cap"),
      "face_vertices", len(reweighted_face), "cap_components", sum(1 for row in component_report if row[0] == "cap"),
      "cap_vertices", len(reweighted_cap), "faces", len(mesh.polygons), "triangles", sum(max(0, len(p.vertices)-2) for p in mesh.polygons))
print("WEIGHT_REPAIR fbx", FBX_OUT.name)
for row in component_report:
    print("WEIGHT_REPAIR component", row)
