"""Create a review-only run clip with folded, counter-swinging flippers.

This starts from the locally authored Iter588 rig/clip and leaves that source
untouched. The output is a separate FBX candidate for preview on the active
Unity skin; it is not wired into gameplay.
"""

import math
import os

import bpy


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SOURCE_BLEND = os.path.join(
    PROJECT,
    "Assets",
    "SpaceAndCrafts",
    "Art",
    "Animations",
    "Authoring",
    "Iter588",
    "Splat_Animations_Iter588.blend",
)
OUTPUT_DIR = os.path.join(
    PROJECT,
    "Assets",
    "SpaceAndCrafts",
    "Art",
    "Animations",
    "Authoring",
    "Iter688",
)
ARMATURE_NAME = "Splat_SkinRig"
ACTION_NAME = "Splat_Run_Iter588"


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=SOURCE_BLEND)

    armature = bpy.data.objects.get(ARMATURE_NAME)
    action = bpy.data.actions.get(ACTION_NAME)
    if armature is None or armature.type != "ARMATURE" or action is None:
        raise RuntimeError("Could not find Splat's authored run action and rig")

    slot = armature.animation_data.action_slot
    channelbag = action.layers[0].strips[0].channelbag(slot)
    if channelbag is None:
        raise RuntimeError("Run action has no animation curves")

    z_swing_degrees = {
        "Bone_ArmL": [-18.0, 0.0, 18.0, 0.0, -18.0],
        "Bone_ArmR": [18.0, 0.0, -18.0, 0.0, 18.0],
    }
    found_x = set()
    found_z = set()
    for curve in channelbag.fcurves:
        for bone_name, z_values in z_swing_degrees.items():
            if curve.data_path != f'pose.bones["{bone_name}"].rotation_euler':
                continue
            if curve.array_index == 0:
                for point in curve.keyframe_points:
                    point.co.y = math.radians(-34.0)
                    point.handle_left_type = "AUTO_CLAMPED"
                    point.handle_right_type = "AUTO_CLAMPED"
                found_x.add(bone_name)
            elif curve.array_index == 2:
                expected = [1, 7, 13, 19, 25]
                if len(curve.keyframe_points) != len(expected):
                    raise RuntimeError(f"Unexpected {bone_name} flipper key count")
                for point, frame, degrees in zip(curve.keyframe_points, expected, z_values):
                    point.co = (frame, math.radians(degrees))
                    point.handle_left_type = "AUTO_CLAMPED"
                    point.handle_right_type = "AUTO_CLAMPED"
                found_z.add(bone_name)

    if found_x != set(z_swing_degrees) or found_z != set(z_swing_degrees):
        raise RuntimeError(f"Missing arm channels: x={found_x}, z={found_z}")

    action.name = "Splat_Run_Iter688"
    armature.animation_data.action = action
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUTPUT_DIR, "Splat_Animations_Iter688.blend"))

    fbx_path = os.path.join(OUTPUT_DIR, "Splat_Run_Iter688.fbx")
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        object_types={"ARMATURE"},
        bake_anim=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
        add_leaf_bones=False,
        axis_forward="-Z",
        axis_up="Y",
        path_mode="AUTO",
    )
    print(f"Saved separate Iter688 run candidate: {fbx_path}")


if __name__ == "__main__":
    main()
