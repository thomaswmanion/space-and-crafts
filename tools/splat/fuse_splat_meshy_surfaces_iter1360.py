from collections import defaultdict
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/rigged_character.glb"
OUT_DIR = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1360"
OUT_GLB = OUT_DIR / "Splat_Meshy_Iter1337_Fused.glb"
OUT_FBX = OUT_DIR / "Splat_Meshy_Iter1337_Fused.fbx"
VOXEL_SIZE = 0.006

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
scene = bpy.context.scene
mesh = next(obj for obj in scene.objects if obj.type == "MESH" and obj.name == "char1")
body = next(obj for obj in scene.objects if obj.type == "MESH" and obj.name == "Icosphere")
armature = next(obj for obj in scene.objects if obj.type == "ARMATURE")
armature.data.pose_position = "REST"
if armature.animation_data:
    armature.animation_data_clear()

# Fail closed unless the auxiliary sphere is a truly skin-bound visual body.
# The imported GLB's current Icosphere is an unweighted proxy centered on the
# origin and extends below char1's visible sole baseline; fusing it would add
# a large blob beneath the feet. Never generate a derivative from it.
char1_min_z = min((mesh.matrix_world @ v.co).z for v in mesh.data.vertices)
char1_max_z = max((mesh.matrix_world @ v.co).z for v in mesh.data.vertices)
body_min_z = min((body.matrix_world @ v.co).z for v in body.data.vertices)
body_max_z = max((body.matrix_world @ v.co).z for v in body.data.vertices)
print("FUSION_PREFLIGHT", "char1_z", (char1_min_z, char1_max_z),
      "secondary_z", (body_min_z, body_max_z),
      "secondary_groups", len(body.vertex_groups),
      "secondary_materials", len(body.data.materials))
if not body.vertex_groups or not body.data.materials or body_min_z < char1_min_z - 0.05:
    raise RuntimeError("Refusing fusion: Icosphere is an unskinned/unshaded proxy outside the character sole bounds")

# Keep a source copy as a nearest-surface reference for UVs and skin weights.
body.data.materials.clear()
body.data.materials.append(mesh.data.materials[0])
hips = mesh.vertex_groups.get("Hips")
if hips is None:
    raise RuntimeError("Meshy source rig has no Hips group")
body_hips = body.vertex_groups.new(name="Hips")
body_hips.add(list(range(len(body.data.vertices))), 1.0, "REPLACE")
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = mesh
bpy.ops.object.join()
mesh.name = "SplatFusionReference"
source = mesh

target = source.copy()
target.data = source.data.copy()
target.name = "SplatFusedCandidate"
scene.collection.objects.link(target)
for group in source.vertex_groups:
    if target.vertex_groups.get(group.name) is None:
        target.vertex_groups.new(name=group.name)
for modifier in list(target.modifiers):
    target.modifiers.remove(modifier)

remesh = target.modifiers.new("VoxelFuse", "REMESH")
remesh.mode = "VOXEL"
remesh.voxel_size = VOXEL_SIZE
remesh.use_smooth_shade = True
bpy.context.view_layer.objects.active = target
target.select_set(True)
source.select_set(False)
bpy.ops.object.modifier_apply(modifier=remesh.name)

# Transfer the source surface's PBR UVs and skin weights onto the fused topology.
transfer = target.modifiers.new("SourceUVAndSkinTransfer", "DATA_TRANSFER")
transfer.object = source
transfer.use_vert_data = True
transfer.data_types_verts = {"VGROUP_WEIGHTS"}
transfer.vert_mapping = "POLYINTERP_NEAREST"
transfer.layers_vgroup_select_src = "ALL"
transfer.layers_vgroup_select_dst = "NAME"
transfer.use_loop_data = True
transfer.data_types_loops = {"UV"}
transfer.loop_mapping = "POLYINTERP_NEAREST"
transfer.layers_uv_select_src = "ALL"
transfer.layers_uv_select_dst = "NAME"
transfer.mix_mode = "REPLACE"
transfer.mix_factor = 1.0
bpy.ops.object.modifier_apply(modifier=transfer.name)

# Keep the single commissioned material and armature, now bound to one fused shell.
target.data.materials.clear()
target.data.materials.append(source.data.materials[0])
arm = target.modifiers.new("SplatMeshyArmature", "ARMATURE")
arm.object = armature
arm.use_vertex_groups = True
for polygon in target.data.polygons:
    polygon.use_smooth = True

totals = []
target_hips = target.vertex_groups.get("Hips")
if target_hips is None:
    raise RuntimeError("Fused candidate lost its Hips vertex group")
for vertex in target.data.vertices:
    weight_sum = sum(item.weight for item in vertex.groups)
    if weight_sum > 0.0001:
        totals.append(weight_sum)
    else:
        target_hips.add([vertex.index], 1.0, "REPLACE")

# Count disconnected topological islands after the union remesh.
parent = list(range(len(target.data.vertices)))
rank = bytearray(len(parent))
def find(index):
    while parent[index] != index:
        parent[index] = parent[parent[index]]
        index = parent[index]
    return index
def union(a, b):
    a, b = find(a), find(b)
    if a == b:
        return
    if rank[a] < rank[b]:
        a, b = b, a
    parent[b] = a
    if rank[a] == rank[b]:
        rank[a] += 1
for edge in target.data.edges:
    union(*edge.vertices)
components = len({find(v.index) for v in target.data.vertices})
print("FUSED_CANDIDATE", "vertices", len(target.data.vertices), "faces", len(target.data.polygons),
      "islands", components, "uvs", [uv.name for uv in target.data.uv_layers],
      "groups", [group.name for group in target.vertex_groups],
      "unweighted", sum(1 for value in totals if value < 0.5))
print("FUSED_SKIN_WEIGHT_SUM", "min", round(min(totals), 3) if totals else None,
      "max", round(max(totals), 3) if totals else None)

OUT_DIR.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="DESELECT")
target.select_set(True)
armature.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.export_scene.gltf(filepath=str(OUT_GLB), export_format="GLB", use_selection=True,
                          export_skins=True, export_materials="EXPORT", export_texcoords=True,
                          export_normals=True, export_apply=False, export_image_format="AUTO")
bpy.ops.export_scene.fbx(filepath=str(OUT_FBX), use_selection=True,
                         object_types={"ARMATURE", "MESH"}, apply_unit_scale=True,
                         apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=False,
                         axis_forward="-Z", axis_up="Y", add_leaf_bones=False,
                         use_mesh_modifiers=False, bake_anim=False, path_mode="COPY",
                         embed_textures=True)
print("FUSED_OUTPUT", OUT_GLB, OUT_FBX)
