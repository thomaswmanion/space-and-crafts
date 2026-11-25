"""Iter1625 readable-run candidate with enlarged swing/arm ranges, based on the Iter1623 foot-lock candidate."""
import bpy
import json
import math
import os
import numpy as np
from mathutils import Quaternion

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
CAND_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1623"
IN_BLEND = CAND_DIR + r"\Splat_ReadableXYFootLock_Run_Iter1623.blend"
CONTACT_REPORT = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1622\Splat_Iter1622_FootLockReport.json"
OUT_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1625"
os.makedirs(OUT_DIR, exist_ok=True)

with open(CONTACT_REPORT, "r", encoding="utf-8") as handle:
    locked = json.load(handle)
bpy.ops.wm.open_mainfile(filepath=IN_BLEND)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
mesh = mesh_obj.data
action = rig.animation_data.action
action.name = "Splat_ReadableXYFootLock_Run_Iter1625"
groups = {g.name: g.index for g in mesh_obj.vertex_groups}
sole_vertices = {}
for side in ("Left", "Right"):
    foot, toe = groups[side + "Foot"], groups[side + "ToeBase"]
    ids = np.asarray([
        v.index for v in mesh.vertices
        if any(item.group in (foot, toe) and item.weight >= 0.25 for item in v.groups)
    ], dtype=np.int32)
    if not len(ids):
        raise RuntimeError(f"No weighted sole vertices for {side}")
    sole_vertices[side] = ids

depsgraph = bpy.context.evaluated_depsgraph_get()
def evaluated_points():
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

def minima(points):
    out = {}
    for side, ids in sole_vertices.items():
        vertex = int(ids[np.argmin(points[ids, 2])])
        out[side] = {"vertex": vertex, "point": [float(x) for x in points[vertex]],
                     "min_z_m": float(points[vertex, 2])}
    return out

stance_by_frame = {}
for row in locked["frames"]:
    stance_by_frame[int(row["frame"])] = row["stance"]
episodes = locked["episodes"]
episode_for = {}
for index, episode in enumerate(episodes):
    for frame in range(int(episode["start"]), int(episode["end"]) + 1):
        episode_for[frame] = index

# Cache the established Iter1622 stance rotations before replacing any keys.
base_pose = {}
for frame in range(1, 34):
    scene.frame_set(frame)
    base_pose[frame] = {}
    for name in ("LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg",
                 "LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase",
                 "LeftShoulder", "RightShoulder", "LeftArm", "RightArm",
                 "LeftForeArm", "RightForeArm"):
        bone = rig.pose.bones.get(name)
        if bone is not None:
            base_pose[frame][name] = bone.rotation_quaternion.copy()

MAX_ABS_DELTA_RAD = 0.35
MAX_STEP_RAD = 0.12
EPS = 0.003
DAMPING = 2.5e-4
SWING_MAX_RAD = 0.78
KNEE_FLEX_MAX_RAD = 0.43
samples = []
for frame in range(1, 34):
    scene.frame_set(frame)
    stance = stance_by_frame[frame]
    swing_side = "Right" if stance == "Left" else "Left"
    episode = episodes[episode_for[frame]]
    target = np.asarray(episode["anchor_xz_m"], dtype=np.float64)
    probe_vertex = int(episode["anchor_vertex"])

    # Restore the fitted Iter1622 pose for both legs, then enlarge only the leg
    # that is in swing. The grounded leg remains at its fitted base before solve.
    for name, quat in base_pose[frame].items():
        rig.pose.bones[name].rotation_quaternion = quat

    if swing_side == "Left":
        phase = math.pi * (frame - 1) / 16.0
    else:
        phase = math.pi * (frame - 17) / 16.0
    envelope = max(0.0, math.sin(phase))
    swing = SWING_MAX_RAD * envelope
    knee_flex = KNEE_FLEX_MAX_RAD * envelope
    rig.pose.bones[swing_side + "UpLeg"].rotation_quaternion = Quaternion((1, 0, 0), swing)
    rig.pose.bones[swing_side + "Leg"].rotation_quaternion = Quaternion((1, 0, 0), knee_flex)
    rig.pose.bones[swing_side + "Foot"].rotation_quaternion = Quaternion((1, 0, 0), -swing * 0.30)
    rig.pose.bones[swing_side + "ToeBase"].rotation_quaternion = Quaternion((1, 0, 0), swing * 0.10)

    # The candidate's neutral shoulder heights are asymmetric by about 72 cm at
    # the hands. First compensate in the local swing plane, then animate around
    # each shoulder's near-vertical local Z axis so the arms pump front/back
    # instead of keeping the Meshy source's raised greeting silhouette.
    stride_sign = -1.0 if swing_side == "Left" else 1.0
    arm_yaw = stride_sign * swing * 0.72
    for side, neutral_x in (("Left", 0.45), ("Right", -0.45)):
        names = (side + "Shoulder", side + "Arm", side + "ForeArm")
        shoulder = rig.pose.bones.get(names[0])
        arm = rig.pose.bones.get(names[1])
        forearm = rig.pose.bones.get(names[2])
        if shoulder is not None:
            base = base_pose[frame][names[0]]
            shoulder.rotation_quaternion = base @ Quaternion((1, 0, 0), neutral_x) \
                @ Quaternion((0, 0, 1), arm_yaw)
        if arm is not None:
            base = base_pose[frame][names[1]]
            arm.rotation_quaternion = base @ Quaternion((0, 0, 1), arm_yaw)
        if forearm is not None:
            base = base_pose[frame][names[2]]
            forearm.rotation_quaternion = base @ Quaternion((0, 0, 1), -arm_yaw * 0.30)

    thigh = rig.pose.bones[stance + "UpLeg"]
    knee = rig.pose.bones[stance + "Leg"]
    base_thigh = thigh.rotation_quaternion.copy()
    base_knee = knee.rotation_quaternion.copy()
    delta = np.zeros(2, dtype=np.float64)

    def apply(candidate):
        thigh.rotation_quaternion = base_thigh @ Quaternion((1, 0, 0), float(candidate[0]))
        knee.rotation_quaternion = base_knee @ Quaternion((1, 0, 0), float(candidate[1]))
        points = evaluated_points()
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
            probe[axis] += EPS
            shifted, _ = apply(probe)
            jacobian[:, axis] = (shifted - current) / EPS
        try:
            change = np.linalg.solve(jacobian.T @ jacobian + DAMPING * np.eye(2),
                                     -(jacobian.T @ error))
        except np.linalg.LinAlgError:
            break
        change = np.clip(change, -MAX_STEP_RAD, MAX_STEP_RAD)
        proposal = np.clip(delta + change, -MAX_ABS_DELTA_RAD, MAX_ABS_DELTA_RAD)
        if np.linalg.norm(proposal - delta) < 1e-5:
            break
        delta = proposal
    after_point, points = apply(delta)
    residual = after_point - target

    # Key the complete changed leg/arm pose each frame so the new candidate
    # is self-contained rather than relying on procedural offsets at runtime.
    changed_bones = set()
    for side in ("Left", "Right"):
        for suffix in ("UpLeg", "Leg", "Foot", "ToeBase", "Shoulder", "Arm", "ForeArm"):
            name = side + suffix
            bone = rig.pose.bones.get(name)
            if bone is not None:
                bone.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=name)
                changed_bones.add(name)

    feet = minima(points)
    samples.append({
        "frame": frame,
        "stance": stance,
        "swing": swing_side,
        "swing_envelope": envelope,
        "requested_swing_rad": swing,
        "requested_knee_flex_rad": knee_flex,
        "stance_probe_target_xz_m": [float(x) for x in target],
        "stance_probe_before_xz_m": [float(x) for x in before_point],
        "stance_probe_after_xz_m": [float(x) for x in after_point],
        "stance_probe_error_before_m": float(np.linalg.norm(before_error)),
        "stance_probe_error_after_m": float(np.linalg.norm(residual)),
        "stance_thigh_solver_delta_rad": float(delta[0]),
        "stance_knee_solver_delta_rad": float(delta[1]),
        "feet": feet,
    })

for layer in action.layers:
    for strip in layer.strips:
        for slot in action.slots:
            bag = strip.channelbag(slot)
            if bag:
                for curve in bag.fcurves:
                    for key in curve.keyframe_points:
                        key.interpolation = "LINEAR"

scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
frames = (1, 5, 9, 13, 17, 21, 25, 29, 33)
rendered = []
for frame in frames:
    scene.frame_set(frame)
    path = os.path.join(OUT_DIR, f"Splat_Iter1625_run_{frame:02d}.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    rendered.append(path)

# Assemble a 3x3 contact sheet using Blender's bundled NumPy image buffers.
images = [bpy.data.images.load(path, check_existing=False) for path in rendered]
width, height = images[0].size
sheet_pixels = np.zeros((height * 3, width * 3, 4), dtype=np.float32)
for index, image in enumerate(images):
    buffer = np.empty(width * height * 4, dtype=np.float32)
    image.pixels.foreach_get(buffer)
    tile = buffer.reshape((height, width, 4))
    row, col = divmod(index, 3)
    sheet_pixels[row * height:(row + 1) * height,
                 col * width:(col + 1) * width] = tile
sheet = bpy.data.images.new("Splat_Iter1625_RunContactSheet", width=width * 3,
                            height=height * 3, alpha=True, float_buffer=False)
sheet.pixels.foreach_set(sheet_pixels.reshape(-1))
sheet.file_format = "PNG"
sheet.filepath_raw = os.path.join(OUT_DIR, "Splat_Iter1625_RunContactSheet.png")
sheet.save()

stance_rows = [row for row in samples]
report = {
    "iteration": 1625,
    "source_candidate": os.path.basename(IN_BLEND),
    "action": action.name,
    "method": "Amplified swing-leg rotations and symmetric shoulder-height compensation; arm pump rotates about local vertical axes with the arm opposite the advancing leg leading; re-solved stance probe in world X/Z. Mesh, rig, and scale channels preserved.",
    "max_stance_probe_error_before_m": max(row["stance_probe_error_before_m"] for row in stance_rows),
    "max_stance_probe_error_after_m": max(row["stance_probe_error_after_m"] for row in stance_rows),
    "max_abs_solver_thigh_delta_rad": max(abs(row["stance_thigh_solver_delta_rad"]) for row in stance_rows),
    "max_abs_solver_knee_delta_rad": max(abs(row["stance_knee_solver_delta_rad"]) for row in stance_rows),
    "max_abs_stance_min_z_m": max(abs(row["feet"][row["stance"]]["min_z_m"]) for row in stance_rows),
    "min_stance_min_z_m": min(row["feet"][row["stance"]]["min_z_m"] for row in stance_rows),
    "max_swing_penetration_m": max(0.0, max(-row["feet"][row["swing"]]["min_z_m"] for row in stance_rows)),
    "rendered_frames": [os.path.basename(path) for path in rendered],
    "contact_sheet": "Splat_Iter1625_RunContactSheet.png",
    "samples": samples,
    "limitations": "Offline Blender candidate, one flat support plane and sampled poses only. Foot-weight minima are proxies; contact during transitions, varied platforms, Unity/runtime integration, player-distance readability, and human-play quality remain unverified.",
}
with open(os.path.join(OUT_DIR, "Splat_Iter1625_RunContactReport.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Splat_ReadableXYFootLock_Run_Iter1625.blend"))
print("ITER1625_SUMMARY", json.dumps({key: value for key, value in report.items() if key != "samples"}, sort_keys=True))



