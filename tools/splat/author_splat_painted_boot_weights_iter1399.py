"""Candidate-only UV and height masked paint-over of Splat's existing boot weighting."""
import bpy
import json
import os
import numpy as np

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BASE = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1395"
SOURCE = BASE + r"\Splat_FootSupportGait_Iter1395.blend"
TEXTURE = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\Splat_300k_Remesh_Iter1365_textures\base_color.png"
OUT_DIR = BASE + r"\Iter1399_PaintedBootWeights"
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SOURCE)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
mesh = mesh_obj.data

image = bpy.data.images.load(TEXTURE, check_existing=True)
width, height = image.size
pixels = np.empty(width * height * 4, dtype=np.float32)
image.pixels.foreach_get(pixels)
pixels = pixels.reshape(height, width, 4)
uv_layer = mesh.uv_layers.active
uv = np.empty((len(mesh.loops), 2), dtype=np.float32)
uv_layer.data.foreach_get("uv", uv.reshape(-1))
loop_vertex = np.empty(len(mesh.loops), dtype=np.int32)
mesh.loops.foreach_get("vertex_index", loop_vertex)
x = np.clip(np.floor(uv[:, 0] * (width - 1)).astype(np.int32), 0, width - 1)
y = np.clip(np.floor(uv[:, 1] * (height - 1)).astype(np.int32), 0, height - 1)
rgb = pixels[y, x, :3]
violet_loop = (rgb[:, 2] > rgb[:, 0] * 1.35) & (rgb[:, 2] > rgb[:, 1] * 0.9) & (rgb[:, 0] < 0.45)
violet_votes = np.bincount(loop_vertex, weights=violet_loop.astype(np.float32), minlength=len(mesh.vertices))
loop_counts = np.bincount(loop_vertex, minlength=len(mesh.vertices))
violet_fraction = violet_votes / np.maximum(loop_counts, 1)

local = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
mesh.vertices.foreach_get("co", local)
local = local.reshape(-1, 3)
matrix = np.asarray(mesh_obj.matrix_world, dtype=np.float64)
world = local @ matrix[:3, :3].T + matrix[:3, 3]
old_weights = np.zeros(len(mesh.vertices), dtype=np.float32)
for vertex in mesh.vertices:
    old_weights[vertex.index] = sum(item.weight for item in vertex.groups
                                     if rig.data.bones[mesh_obj.vertex_groups[item.group].name].name.endswith("Foot")
                                     or rig.data.bones[mesh_obj.vertex_groups[item.group].name].name.endswith("ToeBase"))

side_masks = {
    "Left": (world[:, 0] > 0.24),
    "Right": (world[:, 0] < -0.24),
}
target_groups = {}
for side in side_masks:
    target_groups[side] = (mesh_obj.vertex_groups[side + "Foot"], mesh_obj.vertex_groups[side + "ToeBase"])

groups_by_index = {group.index: group.name for group in mesh_obj.vertex_groups}
assignments = {group.name: {} for group in mesh_obj.vertex_groups}
counts = {}
for side, side_mask in side_masks.items():
    # Restrict paint-over to the purple boot/lower-leg surface and fade it out
    # above ankle height; the white orb and face/cap remain untouched.
    selected = np.flatnonzero(side_mask & (world[:, 2] < 0.42) & (world[:, 1] < -0.08)
                              & (violet_fraction >= 0.55))
    foot_group, toe_group = target_groups[side]
    for vertex_index in selected.tolist():
        height_t = float(np.clip((0.40 - world[vertex_index, 2]) / 0.16, 0.0, 1.0))
        if height_t <= 0:
            continue
        vertex = mesh.vertices[vertex_index]
        old = {item.group: item.weight for item in vertex.groups}
        total = sum(old.values())
        if total <= 1e-8:
            continue
        old = {group: weight / total for group, weight in old.items()}
        # Toe side follows the rig's asymmetric longitudinal axis: left toes +X,
        # right toes -X. Blend naturally from ankle-foot into toe tip.
        normalized_x = float(np.clip((abs(world[vertex_index, 0]) - 0.35) / 0.20, 0.0, 1.0))
        toe_share = 0.12 + 0.38 * normalized_x
        desired = {foot_group.index: 1.0 - toe_share, toe_group.index: toe_share}
        blend = height_t * 0.90
        mixed = {group: weight * (1.0 - blend) for group, weight in old.items()}
        for group, weight in desired.items():
            mixed[group] = mixed.get(group, 0.0) + weight * blend
        total = sum(mixed.values())
        mixed = {group: weight / total for group, weight in mixed.items() if weight > 1e-6}
        raw = {group: weight * 255.0 for group, weight in mixed.items()}
        units = {group: int(np.floor(weight)) for group, weight in raw.items()}
        remainder = 255 - sum(units.values())
        order = sorted(raw, key=lambda group: raw[group] - units[group], reverse=True)
        for group in order[:max(0, remainder)]:
            units[group] += 1
        for group, amount in units.items():
            if amount > 0:
                assignments[groups_by_index[group]].setdefault(amount, []).append(vertex_index)
    counts[side] = int(len(selected))

all_reweighted = sorted({index for group_buckets in assignments.values()
                         for indices in group_buckets.values() for index in indices})
for group in mesh_obj.vertex_groups:
    group.remove(all_reweighted)
for name, buckets in assignments.items():
    group = mesh_obj.vertex_groups[name]
    for amount, indices in buckets.items():
        group.add(indices, amount / 255.0, "REPLACE")

report = {
    "iteration": 1399,
    "source": SOURCE,
    "mask": "UV base-color purple >=55%; Z<0.42m; forward Y<-0.08m; side-separated X; height taper above ankle.",
    "changed_vertices": counts,
    "total_changed_vertices": len(all_reweighted),
    "mean_foot_chain_weight_before": {side: float(old_weights[np.flatnonzero(mask & (world[:, 2] < 0.42) & (world[:, 1] < -0.08) & (violet_fraction >= .55))].mean()) for side, mask in side_masks.items()},
    "limitations": "Candidate-only weight edit. The paint mask may include lower leg/boot junction; requires matched action renders and flat-plane contact comparison before acceptance.",
}
with open(os.path.join(OUT_DIR, "Splat_Iter1399_PaintedBootWeightReport.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Splat_Meshy_PaintedBootWeights_Iter1399.blend"))
print("ITER1399_BOOT_WEIGHTS", json.dumps(report, sort_keys=True))
