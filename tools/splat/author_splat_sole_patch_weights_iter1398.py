"""Candidate-only, tapered boot-sole weights on the existing cohesive Meshy Splat mesh."""
import bpy
import json
import os
import numpy as np
from mathutils.kdtree import KDTree

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BASE_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1395"
SOURCE = BASE_DIR + r"\Splat_FootSupportGait_Iter1395.blend"
OUT_DIR = BASE_DIR + r"\Iter1398_SolePatchWeights"
REPORT_SOURCE = BASE_DIR + r"\Splat_Iter1395_FootSupportGaitReport.json"
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SOURCE)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
mesh = mesh_obj.data
with open(REPORT_SOURCE, "r", encoding="utf-8") as handle:
    source_report = json.load(handle)

# Contact candidate indices sampled across the rejected flat-ground gait. Use
# their bind-space neighborhood to affect only the sole/lower boot shell.
seed_by_side = {"Left": set(), "Right": set()}
for sample in source_report["frame_data"]:
    for side in ("Left", "Right"):
        for phase in ("before", "after"):
            point = sample[phase][side]
            seed_by_side[side].add(int(point["vertex"]))
coords = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
mesh.vertices.foreach_get("co", coords)
coords = coords.reshape(-1, 3)
world_matrix = np.asarray(mesh_obj.matrix_world, dtype=np.float64)
world_coords = coords @ world_matrix[:3, :3].T + world_matrix[:3, 3]
tree = KDTree(len(mesh.vertices))
for index, point in enumerate(world_coords):
    tree.insert(point, index)
tree.balance()

foot_groups = {side: (mesh_obj.vertex_groups[side + "Foot"], mesh_obj.vertex_groups[side + "ToeBase"])
               for side in ("Left", "Right")}
side_patches = {}
for side, seeds in seed_by_side.items():
    side_points = world_coords[np.asarray(sorted(seeds), dtype=np.int32)]
    distances = np.full(len(mesh.vertices), np.inf, dtype=np.float32)
    candidates = set()
    for point in side_points:
        for _, index, distance in tree.find_range(point, 0.045):
            candidates.add(index)
            distances[index] = min(distances[index], distance)
    selected = np.asarray(sorted(candidates), dtype=np.int32)
    # Avoid crossing into the other side or painting the torso while retaining
    # a soft shoulder-free falloff around each boot sole.
    side_sign = 1.0 if side == "Left" else -1.0
    selected = selected[(world_coords[selected, 0] * side_sign > 0.20)
                        & (world_coords[selected, 2] < 0.34)
                        & (world_coords[selected, 1] < -0.10)]
    side_patches[side] = (selected, distances[selected])

group_names = {group.index: group.name for group in mesh_obj.vertex_groups}
assignments = {group.name: {} for group in mesh_obj.vertex_groups}
changed = {side: 0 for side in foot_groups}
for side, (selected, distances) in side_patches.items():
    foot_group, toe_group = foot_groups[side]
    for vertex_index, distance in zip(selected.tolist(), distances.tolist()):
        vertex = mesh.vertices[vertex_index]
        old = {item.group: item.weight for item in vertex.groups}
        falloff = float(np.clip((0.045 - distance) / 0.030, 0.0, 1.0))
        if falloff <= 0.0:
            continue
        old_total = sum(old.values())
        if old_total <= 1e-8:
            continue
        old = {group: weight / old_total for group, weight in old.items()}
        desired = {foot_group.index: 0.76, toe_group.index: 0.24}
        blended = {group: weight * (1.0 - falloff) for group, weight in old.items()}
        for group, weight in desired.items():
            blended[group] = blended.get(group, 0.0) + weight * falloff
        total = sum(blended.values())
        normalized = {group: weight / total for group, weight in blended.items() if weight > 1e-6}
        # Quantize to byte weights as the source's Unity/FBX skin expects.
        raw = {group: weight * 255.0 for group, weight in normalized.items()}
        units = {group: int(np.floor(value)) for group, value in raw.items()}
        remainder = 255 - sum(units.values())
        order = sorted(raw, key=lambda group: raw[group] - units[group], reverse=True)
        for group in order[:max(0, remainder)]:
            units[group] += 1
        for group, amount in units.items():
            if amount > 0:
                assignments[group_names[group]].setdefault(amount, []).append(vertex_index)
        changed[side] += 1

all_indices = sorted({index for _, (indices, _) in side_patches.items() for index in indices.tolist()})
for group in mesh_obj.vertex_groups:
    group.remove(all_indices)
for group_name, buckets in assignments.items():
    group = mesh_obj.vertex_groups[group_name]
    for amount, indices in buckets.items():
        group.add(indices, amount / 255.0, "REPLACE")

probe = {
    "iteration": 1398,
    "source": SOURCE,
    "candidate": "Splat_Meshy_Iter1395_SolePatchWeights_Iter1398.blend",
    "method": "Taper existing vertex weights toward each matching Foot/ToeBase chain within 45 mm of sampled weighted sole vertices; maximum target 76/24, blended over 30 mm.",
    "vertex_counts": {side: int(len(values[0])) for side, values in side_patches.items()},
    "reweighted_vertex_counts": changed,
    "limitations": "Offline geometry/weight candidate only. Bounds are seed-neighborhood diagnostics, not evidence of contact or good animation. Must be evaluated in action poses on a ground plane and compared to the unmodified asset before export or any game integration.",
}
with open(os.path.join(OUT_DIR, "Splat_Iter1398_SolePatchWeightReport.json"), "w", encoding="utf-8") as handle:
    json.dump(probe, handle, indent=2)
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Splat_Meshy_Iter1395_SolePatchWeights_Iter1398.blend"))
print("ITER1398_SOLE_PATCH", json.dumps(probe, sort_keys=True))
