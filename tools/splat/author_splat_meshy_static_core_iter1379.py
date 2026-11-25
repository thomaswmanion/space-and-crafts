"""Create an isolated Splat rig candidate with a stable torso core.

This uses the already downloaded Iter1365 Meshy remesh and the local Iter1370
weight-transfer blend. It does not call Meshy or alter the active game prefab.
"""

import bpy
import json
import math
import os
import re
from collections import defaultdict
from mathutils import Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
SOURCE = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1370\Splat_Meshy_Iter1365_RigBound_Iter1370.blend"
OUT_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1379"
os.makedirs(OUT_DIR, exist_ok=True)

KEEP_BONES = {
    "LeftArm", "LeftForeArm", "LeftHand", "RightArm", "RightForeArm", "RightHand",
    "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
    "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
}
LIMB_GROUPS = {
    "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
    "RightShoulder", "RightArm", "RightForeArm", "RightHand",
    "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
    "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
}
CORE_BONE = "SplatBodyCore"
CORE_FULL_BELOW = 0.10
CORE_NONE_ABOVE = 0.52

bpy.ops.wm.open_mainfile(filepath=SOURCE)
scene = bpy.context.scene
armature = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
mesh = mesh_obj.data
action = armature.animation_data.action if armature.animation_data else None
if action is None:
    raise RuntimeError("Source walk action is missing; candidate was not created.")

# Add a non-animated control bone at the visual center. Vertices assigned to it
# retain the same bind-pose surface while limb joints continue to articulate.
arm_inv = armature.matrix_world.inverted()
bpy.context.view_layer.objects.active = armature
armature.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
edit_bones = armature.data.edit_bones
if CORE_BONE in edit_bones:
    edit_bones.remove(edit_bones[CORE_BONE])
core = edit_bones.new(CORE_BONE)
core.head = arm_inv @ Vector((0.0, 0.0, 0.62))
core.tail = arm_inv @ Vector((0.0, 0.0, 1.02))
core.use_deform = True
bpy.ops.object.mode_set(mode="OBJECT")

groups = {group.name: group for group in mesh_obj.vertex_groups}
core_group = groups.get(CORE_BONE) or mesh_obj.vertex_groups.new(name=CORE_BONE)
group_by_index = {group.index: group.name for group in mesh_obj.vertex_groups}
new_assignments = defaultdict(lambda: defaultdict(list))
core_buckets = defaultdict(list)
selected_core = 0
blend_weights = []
changed_indices = []

def smoothstep(a, b, value):
    t = max(0.0, min(1.0, (value - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)

for vertex in mesh.vertices:
    source_weights = {}
    limb_total = 0.0
    for assignment in vertex.groups:
        name = group_by_index.get(assignment.group)
        if name is None or assignment.weight <= 0.0001:
            continue
        source_weights[name] = source_weights.get(name, 0.0) + assignment.weight
        if name in LIMB_GROUPS:
            limb_total += assignment.weight

    # Smoothly preserve confident limb vertices. At the center of the character,
    # make the Meshy torso/face/cap surface rigid so the imported pelvis/spine
    # squash cannot compress the approved round silhouette.
    core_weight = 1.0 - smoothstep(CORE_FULL_BELOW, CORE_NONE_ABOVE, limb_total)
    if core_weight <= 0.001:
        continue
    selected_core += 1
    blend_weights.append(core_weight)
    changed_indices.append(vertex.index)
    bucket = max(1, min(255, int(round(core_weight * 255.0))))
    core_buckets[bucket].append(vertex.index)
    retained = (1.0 - core_weight)
    for name, weight in source_weights.items():
        value = weight * retained
        if value <= 0.0001:
            continue
        weight_bucket = max(1, min(255, int(round(value * 255.0))))
        new_assignments[name][weight_bucket].append(vertex.index)

for group in list(mesh_obj.vertex_groups):
    try:
        group.remove(changed_indices)
    except RuntimeError:
        pass
for group_name, buckets in new_assignments.items():
    group = groups.get(group_name)
    if group is None:
        continue
    for bucket, indices in buckets.items():
        group.add(indices, bucket / 255.0, "REPLACE")
for bucket, indices in core_buckets.items():
    core_group.add(indices, bucket / 255.0, "REPLACE")

# Keep only the original arm/leg motion. Body, pelvis, spine, head, and shoulder
# curves are removed from this candidate action to avoid translating the fixed
# torso through skin weights at the appendage roots.
slot = next(iter(action.slots))
removed_paths = 0
kept_paths = 0
for layer in action.layers:
    for strip in layer.strips:
        bag = strip.channelbag(slot)
        if bag is None:
            continue
        for curve in list(bag.fcurves):
            match = re.match(r'pose\.bones\["([^"]+)"\]\.', curve.data_path)
            if match and match.group(1) in KEEP_BONES:
                kept_paths += 1
            else:
                bag.fcurves.remove(curve)
                removed_paths += 1
action.name = "Splat_Meshy_LimbsOnly_StaticCore_Iter1379"
mesh_obj.name = "ART_SplatBody_Meshy_StaticCore_Iter1379"
armature.data.pose_position = "POSE"
scene.frame_start = int(action.frame_range[0])
scene.frame_end = int(action.frame_range[1])

# Verify the core contribution remains fixed across representative walk poses.
core_vertex_indices = set(changed_indices)
core_motion = []
for frame in range(scene.frame_start, scene.frame_end + 1):
    scene.frame_set(frame)
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh()
    matrix = mesh_obj.matrix_world
    displacement = 0.0
    for index in core_vertex_indices:
        original = mesh_obj.matrix_world @ mesh.vertices[index].co
        current = matrix @ evaluated_mesh.vertices[index].co
        displacement = max(displacement, (current - original).length)
    core_motion.append(displacement)
    evaluated.to_mesh_clear()

report = {
    "iteration": 1379,
    "candidate": mesh_obj.name,
    "source_blend": os.path.relpath(SOURCE, ROOT),
    "source_action": "Splat_Walk_Cycle",
    "candidate_action": action.name,
    "mesh_vertices": len(mesh.vertices),
    "core_vertices": selected_core,
    "core_weight_mean": sum(blend_weights) / max(1, len(blend_weights)),
    "kept_action_channels": kept_paths,
    "removed_action_channels": removed_paths,
    "core_max_frame_displacement_m": max(core_motion, default=0.0),
    "limitations": "Isolated Blender rig/animation candidate. Static torso core with existing limb-only walk curves; not installed in Unity. This does not prove natural motion, joint quality, varied-platform contact, gameplay, or human-play quality.",
}
report_path = os.path.join(OUT_DIR, "Splat_Iter1379_StaticCoreReport.json")
with open(report_path, "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
print("ITER1379_REPORT", json.dumps(report, indent=2))

# Reuse the isolated studio scene. Render only representative source frames.
scene.render.engine = "CYCLES"
scene.cycles.samples = 16
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
for frame in (scene.frame_start, scene.frame_start + 8, scene.frame_start + 16, scene.frame_start + 24):
    frame = min(frame, scene.frame_end)
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(OUT_DIR, f"Splat_Iter1379_walk_{frame:02d}.png")
    bpy.ops.render.render(write_still=True)

blend_path = os.path.join(OUT_DIR, "Splat_Meshy_StaticCore_Iter1379.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print("ITER1379_OUTPUT", blend_path, report_path)
