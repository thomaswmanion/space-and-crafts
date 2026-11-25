from collections import defaultdict
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1357/Splat_Meshy_Iter1337_BodyRooted.fbx"
OUT_DIR = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1363"
OUT_FBX = OUT_DIR / "Splat_Meshy_Iter1337_FootSeated.fbx"
OUT_GLB = OUT_DIR / "Splat_Meshy_Iter1337_FootSeated.glb"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE))
scene = bpy.context.scene
mesh = next(obj for obj in scene.objects if obj.type == "MESH" and obj.vertex_groups)
armature = next(obj for obj in scene.objects if obj.type == "ARMATURE")

for name in ("LeftFoot", "RightFoot"):
    if mesh.vertex_groups.get(name) is None:
        raise RuntimeError(f"Candidate has no {name} group")

parent = list(range(len(mesh.data.vertices)))
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

for edge in mesh.data.edges:
    union(*edge.vertices)
components = defaultdict(list)
for vertex in mesh.data.vertices:
    components[find(vertex.index)].append(vertex.index)

def describe(indices):
    coords = [mesh.matrix_world @ mesh.data.vertices[i].co for i in indices]
    low = tuple(min(point[a] for point in coords) for a in range(3))
    high = tuple(max(point[a] for point in coords) for a in range(3))
    center = tuple(sum(point[a] for point in coords) / len(coords) for a in range(3))
    return low, high, center

# The two lower, forward-facing boot shells on each side are disconnected
# surfaces in the commissioned mesh. Match only their compact, near-floor
# components; fail closed if the expected pair on either side is ambiguous.
boots = {"LeftFoot": [], "RightFoot": []}
for indices in components.values():
    if not 400 <= len(indices) <= 1800:
        continue
    low, high, center = describe(indices)
    if center[1] >= -0.25 or high[2] > 0.27 or low[2] > 0.03:
        continue
    if center[0] >= 0.33 and center[2] < 0.20:
        boots["LeftFoot"].append((indices, low, high, center))
    elif center[0] <= -0.40 and center[2] < 0.20:
        boots["RightFoot"].append((indices, low, high, center))

for side, regions in boots.items():
    print("BOOT_REGION_PREFLIGHT", side, len(regions), [
        (len(indices), tuple(round(v, 4) for v in center),
         tuple((round(low[a], 4), round(high[a], 4)) for a in range(3)))
        for indices, low, high, center in regions])
    if len(regions) != 2:
        raise RuntimeError(f"Refusing weight repair: expected exactly two {side} shoe shells")

group_by_index = {group.index: group.name for group in mesh.vertex_groups}
for side, regions in boots.items():
    indices = [index for region, *_ in regions for index in region]
    totals_before = defaultdict(float)
    for index in indices:
        for membership in mesh.data.vertices[index].groups:
            totals_before[group_by_index[membership.group]] += membership.weight
    print("BOOT_WEIGHTS_BEFORE", side, dict(totals_before))
    for group in mesh.vertex_groups:
        group.remove(indices)
    mesh.vertex_groups[side].add(indices, 1.0, "REPLACE")
    print("BOOT_WEIGHTS_AFTER", side, "vertices", len(indices), "target", side, "weight", 1.0)

for polygon in mesh.data.polygons:
    polygon.use_smooth = True

OUT_DIR.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
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
print("FOOT_SEATED_OUTPUT", OUT_GLB, OUT_FBX)
