"""Author an isolated, root-stable gait on the existing 24-bone Meshy rig."""
import bpy, json, os, math
from mathutils import Vector, Quaternion

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
IN_BLEND = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1370\Splat_Meshy_Iter1365_RigBound_Iter1370.blend"
OUT_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1389"
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=IN_BLEND)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh = next(obj for obj in scene.objects if obj.type == "MESH" and "RigBound" in obj.name)
rig.data.pose_position = "POSE"
rig.animation_data_clear()
for bone in rig.pose.bones:
    bone.rotation_mode = "QUATERNION"
    bone.rotation_quaternion = Quaternion((1, 0, 0), 0)
    bone.location = (0, 0, 0)
    bone.scale = (1, 1, 1)

action = bpy.data.actions.new("Splat_RotationOnly_GroundedGait_Iter1389")
rig.animation_data_create()
rig.animation_data.action = action
scene.frame_start = 1
scene.frame_end = 33
poses = [
    # phase, left-leg forward swing, right-leg forward swing
    (1, 0.0, 0.0),
    (5, 0.16, -0.16),
    (9, 0.20, -0.20),
    (13, 0.10, -0.10),
    (17, 0.0, 0.0),
    (21, -0.16, 0.16),
    (25, -0.20, 0.20),
    (29, -0.10, 0.10),
    (33, 0.0, 0.0),
]

def set_bone_quat(name, angle_x, angle_z=0.0):
    bone = rig.pose.bones.get(name)
    if bone is None:
        raise RuntimeError(f"Missing expected rig bone: {name}")
    bone.rotation_mode = "QUATERNION"
    bone.rotation_quaternion = Quaternion((1, 0, 0), angle_x) @ Quaternion((0, 0, 1), angle_z)
    bone.keyframe_insert(data_path="rotation_quaternion", frame=scene.frame_current, group=name)

for frame, left_swing, right_swing in poses:
    scene.frame_set(frame)
    set_bone_quat("LeftUpLeg", left_swing)
    set_bone_quat("RightUpLeg", right_swing)
    # Small knee folds occur only while the corresponding foot is in swing.
    set_bone_quat("LeftLeg", max(0.0, abs(left_swing) - 0.08) * 0.55)
    set_bone_quat("RightLeg", max(0.0, abs(right_swing) - 0.08) * 0.55)
    # Arms counter-swing gently at the upper-arm joint; shoulders are deliberately
    # fixed so their broad existing white-body weights do not drive torso squash.
    set_bone_quat("LeftArm", -left_swing * 0.58, -0.035)
    set_bone_quat("RightArm", -right_swing * 0.58, 0.035)
    set_bone_quat("LeftForeArm", 0.055 + max(0.0, -left_swing) * 0.18)
    set_bone_quat("RightForeArm", 0.055 + max(0.0, -right_swing) * 0.18)
    # Toe/foot compensation is intentionally modest; it is a pose preview, not IK.
    set_bone_quat("LeftFoot", -left_swing * 0.32)
    set_bone_quat("RightFoot", -right_swing * 0.32)
    set_bone_quat("LeftToeBase", left_swing * 0.12)
    set_bone_quat("RightToeBase", right_swing * 0.12)

points = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
center = sum(points, Vector()) / len(points)
lo_z = min(point.z for point in points)
span_x = max(p.x for p in points) - min(p.x for p in points)
span_y = max(p.y for p in points) - min(p.y for p in points)
floor_mesh = bpy.data.meshes.new("Iter1389_Support")
floor_mesh.from_pydata([(-4, -4, lo_z), (4, -4, lo_z), (4, 4, lo_z), (-4, 4, lo_z)], [], [(0, 1, 2, 3)])
floor = bpy.data.objects.new("Iter1389_Support", floor_mesh)
scene.collection.objects.link(floor)
floor_material = bpy.data.materials.new("Iter1389_SupportMaterial")
floor_material.diffuse_color = (.23, .29, .36, 1)
floor.data.materials.append(floor_material)
for name, energy, size, offset in (("Key", 900, 4, (3, -4, 5)), ("Fill", 450, 5, (-4, -2, 1.5))):
    light_data = bpy.data.lights.new("Iter1389_" + name, "AREA")
    light_data.energy = energy
    light_data.size = size
    light = bpy.data.objects.new("Iter1389_" + name, light_data)
    scene.collection.objects.link(light)
    light.location = center + Vector(offset)
    light.rotation_euler = (center - light.location).to_track_quat("-Z", "Y").to_euler()
camera_data = bpy.data.cameras.new("Iter1389_Camera")
camera = bpy.data.objects.new("Iter1389_Camera", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.data.type = "ORTHO"
camera.data.ortho_scale = max(span_x, span_y) * 1.8
camera.location = center + Vector((0, -5, .55))
camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Z").to_euler()
world = bpy.data.worlds.new("Iter1389_World")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (.63, .69, .76, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = .8
scene.world = world
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.view_settings.view_transform = "AgX"
renders = []
for frame in (1, 5, 9, 13, 17, 21, 25, 29, 33):
    scene.frame_set(frame)
    path = os.path.join(OUT_DIR, f"Splat_Iter1389_gait_{frame:02d}.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    renders.append(os.path.basename(path))

report = {
    "iteration": 1389,
    "action": action.name,
    "source": "Iter1370 remesh bound to existing 24-bone Meshy rig",
    "frame_range": [1, 33],
    "animation_style": "Rotation-only in-place cycle; Hips, shoulders, spine, head, and scale stay at rest. Small alternating leg/arm arcs, knee flex, toe compensation.",
    "rendered_frames": renders,
    "limitations": "Offline Blender pose candidate only; no Unity retarget/import, gameplay, support-plane sole audit, varied-platform review, or human play. Candidate remains isolated.",
}
with open(os.path.join(OUT_DIR, "Splat_Iter1389_GaitReport.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Splat_RotationOnly_Gait_Iter1389.blend"))
print("ITER1389_REPORT", json.dumps(report, sort_keys=True))
