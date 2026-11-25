"""Render a few review poses from the authored Splat Blender rig.

This is a visual art-review helper. It does not modify the saved blend, FBX, or Unity assets.
"""

import math
import os
import bpy
from mathutils import Vector


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT, "Assets", "SpaceAndCrafts", "Art", "Characters", "Splat", "Splat_Rigged.blend")
OUTPUT_DIR = os.path.join(PROJECT, "docs", "art", "splat-rigged-preview", "poses")


def set_pose(armature, values):
    for pose_bone in armature.pose.bones:
        pose_bone.rotation_mode = "XYZ"
        pose_bone.rotation_euler = (0.0, 0.0, 0.0)
        pose_bone.location = (0.0, 0.0, 0.0)
        pose_bone.scale = (1.0, 1.0, 1.0)
    for name, angles in values.items():
        bone = armature.pose.bones.get(name)
        if bone is not None:
            bone.rotation_euler = tuple(math.radians(angle) for angle in angles)
    bpy.context.view_layer.update()


def render_pose(scene, armature, camera, name, values):
    set_pose(armature, values)
    scene.camera = camera
    scene.render.filepath = os.path.join(OUTPUT_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)


def make_camera(name, location, target):
    data = bpy.data.cameras.new(name)
    camera = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = location
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 2.75
    return camera


def make_area_light(name, location, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    light = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(light)
    light.location = location
    direction = Vector((0, 0, 0.95)) - light.location
    light.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return light


os.makedirs(OUTPUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=BLEND_PATH)
scene = bpy.context.scene
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
armature = bpy.data.objects.get("Splat_SkinRig")
camera = bpy.data.objects.get("PreviewCamera_front-three-quarter")
if armature is None:
    raise RuntimeError("Splat rig is missing from the saved blend")
if camera is None:
    camera = make_camera("ReviewCamera_Pose", (2.5, -5.8, 1.45), (0, 0, 1.0))
    make_area_light("ReviewKey", (3.0, -4.0, 4.2), 470, 4.0, (0.76, 0.85, 1.0))
    make_area_light("ReviewFill", (-3.0, -2.0, 2.2), 270, 3.2, (1.0, 0.67, 0.49))
    make_area_light("ReviewRim", (0.0, 2.0, 3.1), 520, 3.0, (0.32, 0.70, 1.0))

render_pose(scene, armature, camera, "pose-idle", {})
# A profile-biased angle separates the alternating boots so this static review
# can reveal foot pose and leg overlap instead of hiding the far leg behind Splat.
run_camera = make_camera("ReviewCamera_RunProfile", (4.2, -4.6, 1.35), (0.0, -0.05, 0.86))
run_camera.data.ortho_scale = 2.9
render_pose(scene, armature, run_camera, "pose-run", {
    "Bone_ArmL": (18, 0, -18),
    "Bone_ArmR": (-18, 0, 18),
    "Bone_LegL": (-18, 0, 0),
    "Bone_LegR": (18, 0, 0),
    "Bone_Spine": (2, 0, 0),
    "Bone_Head": (-2, 0, 0),
    "Bone_Antenna": (0, 0, 4.5),
})
render_pose(scene, armature, run_camera, "pose-run-reverse", {
    "Bone_ArmL": (-18, 0, -18),
    "Bone_ArmR": (18, 0, 18),
    "Bone_LegL": (18, 0, 0),
    "Bone_LegR": (-18, 0, 0),
    "Bone_Spine": (2, 0, 0),
    "Bone_Head": (-2, 0, 0),
    "Bone_Antenna": (0, 0, -4.5),
})
rear_run_camera = make_camera("ReviewCamera_RunRear", (2.5, 5.8, 1.45), (0.0, 0.05, 0.92))
rear_run_camera.data.ortho_scale = 2.9
render_pose(scene, armature, rear_run_camera, "pose-run-rear", {
    "Bone_ArmL": (18, 0, -18),
    "Bone_ArmR": (-18, 0, 18),
    "Bone_LegL": (-18, 0, 0),
    "Bone_LegR": (18, 0, 0),
    "Bone_Spine": (2, 0, 0),
    "Bone_Head": (-2, 0, 0),
    "Bone_Antenna": (0, 0, 4.5),
})
render_pose(scene, armature, rear_run_camera, "pose-run-rear-reverse", {
    "Bone_ArmL": (-18, 0, -18),
    "Bone_ArmR": (18, 0, 18),
    "Bone_LegL": (18, 0, 0),
    "Bone_LegR": (-18, 0, 0),
    "Bone_Spine": (2, 0, 0),
    "Bone_Head": (-2, 0, 0),
    "Bone_Antenna": (0, 0, -4.5),
})
render_pose(scene, armature, camera, "pose-jump", {
    "Bone_ArmL": (-48, 0, -25),
    "Bone_ArmR": (-48, 0, 25),
    "Bone_LegL": (12, 0, 0),
    "Bone_LegR": (12, 0, 0),
    "Bone_Spine": (-10, 0, 0),
    "Bone_Head": (-7, 0, 0),
    "Bone_Antenna": (0, 0, 12),
})
print("Rendered Splat rig pose previews to " + OUTPUT_DIR)
