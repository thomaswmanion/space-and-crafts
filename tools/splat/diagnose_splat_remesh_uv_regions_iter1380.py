"""Read-only report of Splat remesh UV texture classes and skin weights."""
import bpy
from array import array
from collections import Counter, defaultdict

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BLEND = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1370\Splat_Meshy_Iter1365_RigBound_Iter1370.blend"
bpy.ops.wm.open_mainfile(filepath=BLEND)
mesh_obj = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
mesh = mesh_obj.data
images = []
for material in mesh.materials:
    if material and material.use_nodes:
        images.extend(node.image for node in material.node_tree.nodes if node.type == "TEX_IMAGE" and node.image)
if not images:
    raise RuntimeError("No base-color texture found on remesh material.")
image = images[0]
image.scale(1024, 1024)
width, height = image.size
pixels = array("f", [0.0]) * (width * height * 4)
image.pixels.foreach_get(pixels)
uv_data = mesh.uv_layers.active.data if mesh.uv_layers.active else None
if uv_data is None:
    raise RuntimeError("Mesh has no active UV layer.")

names = {group.index: group.name for group in mesh_obj.vertex_groups}
limb_names = {"LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightShoulder", "RightArm", "RightForeArm", "RightHand", "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"}
counts = Counter()
per_vertex = defaultdict(Counter)
per_vertex_limb = defaultdict(float)
samples = 0
for polygon in mesh.polygons:
    for loop_index in polygon.loop_indices:
        uv = uv_data[loop_index].uv
        x = max(0, min(width - 1, int(uv.x * (width - 1))))
        y = max(0, min(height - 1, int(uv.y * (height - 1))))
        offset = (y * width + x) * 4
        r, g, b = pixels[offset], pixels[offset + 1], pixels[offset + 2]
        if min(r, g, b) > 0.62 and max(r, g, b) - min(r, g, b) < 0.22:
            color_class = "light_neutral"
        elif b > r * 1.35 and b > g * 0.9 and r < 0.45:
            color_class = "violet"
        elif max(r, g, b) < 0.16:
            color_class = "dark_feature"
        else:
            color_class = "other"
        vertex = mesh.loops[loop_index].vertex_index
        per_vertex[vertex][color_class] += 1
        samples += 1

for vertex in mesh.vertices:
    weights = 0.0
    for assignment in vertex.groups:
        if names.get(assignment.group) in limb_names:
            weights += assignment.weight
    per_vertex_limb[vertex.index] = weights

for vertex_index, classes in per_vertex.items():
    category = classes.most_common(1)[0][0]
    counts[category] += 1
    counts[f"{category}_low_limb"] += int(per_vertex_limb[vertex_index] < 0.10)
    counts[f"{category}_mid_limb"] += int(0.10 <= per_vertex_limb[vertex_index] < 0.52)
    counts[f"{category}_high_limb"] += int(per_vertex_limb[vertex_index] >= 0.52)

print("UV_REGION_REPORT", {"mesh": mesh_obj.name, "vertices": len(mesh.vertices), "uv_loop_samples": samples, "texture": image.name, "size": [width, height], "vertex_classes_and_limb_weight_bands": dict(counts)})
