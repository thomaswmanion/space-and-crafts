"""Author root-stable Meshy-rig gait with per-frame flat-plane foot-pivot correction."""
import bpy, json, os
from mathutils import Quaternion, Vector
import numpy as np

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
IN_BLEND = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1389\Splat_RotationOnly_Gait_Iter1389.blend"
OUT_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1393"
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=IN_BLEND)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
mesh = mesh_obj.data
rig.animation_data_clear()
for bone in rig.pose.bones:
    bone.rotation_mode = "QUATERNION"
    bone.rotation_quaternion = Quaternion((1, 0, 0), 0)
    bone.location = (0, 0, 0)
    bone.scale = (1, 1, 1)
bpy.context.view_layer.update()
for bone in rig.pose.bones:
    bone.rotation_mode = "QUATERNION"
    bone.rotation_quaternion = Quaternion((1, 0, 0), 0)
    bone.location = (0, 0, 0)
    bone.scale = (1, 1, 1)

action = bpy.data.actions.new("Splat_RotationOnly_FootLocked_Gait_Iter1393")
rig.animation_data_create()
rig.animation_data.action = action
scene.frame_start, scene.frame_end = 1, 33
phases = [
    (1, 0.0, 0.0), (5, 0.16, -0.16), (9, 0.20, -0.20),
    (13, 0.10, -0.10), (17, 0.0, 0.0), (21, -0.16, 0.16),
    (25, -0.20, 0.20), (29, -0.10, 0.10), (33, 0.0, 0.0),
]

def swing_at(frame):
    for index in range(len(phases) - 1):
        f0, l0, r0 = phases[index]
        f1, l1, r1 = phases[index + 1]
        if f0 <= frame <= f1:
            t = (frame - f0) / (f1 - f0)
            return l0 + (l1 - l0) * t, r0 + (r1 - r0) * t
    return 0.0, 0.0

def set_rotation(name, angle_x, angle_z=0.0, frame=None):
    bone = rig.pose.bones.get(name)
    if bone is None:
        raise RuntimeError(f"Missing expected rig bone: {name}")
    bone.rotation_mode = "QUATERNION"
    bone.rotation_quaternion = Quaternion((1, 0, 0), angle_x) @ Quaternion((0, 0, 1), angle_z)
    bone.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=name)

for frame, left_swing, right_swing in phases:
    scene.frame_set(frame)
    set_rotation("LeftUpLeg", left_swing, frame=frame)
    set_rotation("RightUpLeg", right_swing, frame=frame)
    set_rotation("LeftLeg", max(0.0, abs(left_swing) - .08) * .55, frame=frame)
    set_rotation("RightLeg", max(0.0, abs(right_swing) - .08) * .55, frame=frame)
    # Preserve the Meshy shoulder rest pose to keep its broad white-body weights still.
    set_rotation("LeftArm", -left_swing * .58, -.035, frame)
    set_rotation("RightArm", -right_swing * .58, .035, frame)
    set_rotation("LeftForeArm", .055 + max(0.0, -left_swing) * .18, frame=frame)
    set_rotation("RightForeArm", .055 + max(0.0, -right_swing) * .18, frame=frame)
    set_rotation("LeftFoot", -left_swing * .32, frame=frame)
    set_rotation("RightFoot", -right_swing * .32, frame=frame)
    set_rotation("LeftToeBase", left_swing * .12, frame=frame)
    set_rotation("RightToeBase", right_swing * .12, frame=frame)

group_ids = {group.name: group.index for group in mesh_obj.vertex_groups}
foot_sets = {}
for side in ("Left", "Right"):
    indices = np.asarray([
        vertex.index for vertex in mesh.vertices
        if any(item.group in (group_ids[side + "Foot"], group_ids[side + "ToeBase"])
               and item.weight >= .25 for item in vertex.groups)
    ], dtype=np.int32)
    if not len(indices):
        raise RuntimeError(f"No weighted sole vertices found for {side}; refusing foot correction")
    foot_sets[side] = indices
coords = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
mesh.vertices.foreach_get("co", coords)
coords = coords.reshape(-1, 3)
homogeneous = np.ones((len(coords), 4), dtype=np.float64)
homogeneous[:, :3] = coords
rest_matrix = np.array(mesh_obj.matrix_world, dtype=np.float64)
support_z = float((homogeneous @ rest_matrix.T)[:, 2].min())

def evaluated_world_z():
    depsgraph = bpy.context.evaluated_depsgraph_get()
    depsgraph.update()
    evaluated = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)
    try:
        local = np.empty(len(evaluated_mesh.vertices) * 3, dtype=np.float32)
        evaluated_mesh.vertices.foreach_get("co", local)
        local = local.reshape(-1, 3)
        matrix = np.array(evaluated.matrix_world, dtype=np.float64)
        points = np.ones((len(local), 4), dtype=np.float64)
        points[:, :3] = local
        return (points @ matrix.T)[:, 2]
    finally:
        evaluated.to_mesh_clear()

contact_report = []
for frame in range(1, 34):
    scene.frame_set(frame)
    left_foot = rig.pose.bones["LeftFoot"]
    right_foot = rig.pose.bones["RightFoot"]
    left_foot.location = (0, 0, 0)
    right_foot.location = (0, 0, 0)
    bpy.context.view_layer.update()
    z_before = evaluated_world_z()
    minima_before = {side: float(z_before[indices].min()) for side, indices in foot_sets.items()}
    stance = min(minima_before, key=minima_before.get)
    offsets = {}
    for side, bone in (("Left", left_foot), ("Right", right_foot)):
        minimum = minima_before[side]
        # Lock the lower/stance foot; keep a swing foot's natural clearance but
        # prevent it from penetrating the support surface.
        world_offset = support_z - minimum if side == stance else max(0.0, support_z - minimum)
        basis_world = rig.matrix_world @ bone.matrix
        axis = max(range(3), key=lambda i: abs(basis_world.to_3x3().col[i].z))
        signed_world_per_unit = float(basis_world.to_3x3().col[axis].z)
        if abs(signed_world_per_unit) < .005:
            raise RuntimeError(f"No stable world-up channel for {side}Foot at frame {frame}")
        local_location = [0.0, 0.0, 0.0]
        local_location[axis] = world_offset / signed_world_per_unit
        bone.location = local_location
        bone.keyframe_insert(data_path="location", frame=frame, group=side + "Foot")
        offsets[side] = world_offset
    bpy.context.view_layer.update()
    z_after = evaluated_world_z()
    minima_after = {side: float(z_after[indices].min()) for side, indices in foot_sets.items()}
    contact_report.append({
        "frame": frame,
        "swing_radians": {"left": swing_at(frame)[0], "right": swing_at(frame)[1]},
        "stance_foot": stance,
        "before_z_m": minima_before,
        "applied_world_up_offset_m": offsets,
        "after_z_m": minima_after,
    })

for layer in action.layers:
    for strip in layer.strips:
        for slot in action.slots:
            bag = strip.channelbag(slot)
            if bag:
                for curve in bag.fcurves:
                    for key in curve.keyframe_points:
                        key.interpolation = "LINEAR"

report = {
    "iteration": 1393,
    "action": action.name,
    "source": "Iter1370 remesh bound to the existing Meshy 24-bone rig",
    "frame_range": [1, 33],
    "support_z_m": support_z,
    "foot_weight_threshold": .25,
    "weighted_vertices": {side: int(len(indices)) for side, indices in foot_sets.items()},
    "frame_contacts": contact_report,
    "limitations": "Offline Blender candidate only; local per-foot offsets are authored against one flat plane. This is not Unity IK, varied-platform data, gameplay, or human-play verification. Body/shoulder motion is intentionally held stable.",
}
with open(os.path.join(OUT_DIR, "Splat_Iter1393_FootLockedGaitReport.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)

scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
for frame in (1, 5, 9, 13, 17, 21, 25, 29, 33):
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(OUT_DIR, f"Splat_Iter1393_gait_{frame:02d}.png")
    bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Splat_RotationOnly_FootLocked_Gait_Iter1393.blend"))
print("ITER1393_REPORT", json.dumps({"support_z_m": support_z, "weighted_vertices": report["weighted_vertices"], "frames": contact_report}, sort_keys=True))
