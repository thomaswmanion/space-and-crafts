"""Vectorized, read-only UV/color and existing limb-weight audit."""
import bpy
import json
import numpy as np

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BLEND = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1370\Splat_Meshy_Iter1365_RigBound_Iter1370.blend"
OUT = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1384\Iter1385_UVRegionReport.json"
bpy.ops.wm.open_mainfile(filepath=BLEND)
mesh_obj = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
mesh = mesh_obj.data
images = [node.image for material in mesh.materials if material and material.use_nodes
          for node in material.node_tree.nodes if node.type == "TEX_IMAGE" and node.image]
if not images:
    raise RuntimeError("No base-color texture found on remesh material.")
image = images[0]
if image.size[0] > 1024 or image.size[1] > 1024:
    image.scale(min(image.size[0], 1024), min(image.size[1], 1024))
width, height = image.size
pixels = np.empty(width * height * 4, dtype=np.float32)
image.pixels.foreach_get(pixels)
pixels = pixels.reshape(height, width, 4)

uv_layer = mesh.uv_layers.active
if uv_layer is None:
    raise RuntimeError("Mesh has no active UV layer.")
uv_flat = np.empty(len(mesh.loops) * 2, dtype=np.float32)
uv_layer.data.foreach_get("uv", uv_flat)
uv = uv_flat.reshape(-1, 2)
loop_vertices = np.empty(len(mesh.loops), dtype=np.int32)
mesh.loops.foreach_get("vertex_index", loop_vertices)
xy = np.floor(uv * np.array([width - 1, height - 1], dtype=np.float32)).astype(np.int32)
xy = np.clip(xy, [0, 0], [width - 1, height - 1])
rgb = pixels[xy[:, 1], xy[:, 0], :3]
r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
labels = np.full(len(mesh.loops), 3, dtype=np.int8)  # other
labels[(np.minimum(np.minimum(r, g), b) > .62) & (np.maximum(np.maximum(r, g), b) - np.minimum(np.minimum(r, g), b) < .22)] = 0
labels[(b > r * 1.35) & (b > g * .9) & (r < .45)] = 1
labels[np.maximum(np.maximum(r, g), b) < .16] = 2

nverts = len(mesh.vertices)
sample_counts = np.bincount(labels, minlength=4)
vote = np.bincount(labels.astype(np.int64) * nverts + loop_vertices, minlength=4 * nverts).reshape(4, nverts)
dominant = vote.argmax(axis=0)
names = {group.index: group.name for group in mesh_obj.vertex_groups}
limbs = {"LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightShoulder", "RightArm", "RightForeArm", "RightHand", "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"}
limb_weights = np.zeros(nverts, dtype=np.float32)
for vertex in mesh.vertices:
    limb_weights[vertex.index] = sum(item.weight for item in vertex.groups if names.get(item.group) in limbs)
categories = ["light_neutral", "violet", "dark_feature", "other"]
edge_vertices = np.empty(len(mesh.edges) * 2, dtype=np.int32)
mesh.edges.foreach_get("vertices", edge_vertices)
edge_vertices = edge_vertices.reshape(-1, 2)
parent = np.arange(nverts, dtype=np.int32)
rank = np.zeros(nverts, dtype=np.uint8)
def find_component(index):
    root = index
    while parent[root] != root:
        root = int(parent[root])
    while parent[index] != index:
        next_index = int(parent[index])
        parent[index] = root
        index = next_index
    return root
for a, b in edge_vertices:
    ra, rb = find_component(int(a)), find_component(int(b))
    if ra == rb:
        continue
    if rank[ra] < rank[rb]:
        ra, rb = rb, ra
    parent[rb] = ra
    if rank[ra] == rank[rb]:
        rank[ra] += 1
component_roots = np.fromiter((find_component(i) for i in range(nverts)), dtype=np.int32, count=nverts)
unique_roots, component_ids, component_sizes = np.unique(component_roots, return_inverse=True, return_counts=True)
component_colors = np.zeros((len(unique_roots), 4), dtype=np.int32)
np.add.at(component_colors, (component_ids, dominant), 1)
largest_component_indices = np.argsort(component_sizes)[::-1][:20]
component_report = [
    {
        "vertices": int(component_sizes[i]),
        "dominant_color_vertices": {categories[j]: int(component_colors[i, j]) for j in range(4)},
    }
    for i in largest_component_indices
]
by_category = {}
coords_flat = np.empty(nverts * 3, dtype=np.float32)
mesh.vertices.foreach_get("co", coords_flat)
coords = coords_flat.reshape(-1, 3)
group_weights = {name: np.zeros(nverts, dtype=np.float32) for name in names.values()}
for vertex in mesh.vertices:
    for item in vertex.groups:
        group_name = names.get(item.group)
        if group_name in group_weights:
            group_weights[group_name][vertex.index] = item.weight
for index, name in enumerate(categories):
    selected = dominant == index
    weights = limb_weights[selected]
    by_category[name] = {
        "vertices": int(selected.sum()),
        "low_limb_weight_lt_0_10": int((weights < .10).sum()),
        "mid_limb_weight_0_10_to_0_52": int(((weights >= .10) & (weights < .52)).sum()),
        "high_limb_weight_gte_0_52": int((weights >= .52).sum()),
        "mean_limb_weight": float(weights.mean()) if len(weights) else None,
        "mean_local_position": [float(value) for value in coords[selected].mean(axis=0)] if selected.any() else None,
        "bounds_local_position": {
            "min": [float(value) for value in coords[selected].min(axis=0)],
            "max": [float(value) for value in coords[selected].max(axis=0)],
        } if selected.any() else None,
        "mean_named_bone_weights": {
            bone: float(values[selected].mean())
            for bone, values in group_weights.items()
            if selected.any() and values[selected].mean() >= .01
        },
    }
report = {
    "mesh": mesh_obj.name,
    "vertices": nverts,
    "triangles": sum(len(poly.vertices) - 2 for poly in mesh.polygons),
    "texture": image.name,
    "texture_size": [width, height],
    "uv_loop_samples": int(len(mesh.loops)),
    "loop_color_samples": {categories[i]: int(sample_counts[i]) for i in range(4)},
    "dominant_vertex_color_and_limb_weight_bands": by_category,
    "connected_surface_components": int(len(unique_roots)),
    "largest_components": component_report,
    "classification_note": "Color labels match the Iter1380 thresholds, sampled from UV-mapped base color; this is a diagnostic, not a weight edit or visual approval.",
}
with open(OUT, "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
print("UV_REGION_REPORT", json.dumps(report, sort_keys=True))
