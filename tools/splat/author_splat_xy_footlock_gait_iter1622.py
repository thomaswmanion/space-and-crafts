"""Isolated no-credit XY stance-lock probe on Splat's existing Meshy rig.

Loads only the rejected Iter1395 gait, keeps its mesh/armature and action
isolated, then adds per-frame thigh/knee rotation keys to hold the measured
weighted sole probe near a fixed world-X/support-Z anchor during each stance.
This is an offline candidate, not a gameplay integration.
"""
import bpy
import json
import math
import os
import numpy as np
from mathutils import Quaternion

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
SOURCE_BLEND = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1395\Splat_FootSupportGait_Iter1395.blend"
SOURCE_REPORT = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1395\Splat_Iter1395_FootSupportGaitReport.json"
OUT_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1622"
os.makedirs(OUT_DIR, exist_ok=True)

with open(SOURCE_REPORT, "r", encoding="utf-8") as handle:
    source_report = json.load(handle)
bpy.ops.wm.open_mainfile(filepath=SOURCE_BLEND)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
mesh = mesh_obj.data
groups = {g.name: g.index for g in mesh_obj.vertex_groups}
sole_vertices = {}
for side in ("Left", "Right"):
    foot, toe = groups[side + "Foot"], groups[side + "ToeBase"]
    ids = np.asarray([
        vertex.index for vertex in mesh.vertices
        if any(item.group in (foot, toe) and item.weight >= 0.25 for item in vertex.groups)
    ], dtype=np.int32)
    if not len(ids):
        raise RuntimeError(f"No weighted sole vertices for {side}")
    sole_vertices[side] = ids

depsgraph = bpy.context.evaluated_depsgraph_get()
def evaluated_world_points():
    bpy.context.view_layer.update()
    depsgraph.update()
    evaluated = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)
    try:
        coords = np.empty(len(evaluated_mesh.vertices) * 3, dtype=np.float32)
        evaluated_mesh.vertices.foreach_get("co", coords)
        coords = coords.reshape(-1, 3)
        matrix = np.asarray(evaluated.matrix_world, dtype=np.float64)
        return coords @ matrix[:3, :3].T + matrix[:3, 3]
    finally:
        evaluated.to_mesh_clear()

def measure():
    points = evaluated_world_points()
    result = {}
    for side, ids in sole_vertices.items():
        vertex = int(ids[np.argmin(points[ids, 2])])
        result[side] = {
            "point": [float(x) for x in points[vertex]],
            "vertex": vertex,
            "weighted_vertices": int(len(ids)),
            "min_z_m": float(points[vertex, 2]),
        }
    return result

stance_by_frame = {int(row["frame"]): row["stance"] for row in source_report["frame_data"]}
episodes = []
for frame in range(1, 34):
    side = stance_by_frame[frame]
    if frame == 1 or side != stance_by_frame[frame - 1]:
        episodes.append({"side": side, "start": frame, "end": frame})
    else:
        episodes[-1]["end"] = frame
episode_index = {}
for index, episode in enumerate(episodes):
    for frame in range(episode["start"], episode["end"] + 1):
        episode_index[frame] = index

MAX_ABS_DELTA_RAD = 0.35
MAX_STEP_RAD = 0.12
JACOBIAN_EPS_RAD = 0.003
DAMPING = 2.5e-4
samples = []
for frame in range(1, 34):
    scene.frame_set(frame)
    side = stance_by_frame[frame]
    episode = episodes[episode_index[frame]]
    measured = measure()
    if frame == episode["start"]:
        episode["anchor_vertex"] = measured[side]["vertex"]
        episode["anchor_xz_m"] = [measured[side]["point"][0], measured[side]["point"][2]]
    target = np.asarray(episode["anchor_xz_m"], dtype=np.float64)
    probe_vertex = int(episode["anchor_vertex"])
    thigh = rig.pose.bones[side + "UpLeg"]
    knee = rig.pose.bones[side + "Leg"]
    base_thigh = thigh.rotation_quaternion.copy()
    base_knee = knee.rotation_quaternion.copy()
    delta = np.zeros(2, dtype=np.float64)

    def apply(candidate):
        thigh.rotation_quaternion = base_thigh @ Quaternion((1, 0, 0), float(candidate[0]))
        knee.rotation_quaternion = base_knee @ Quaternion((1, 0, 0), float(candidate[1]))
        bpy.context.view_layer.update()
        points = evaluated_world_points()
        point = points[probe_vertex]
        return np.asarray((point[0], point[2]), dtype=np.float64), points

    before_point, _ = apply(delta)
    before_error = before_point - target
    for _ in range(22):
        current, _ = apply(delta)
        error = current - target
        if float(np.linalg.norm(error)) <= 0.002:
            break
        jacobian = np.zeros((2, 2), dtype=np.float64)
        for axis in range(2):
            probe = delta.copy()
            probe[axis] += JACOBIAN_EPS_RAD
            shifted, _ = apply(probe)
            jacobian[:, axis] = (shifted - current) / JACOBIAN_EPS_RAD
        normal = jacobian.T @ jacobian + DAMPING * np.eye(2)
        try:
            change = np.linalg.solve(normal, -(jacobian.T @ error))
        except np.linalg.LinAlgError:
            break
        change = np.clip(change, -MAX_STEP_RAD, MAX_STEP_RAD)
        proposal = np.clip(delta + change, -MAX_ABS_DELTA_RAD, MAX_ABS_DELTA_RAD)
        if np.linalg.norm(proposal - delta) < 1e-5:
            break
        delta = proposal

    final_point, points = apply(delta)
    residual = final_point - target
    thigh.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=side + "UpLeg")
    knee.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=side + "Leg")
    minima = {}
    for name, ids in sole_vertices.items():
        vertex = int(ids[np.argmin(points[ids, 2])])
        minima[name] = {
            "point": [float(x) for x in points[vertex]],
            "vertex": vertex,
            "min_z_m": float(points[vertex, 2]),
        }
    samples.append({
        "frame": frame,
        "stance": side,
        "episode_start": episode["start"],
        "probe_vertex": probe_vertex,
        "target_xz_m": [float(x) for x in target],
        "probe_before_xz_m": [float(x) for x in before_point],
        "probe_after_xz_m": [float(x) for x in final_point],
        "probe_before_error_m": float(np.linalg.norm(before_error)),
        "probe_after_error_m": float(np.linalg.norm(residual)),
        "thigh_delta_rad": float(delta[0]),
        "knee_delta_rad": float(delta[1]),
        "minima": minima,
    })

# Keep the candidate pose interpolation readable; only candidate action is edited.
for layer in rig.animation_data.action.layers:
    for strip in layer.strips:
        for slot in rig.animation_data.action.slots:
            bag = strip.channelbag(slot)
            if bag:
                for curve in bag.fcurves:
                    for key in curve.keyframe_points:
                        key.interpolation = "LINEAR"

scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
rendered = []
for frame in (1, 5, 9, 13, 17, 21, 25, 29, 33):
    scene.frame_set(frame)
    path = os.path.join(OUT_DIR, f"Splat_Iter1622_footlock_{frame:02d}.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    rendered.append(os.path.basename(path))

stance = [row for row in samples]
report = {
    "iteration": 1622,
    "source_blend": os.path.basename(SOURCE_BLEND),
    "candidate_action": rig.animation_data.action.name,
    "method": "Per-frame damped least-squares adjustment of UpLeg and Leg X rotations to hold a fixed weighted-sole probe in world X/Z during each source-designated stance episode; mesh and rig are preserved.",
    "episodes": episodes,
    "max_probe_error_before_m": max(row["probe_before_error_m"] for row in stance),
    "max_probe_error_after_m": max(row["probe_after_error_m"] for row in stance),
    "max_abs_thigh_delta_rad": max(abs(row["thigh_delta_rad"]) for row in stance),
    "max_abs_knee_delta_rad": max(abs(row["knee_delta_rad"]) for row in stance),
    "min_stance_sole_z_m": min(row["minima"][row["stance"]]["min_z_m"] for row in stance),
    "max_abs_stance_min_z_m": max(abs(row["minima"][row["stance"]]["min_z_m"]) for row in stance),
    "max_swing_penetration_m": max(0.0, max(-row["minima"]["Right" if row["stance"] == "Left" else "Left"]["min_z_m"] for row in stance)),
    "max_stance_probe_drift_m": max(row["probe_after_error_m"] for row in stance),
    "frames": samples,
    "rendered_frames": rendered,
    "limitations": "Offline Blender-only candidate on one flat support plane. Weighted probes are skin/contact proxies; support normal, Unity import, varied surfaces, animation transitions, active-player integration, and human-play quality are unverified.",
}
with open(os.path.join(OUT_DIR, "Splat_Iter1622_FootLockReport.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Splat_XYFootLock_Gait_Iter1622.blend"))
print("ITER1622_SUMMARY", json.dumps({key: value for key, value in report.items() if key != "frames"}, sort_keys=True))
