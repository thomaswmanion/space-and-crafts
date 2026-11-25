"""Measure isolated Meshy clip sole proxies against a flat bind-pose plane."""
import json
import os
import sys

import bpy
from mathutils import Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if len(ARGS) < 3:
    raise RuntimeError("Usage: <fbx> <action substring> <report json>")
FBX, ACTION_MATCH, REPORT = ARGS[:3]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=True)
scene = bpy.context.scene
mesh_objs = [obj for obj in scene.objects if obj.type == "MESH"]
armatures = [obj for obj in scene.objects if obj.type == "ARMATURE"]
if len(mesh_objs) != 1 or len(armatures) != 1:
    raise RuntimeError(f"Expected one skinned mesh and armature; found {len(mesh_objs)} meshes, {len(armatures)} armatures")
obj = mesh_objs[0]
armature = armatures[0]
action = next((item for item in bpy.data.actions if ACTION_MATCH.lower() in item.name.lower()), None)
if action is None:
    raise RuntimeError(f"Action {ACTION_MATCH!r} not found")
armature.animation_data.action = action

name_to_index = {group.name: group.index for group in obj.vertex_groups}
foot_groups = {
    "left": [name_to_index[name] for name in ("LeftFoot", "LeftToeBase") if name in name_to_index],
    "right": [name_to_index[name] for name in ("RightFoot", "RightToeBase") if name in name_to_index],
}
if not foot_groups["left"] or not foot_groups["right"]:
    raise RuntimeError(f"No bilateral foot groups found: {name_to_index}")

weighted_indices = {side: [] for side in foot_groups}
threshold = 0.25
for vertex in obj.data.vertices:
    group_weights = {assignment.group: assignment.weight for assignment in vertex.groups}
    for side, indices in foot_groups.items():
        weight = sum(group_weights.get(index, 0.0) for index in indices)
        if weight >= threshold:
            weighted_indices[side].append(vertex.index)

start, end = action.frame_range
sample_frames = sorted(set(round(start + (end - start) * t) for t in (0.0, 0.24, 0.50, 0.76, 1.0)))
rows = []
all_world_heights = []
for frame in sample_frames:
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    baked = evaluated.to_mesh()
    world_matrix = evaluated.matrix_world.copy()
    points = [world_matrix @ vertex.co for vertex in baked.vertices]
    all_world_heights.extend(point.y for point in points)
    foot_bottoms = {}
    for side, indices in weighted_indices.items():
        foot_bottoms[side] = min(points[index].y for index in indices)
    evaluated.to_mesh_clear()
    rows.append({"frame": frame, "foot_bottom_y": foot_bottoms,
                 "body_min_y": min(point.y for point in points),
                 "body_max_y": max(point.y for point in points),
                 "body_height": max(point.y for point in points) - min(point.y for point in points)})

floor_y = min(rows[0]["foot_bottom_y"].values())
for row in rows:
    row["foot_clearance_vs_start_floor"] = {
        side: bottom - floor_y for side, bottom in row["foot_bottom_y"].items()
    }

report = {
    "source": FBX,
    "action": action.name,
    "frame_range": list(action.frame_range),
    "sample_frames": sample_frames,
    "foot_weight_threshold": threshold,
    "weighted_vertex_counts": {side: len(indices) for side, indices in weighted_indices.items()},
    "reference_plane_y": floor_y,
    "samples": rows,
    "limitations": [
        "Offline FBX skin evaluation only; no Unity humanoid retarget, CharacterController, platform collider, or in-game offsets.",
        "The reference plane is set from the lowest weighted sole at the first sampled frame; positive clearance is not itself a failure during swing.",
    ],
}
os.makedirs(os.path.dirname(REPORT), exist_ok=True)
with open(REPORT, "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
print("SPLAT_MESHY_SOLE_AUDIT", json.dumps({
    "action": action.name, "weighted_vertex_counts": report["weighted_vertex_counts"],
    "samples": [{"frame": row["frame"], "clearance": row["foot_clearance_vs_start_floor"], "body_height": row["body_height"]} for row in rows],
    "report": REPORT,
}))
