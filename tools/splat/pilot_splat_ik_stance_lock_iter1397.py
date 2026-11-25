"""Offline Blender probe for locking the designated Splat stance foot in XY."""
import bpy
import json
import os
import numpy as np
from mathutils import Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
SOURCE_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1395"
SOURCE = SOURCE_DIR + r"\Splat_FootSupportGait_Iter1395.blend"
REPORT_PATH = SOURCE_DIR + r"\Iter1397_IKStanceLockProbe.json"
POSE_DIR = SOURCE_DIR + r"\Iter1397_IKStanceLockProbe"
os.makedirs(POSE_DIR, exist_ok=True)

with open(SOURCE_DIR + r"\Splat_Iter1395_FootSupportGaitReport.json", "r", encoding="utf-8") as handle:
    source_report = json.load(handle)
bpy.ops.wm.open_mainfile(filepath=SOURCE)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh = next(obj for obj in scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
depsgraph = bpy.context.evaluated_depsgraph_get()
group_names = {group.name: group.index for group in mesh.vertex_groups}
sole_vertices = {
    side: np.asarray([
        vertex.index for vertex in mesh.data.vertices
        if any(item.group in (group_names[side + "Foot"], group_names[side + "ToeBase"])
               and item.weight >= 0.25 for item in vertex.groups)
    ], dtype=np.int32)
    for side in ("Left", "Right")
}

def evaluated_vertices():
    bpy.context.view_layer.update()
    evaluated = mesh.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)
    try:
        coords = np.empty(len(evaluated_mesh.vertices) * 3, dtype=np.float32)
        evaluated_mesh.vertices.foreach_get("co", coords)
        coords = coords.reshape(-1, 3)
        matrix = np.asarray(evaluated.matrix_world, dtype=np.float64)
        return coords @ matrix[:3, :3].T + matrix[:3, 3]
    finally:
        evaluated.to_mesh_clear()

def soles():
    points = evaluated_vertices()
    answer = {}
    for side, indices in sole_vertices.items():
        index = int(indices[np.argmin(points[indices, 2])])
        answer[side] = {"point": points[index].tolist(), "vertex": index}
    return answer

targets = {}
polers = {}
constraints = {}
for side in ("Left", "Right"):
    target = bpy.data.objects.new("Iter1397_" + side + "StanceTarget", None)
    target.empty_display_type = "SPHERE"
    target.empty_display_size = 0.03
    scene.collection.objects.link(target)
    pole = bpy.data.objects.new("Iter1397_" + side + "KneePole", None)
    pole.empty_display_type = "CIRCLE"
    pole.empty_display_size = 0.04
    scene.collection.objects.link(pole)
    bone = rig.pose.bones[side + "ToeBase"]
    constraint = bone.constraints.new("IK")
    constraint.name = "Iter1397_StanceLockProbe"
    constraint.target = target
    constraint.pole_target = pole
    constraint.chain_count = 4
    constraint.use_stretch = False
    constraint.use_tail = True
    constraint.iterations = 128
    targets[side], polers[side], constraints[side] = target, pole, constraint

sequence = {entry["frame"]: entry["stance"] for entry in source_report["frame_data"]}
stance_anchor = {}
episode = {"Left": 0, "Right": 0}
last_stance = None
samples = []
for frame in range(1, 34):
    for chain_side in ("Left", "Right"):
        constraints[chain_side].mute = True
    scene.frame_set(frame)
    side = sequence[frame]
    before = soles()
    # Keep the sole vertex chosen by the Iter1395 fitter fixed in world X/Z,
    # using its current offset from the toe-bone endpoint as the target offset.
    sole = Vector(before[side]["point"])
    if last_stance != side:
        episode[side] += 1
        stance_anchor[(side, episode[side])] = Vector((sole.x, sole.y, 0.0))
    anchor = stance_anchor[(side, episode[side])]
    effector_tail = rig.matrix_world @ rig.pose.bones[side + "ToeBase"].tail
    target_location = effector_tail + (anchor - sole)
    targets[side].location = target_location
    knee_head = rig.matrix_world @ rig.pose.bones[side + "Leg"].head
    polers[side].location = knee_head + Vector((0.0, -0.2, 0.0))
    constraints[side].mute = False
    bpy.context.view_layer.update()
    after = soles()
    active_point = Vector(after[side]["point"])
    ik_tail = rig.matrix_world @ rig.pose.bones[side + "ToeBase"].tail
    target_world = targets[side].matrix_world.translation.copy()
    samples.append({
        "frame": frame,
        "stance": side,
        "episode": episode[side],
        "target_world": [float(v) for v in targets[side].matrix_world.translation],
        "anchor_world": [float(v) for v in anchor],
        "ik_tail_world": [float(v) for v in ik_tail],
        "ik_tail_error_m": float((ik_tail - target_world).length),
        "sole_before": before[side]["point"],
        "sole_after": after[side]["point"],
        "contact_error_m": float(active_point.z),
        "horizontal_error_m": float((active_point - anchor).xy.length),
        "swing_sole_after": after["Right" if side == "Left" else "Left"]["point"],
    })
    last_stance = side

for frame in (1, 5, 9, 13, 17, 21, 25, 29, 33):
    scene.frame_set(frame)
    side = sequence[frame]
    for chain_side in ("Left", "Right"):
        constraints[chain_side].mute = chain_side != side
    sample = samples[frame - 1]
    targets[side].location = Vector(sample["target_world"])
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.filepath = os.path.join(POSE_DIR, f"stance_{frame:02d}.png")
    bpy.ops.render.render(write_still=True)

report = {
    "iteration": 1397,
    "source_action": source_report["action"],
    "method": "Per-frame Blender IK constraint on ToeBase with a four-bone leg chain and fixed selected sole X/Y/Z target within each stance episode.",
    "stance_episodes": episode,
    "max_stance_contact_error_m": max(abs(sample["contact_error_m"]) for sample in samples),
    "max_stance_horizontal_error_m": max(sample["horizontal_error_m"] for sample in samples),
    "max_ik_tail_error_m": max(sample["ik_tail_error_m"] for sample in samples),
    "max_swing_penetration_m": max(0.0, max(-sample["swing_sole_after"][2] for sample in samples)),
    "samples": samples,
    "limitations": "One flat support plane and candidate-only pose probe. Action curves were not baked, exported, integrated, or human-reviewed in gameplay; support-point selection and projected pose require visual and runtime validation.",
}
with open(REPORT_PATH, "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
print("ITER1397_IK_PROBE", json.dumps({k: v for k, v in report.items() if k != "samples"}, sort_keys=True))
