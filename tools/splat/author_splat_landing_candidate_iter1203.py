"""Author a landing-clip review candidate on Splat's existing skinned rig."""

import math
import os

import bpy
from mathutils import Vector


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SOURCE_BLEND = os.path.join(PROJECT, "docs", "art", "splat-foot-rig-iter1137",
                            "Splat_Character_Iter1137.blend")
CANDIDATE_DIR = os.path.join(PROJECT, "Assets", "SpaceAndCrafts", "Art", "Animations",
                             "Authoring", "Iter1203")
REVIEW_DIR = os.path.join(PROJECT, "Logs", "score-comparison", "splat-landing-authoring-iter1203")
ARMATURE_NAME = "Splat_SkinRig"
FPS = 30


def key_pose(armature, frame, rotations, locations):
    for bone in armature.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = tuple(math.radians(v) for v in rotations.get(bone.name, (0, 0, 0)))
        bone.location = locations.get(bone.name, (0, 0, 0))
        bone.scale = (1, 1, 1)
        bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone.name)
        bone.keyframe_insert(data_path="location", frame=frame, group=bone.name)


def main():
    os.makedirs(CANDIDATE_DIR, exist_ok=True)
    os.makedirs(REVIEW_DIR, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=SOURCE_BLEND)
    scene = bpy.context.scene
    rig = bpy.data.objects.get(ARMATURE_NAME)
    if rig is None or rig.type != "ARMATURE":
        raise RuntimeError("Splat_SkinRig missing from source blend")

    action = bpy.data.actions.new("Splat_Land_Iter1203_Candidate")
    rig.animation_data_create()
    rig.animation_data.action = action

    # Hold the impact compression through the runtime's 0.14s action blend,
    # then release into a quiet recovery. Otherwise the clip has already left
    # its only crouched frame before the transition reaches full weight. Hips
    # and leg bones absorb the hit; feet counter their travel. No bone is scaled.
    poses = [
        (1,
         {"Bone_Spine": (14, 0, 0), "Bone_Chest": (7, 0, 0), "Bone_Head": (2, 0, 0),
          "Bone_ArmL": (32, 0, -24), "Bone_ArmR": (32, 0, 24),
          "Bone_LegL": (-11, 0, 0), "Bone_LegR": (-11, 0, 0),
          "Bone_FootL": (5, 0, 0), "Bone_FootR": (5, 0, 0),
          "Bone_Antenna": (0, 0, -18)},
         {"Bone_Hips": (0, 0, -0.070), "Bone_LegL": (0, 0, 0.055),
          "Bone_LegR": (0, 0, 0.055)}),
        (5,
         {"Bone_Spine": (12, 0, 0), "Bone_Chest": (6, 0, 0), "Bone_Head": (2, 0, 0),
          "Bone_ArmL": (29, 0, -22), "Bone_ArmR": (29, 0, 22),
          "Bone_LegL": (-10, 0, 0), "Bone_LegR": (-10, 0, 0),
          "Bone_FootL": (5, 0, 0), "Bone_FootR": (5, 0, 0),
          "Bone_Antenna": (0, 0, -16)},
         {"Bone_Hips": (0, 0, -0.063), "Bone_LegL": (0, 0, 0.050),
          "Bone_LegR": (0, 0, 0.050)}),
        (9,
         {"Bone_Spine": (5, 0, 0), "Bone_Chest": (2, 0, 0), "Bone_Head": (0, 0, 0),
          "Bone_ArmL": (12, 0, -9), "Bone_ArmR": (12, 0, 9),
          "Bone_LegL": (-4, 0, 0), "Bone_LegR": (-4, 0, 0),
          "Bone_FootL": (2, 0, 0), "Bone_FootR": (2, 0, 0),
          "Bone_Antenna": (0, 0, 7)},
         {"Bone_Hips": (0, 0, -0.018), "Bone_LegL": (0, 0, 0.015),
          "Bone_LegR": (0, 0, 0.015)}),
        (16, {}, {}),
    ]
    for frame, rotations, locations in poses:
        key_pose(rig, frame, rotations, locations)

    strip = action.layers[0].strips[0]
    curves = strip.channelbag(rig.animation_data.action_slot).fcurves
    for curve in curves:
        for key in curve.keyframe_points:
            key.interpolation = "BEZIER"
            key.handle_left_type = "AUTO_CLAMPED"
            key.handle_right_type = "AUTO_CLAMPED"

    scene.render.fps = FPS
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    camera = bpy.data.objects.get("PreviewCamera_front-three-quarter")
    if camera is None:
        camera_data = bpy.data.cameras.new("LandingReviewCamera")
        camera = bpy.data.objects.new("LandingReviewCamera", camera_data)
        scene.collection.objects.link(camera)
        camera.location = (2.5, -5.8, 1.45)
        camera.rotation_euler = (Vector((0, 0, 1.0)) - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.type = "ORTHO"
        camera.data.ortho_scale = 2.9
    scene.camera = camera

    for frame, name in ((1, "impact"), (3, "release"), (7, "settle"), (12, "neutral")):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        scene.render.filepath = os.path.join(REVIEW_DIR, name + ".png")
        bpy.ops.render.render(write_still=True)

    filepath = os.path.join(CANDIDATE_DIR, "Splat_Land_Iter1203_Candidate.fbx")
    scene.frame_start, scene.frame_end = 1, 16
    scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    rig.animation_data.action = action
    bpy.ops.export_scene.fbx(
        filepath=filepath, use_selection=True, object_types={"ARMATURE"},
        bake_anim=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, add_leaf_bones=False,
        axis_forward="-Z", axis_up="Y", path_mode="AUTO")

    print("Candidate action:", action.name, tuple(action.frame_range), len(curves), "curves")
    print("Candidate FBX:", filepath)
    print("Pose renders:", REVIEW_DIR)


if __name__ == "__main__":
    main()
