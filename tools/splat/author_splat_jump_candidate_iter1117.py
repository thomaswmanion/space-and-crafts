"""Author a clearer open-glide jump study on Splat's active 19-bone Iter378 rig."""

import math
import os
import bpy


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SOURCE_FBX = os.path.join(PROJECT, "Assets", "SpaceAndCrafts", "Resources", "Characters", "Splat_Rigged_Candidate_Iter378.fbx")
OUTPUT_DIR = os.path.join(PROJECT, "Assets", "SpaceAndCrafts", "Art", "Animations", "Authoring", "Iter1117")
ARMATURE_NAME = "Splat_SkinRig"


def key_pose(armature, frame, rotations):
    rotations = rotations or {}
    for bone in armature.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = tuple(math.radians(value) for value in rotations.get(bone.name, (0.0, 0.0, 0.0)))
        bone.location = (0.0, 0.0, 0.0)
        bone.scale = (1.0, 1.0, 1.0)
        bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone.name)
        bone.keyframe_insert(data_path="location", frame=frame, group=bone.name)


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE_FBX)
    scene = bpy.context.scene
    armature = bpy.data.objects.get(ARMATURE_NAME)
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("Active Iter378 Splat armature was not found in its FBX")
    bone_names = {bone.name for bone in armature.data.bones}
    for required in ("Bone_ArmTipL", "Bone_ArmTipR", "Bone_LegL", "Bone_LegR"):
        if required not in bone_names:
            raise RuntimeError(f"Active rig is missing {required}")

    action = bpy.data.actions.new("Splat_Jump_Iter1117")
    action.use_fake_user = True
    armature.animation_data_create()
    armature.animation_data.action = action
    sequence = [
        # Brief anticipation and a compact, recognizable crouch.
        (1, {"Bone_Spine": (5, 0, 0), "Bone_Chest": (2, 0, 0), "Bone_Head": (-3, 0, 0),
             "Bone_ArmL": (-26, 0, -9), "Bone_ArmR": (-26, 0, 9),
             "Bone_ArmTipL": (12, 0, -8), "Bone_ArmTipR": (12, 0, 8),
             "Bone_LegL": (-7, 0, 0), "Bone_LegR": (-7, 0, 0), "Bone_Antenna": (0, 0, 4)}),
        # Launch: arms trail and the antenna follows the upward acceleration.
        (4, {"Bone_Spine": (-7, 0, 0), "Bone_Chest": (-3, 0, 0), "Bone_Head": (4, 0, 0),
             "Bone_ArmL": (-34, 0, -14), "Bone_ArmR": (-34, 0, 14),
             "Bone_ArmTipL": (22, 0, -10), "Bone_ArmTipR": (22, 0, 10),
             "Bone_LegL": (12, 0, 0), "Bone_LegR": (12, 0, 0), "Bone_Antenna": (0, 0, -10)}),
        # Ascent and apex: the distal flippers lag and then counter-swing.
        (10, {"Bone_Spine": (-4, 0, 0), "Bone_Chest": (-2, 0, 0), "Bone_Head": (2, 0, 0),
              "Bone_ArmL": (8, 0, -16), "Bone_ArmR": (8, 0, 16),
              "Bone_ArmTipL": (22, 0, -10), "Bone_ArmTipR": (22, 0, 10),
              "Bone_LegL": (10, 0, 0), "Bone_LegR": (10, 0, 0), "Bone_Antenna": (0, 0, -12)}),
        (16, {"Bone_Spine": (-5, 0, 0), "Bone_Chest": (-2, 0, 0), "Bone_Head": (4, 0, 0),
              "Bone_ArmL": (31, 0, -20), "Bone_ArmR": (25, 0, 17),
              "Bone_ArmTipL": (26, 0, 14), "Bone_ArmTipR": (-20, 0, -14),
              "Bone_LegL": (9, 0, 0), "Bone_LegR": (11, 0, 0), "Bone_Antenna": (0, 0, -15)}),
        # Descending brace: turn the flippers forward and extend the feet.
        (22, {"Bone_Spine": (-2, 0, 0), "Bone_Chest": (0, 0, 0), "Bone_Head": (2, 0, 0),
              "Bone_ArmL": (11, 0, -12), "Bone_ArmR": (9, 0, 12),
              "Bone_ArmTipL": (-16, 0, 10), "Bone_ArmTipR": (-14, 0, -10),
              "Bone_LegL": (-4, 0, 0), "Bone_LegR": (-4, 0, 0), "Bone_Antenna": (0, 0, 8)}),
        # Touchdown and recovery; rotate joints, never scale the body.
        (26, {"Bone_Spine": (8, 0, 0), "Bone_Chest": (4, 0, 0), "Bone_Head": (-1, 0, 0),
              "Bone_ArmL": (-18, 0, -12), "Bone_ArmR": (-18, 0, 12),
              "Bone_ArmTipL": (-8, 0, -8), "Bone_ArmTipR": (-8, 0, 8),
              "Bone_LegL": (-8, 0, 0), "Bone_LegR": (-8, 0, 0), "Bone_Antenna": (0, 0, -8)}),
        (30, {"Bone_Spine": (-3, 0, 0), "Bone_Chest": (-1, 0, 0), "Bone_Head": (1, 0, 0),
              "Bone_ArmL": (-12, 0, -4), "Bone_ArmR": (-12, 0, 4),
              "Bone_ArmTipL": (2, 0, -2), "Bone_ArmTipR": (2, 0, 2),
              "Bone_LegL": (0, 0, 0), "Bone_LegR": (0, 0, 0), "Bone_Antenna": (0, 0, 6)}),
        (36, {}),
    ]
    for frame, pose in sequence:
        key_pose(armature, frame, pose)
    strip = action.layers[0].strips[0]
    curves = strip.channelbag(armature.animation_data.action_slot).fcurves
    for curve in curves:
        for key in curve.keyframe_points:
            key.interpolation = "BEZIER"
            key.handle_left_type = "AUTO_CLAMPED"
            key.handle_right_type = "AUTO_CLAMPED"

    scene.render.fps = 30
    scene.frame_start, scene.frame_end = 1, 36
    filepath = os.path.join(OUTPUT_DIR, "Splat_Jump_Iter1117.fbx")
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=filepath, use_selection=True, object_types={"ARMATURE"},
        bake_anim=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, add_leaf_bones=False,
        axis_forward="-Z", axis_up="Y", path_mode="AUTO")
    print(f"SPLAT_JUMP_CANDIDATE output={filepath} bones={len(armature.data.bones)} frames=1-36 keys={len(sequence)}")


if __name__ == "__main__":
    main()
