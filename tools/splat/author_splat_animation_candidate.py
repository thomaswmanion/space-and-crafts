"""Author full-body animation candidates on Splat's existing rig.

This creates a review candidate only: it does not overwrite the production blend
or FBX. The existing mesh, skin weights, materials, and bone hierarchy are used.
"""

import math
import os
import bpy
from mathutils import Vector


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SOURCE_BLEND = os.path.join(PROJECT, "Assets", "SpaceAndCrafts", "Art", "Characters", "Splat", "Splat_Rigged.blend")
CANDIDATE_DIR = os.path.join(PROJECT, "Assets", "SpaceAndCrafts", "Art", "Animations", "Authoring", "Iter588")
REVIEW_DIR = os.path.join(PROJECT, "Logs", "score-comparison", "splat-animation-authoring-iter588")
ARMATURE_NAME = "Splat_SkinRig"
FPS = 30


def deg(values):
    return tuple(math.radians(value) for value in values)


def key_pose(armature, frame, rotations=None, locations=None):
    rotations = rotations or {}
    locations = locations or {}
    for bone in armature.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = deg(rotations.get(bone.name, (0.0, 0.0, 0.0)))
        bone.location = locations.get(bone.name, (0.0, 0.0, 0.0))
        bone.scale = (1.0, 1.0, 1.0)
        # Complete keys on every rig bone make this clip self-contained and
        # prevent a transition from inheriting the previous action's pose.
        bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone.name)
        bone.keyframe_insert(data_path="location", frame=frame, group=bone.name)


def set_action_curves(action, armature, cyclic=False):
    strip = action.layers[0].strips[0]
    curves = strip.channelbag(armature.animation_data.action_slot).fcurves
    for curve in curves:
        for key in curve.keyframe_points:
            key.interpolation = "BEZIER"
            key.handle_left_type = "AUTO_CLAMPED"
            key.handle_right_type = "AUTO_CLAMPED"
        if cyclic:
            modifier = curve.modifiers.new(type="CYCLES")
            modifier.mode_before = "REPEAT"
            modifier.mode_after = "REPEAT"


def create_action(armature, scene, name, fps, keys, cyclic=False):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    armature.animation_data_create()
    armature.animation_data.action = action
    scene.render.fps = fps
    scene.frame_start = keys[0][0]
    scene.frame_end = keys[-1][0]
    for frame, rotations, locations in keys:
        key_pose(armature, frame, rotations, locations)
    set_action_curves(action, armature, cyclic=cyclic)
    return action


def capture_pose(armature, scene, camera, action, frame, name):
    armature.animation_data.action = action
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    scene.camera = camera
    scene.render.filepath = os.path.join(REVIEW_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)


def export_action(armature, action, name):
    filepath = os.path.join(CANDIDATE_DIR, name + ".fbx")
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    armature.animation_data.action = action
    bpy.ops.export_scene.fbx(
        filepath=filepath,
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


def main():
    os.makedirs(CANDIDATE_DIR, exist_ok=True)
    os.makedirs(REVIEW_DIR, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=SOURCE_BLEND)
    scene = bpy.context.scene
    armature = bpy.data.objects.get(ARMATURE_NAME)
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("Splat's authored armature was not found")

    scene.render.fps = FPS
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"

    # Idle: quiet breathing, a small curious glance, and a soft antenna follow.
    idle = create_action(armature, scene, "Splat_Idle_Iter588", FPS, [
        (1, {}, {}),
        (16, {
            "Bone_Spine": (1.5, 0.0, -1.0),
            "Bone_Chest": (-1.0, 0.0, 0.8),
            "Bone_Head": (0.0, 2.5, 1.2),
            "Bone_Antenna": (0.0, 0.0, -6.0),
            "Bone_ArmL": (3.0, 0.0, -3.0),
            "Bone_ArmR": (-2.0, 0.0, 2.0),
        }, {"Bone_Hips": (0.0, 0.0, 0.012)}),
        (31, {}, {}),
    ], cyclic=True)

    # Run: opposing arms and feet with a low, single-beat body counter-motion.
    run = create_action(armature, scene, "Splat_Run_Iter588", FPS, [
        (1, {
            "Bone_ArmL": (22.0, 0.0, -20.0),
            "Bone_ArmR": (-22.0, 0.0, 20.0),
            "Bone_LegL": (-22.0, 0.0, 0.0),
            "Bone_LegR": (22.0, 0.0, 0.0),
            "Bone_Spine": (2.0, 0.0, 0.0),
            "Bone_Head": (-2.0, 0.0, 0.0),
            "Bone_Antenna": (0.0, 0.0, 5.0),
        }, {"Bone_Hips": (0.0, 0.0, 0.0), "Bone_LegL": (0.0, -0.06, 0.0)}),
        (7, {
            "Bone_ArmL": (0.0, 0.0, -8.0),
            "Bone_ArmR": (0.0, 0.0, 8.0),
            "Bone_LegL": (0.0, 0.0, 0.0),
            "Bone_LegR": (0.0, 0.0, 0.0),
            "Bone_Antenna": (0.0, 0.0, 0.0),
        }, {"Bone_Hips": (0.0, 0.0, 0.018), "Bone_LegL": (0.0, 0.0, 0.0)}),
        (13, {
            "Bone_ArmL": (-22.0, 0.0, 20.0),
            "Bone_ArmR": (22.0, 0.0, -20.0),
            "Bone_LegL": (22.0, 0.0, 0.0),
            "Bone_LegR": (-22.0, 0.0, 0.0),
            "Bone_Spine": (2.0, 0.0, 0.0),
            "Bone_Head": (-2.0, 0.0, 0.0),
            "Bone_Antenna": (0.0, 0.0, -5.0),
        }, {"Bone_Hips": (0.0, 0.0, 0.0), "Bone_LegR": (0.0, -0.06, 0.0)}),
        (19, {
            "Bone_ArmL": (0.0, 0.0, 8.0),
            "Bone_ArmR": (0.0, 0.0, -8.0),
            "Bone_LegL": (0.0, 0.0, 0.0),
            "Bone_LegR": (0.0, 0.0, 0.0),
            "Bone_Antenna": (0.0, 0.0, 0.0),
        }, {"Bone_Hips": (0.0, 0.0, 0.018), "Bone_LegR": (0.0, 0.0, 0.0)}),
        (25, {
            "Bone_ArmL": (22.0, 0.0, -20.0),
            "Bone_ArmR": (-22.0, 0.0, 20.0),
            "Bone_LegL": (-22.0, 0.0, 0.0),
            "Bone_LegR": (22.0, 0.0, 0.0),
            "Bone_Spine": (2.0, 0.0, 0.0),
            "Bone_Head": (-2.0, 0.0, 0.0),
            "Bone_Antenna": (0.0, 0.0, 5.0),
        }, {"Bone_Hips": (0.0, 0.0, 0.0), "Bone_LegL": (0.0, -0.06, 0.0)}),
    ], cyclic=True)

    # Jump: fold at takeoff, open at the apex, extend the feet for descent.
    jump = create_action(armature, scene, "Splat_Jump_Iter588", FPS, [
        (1, {
            "Bone_ArmL": (30.0, 0.0, -18.0), "Bone_ArmR": (30.0, 0.0, 18.0),
            "Bone_Spine": (-3.0, 0.0, 0.0), "Bone_Head": (3.0, 0.0, 0.0),
        }, {}),
        (8, {
            "Bone_ArmL": (-35.0, 0.0, -35.0), "Bone_ArmR": (-35.0, 0.0, 35.0),
            "Bone_LegL": (12.0, 0.0, 0.0), "Bone_LegR": (12.0, 0.0, 0.0),
            "Bone_Spine": (-4.0, 0.0, 0.0), "Bone_Head": (2.0, 0.0, 0.0),
            "Bone_Antenna": (0.0, 0.0, 12.0),
        }, {"Bone_Hips": (0.0, 0.0, 0.025), "Bone_LegL": (0.0, 0.0, -0.035), "Bone_LegR": (0.0, 0.0, -0.035)}),
        (18, {
            "Bone_ArmL": (-48.0, 0.0, -25.0), "Bone_ArmR": (-48.0, 0.0, 25.0),
            "Bone_LegL": (12.0, 0.0, 0.0), "Bone_LegR": (12.0, 0.0, 0.0),
            "Bone_Spine": (-7.0, 0.0, 0.0), "Bone_Head": (-4.0, 0.0, 0.0),
            "Bone_Antenna": (0.0, 0.0, 16.0),
        }, {"Bone_Hips": (0.0, 0.0, 0.0), "Bone_LegL": (0.0, 0.0, -0.05), "Bone_LegR": (0.0, 0.0, -0.05)}),
        (28, {
            "Bone_ArmL": (20.0, 0.0, -12.0), "Bone_ArmR": (20.0, 0.0, 12.0),
            "Bone_LegL": (-8.0, 0.0, 0.0), "Bone_LegR": (-8.0, 0.0, 0.0),
            "Bone_Spine": (4.0, 0.0, 0.0), "Bone_Head": (2.0, 0.0, 0.0),
            "Bone_Antenna": (0.0, 0.0, -10.0),
        }, {"Bone_Hips": (0.0, 0.0, -0.01)}),
        (36, {}, {}),
    ])

    # Land: one compact compression and rebound. Keep every scale channel untouched.
    land = create_action(armature, scene, "Splat_Land_Iter588", FPS, [
        (1, {
            "Bone_Spine": (12.0, 0.0, 0.0), "Bone_Chest": (6.0, 0.0, 0.0),
            "Bone_Head": (4.0, 0.0, 0.0), "Bone_ArmL": (25.0, 0.0, -20.0),
            "Bone_ArmR": (25.0, 0.0, 20.0), "Bone_LegL": (-8.0, 0.0, 0.0),
            "Bone_LegR": (-8.0, 0.0, 0.0), "Bone_Antenna": (0.0, 0.0, -14.0),
        }, {"Bone_Hips": (0.0, 0.0, -0.055), "Bone_LegL": (0.0, 0.0, 0.045), "Bone_LegR": (0.0, 0.0, 0.045)}),
        (5, {
            "Bone_Spine": (-4.0, 0.0, 0.0), "Bone_Chest": (-2.0, 0.0, 0.0),
            "Bone_Head": (-2.0, 0.0, 0.0), "Bone_Antenna": (0.0, 0.0, 8.0),
        }, {"Bone_Hips": (0.0, 0.0, 0.018), "Bone_LegL": (0.0, 0.0, -0.015), "Bone_LegR": (0.0, 0.0, -0.015)}),
        (12, {}, {}),
    ])

    # Save a separate authored candidate blend, keeping production source untouched.
    candidate_blend = os.path.join(CANDIDATE_DIR, "Splat_Animations_Iter588.blend")
    bpy.ops.wm.save_as_mainfile(filepath=candidate_blend)

    camera = bpy.data.objects.get("PreviewCamera_front-three-quarter")
    if camera is None:
        camera_data = bpy.data.cameras.new("AnimationStudyCamera")
        camera = bpy.data.objects.new("AnimationStudyCamera", camera_data)
        scene.collection.objects.link(camera)
        camera.location = (2.5, -5.8, 1.45)
        direction = Vector((0, 0, 1.0)) - camera.location
        camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
        camera.data.type = "ORTHO"
        camera.data.ortho_scale = 2.9
    capture_pose(armature, scene, camera, idle, 16, "splat-idle-iter588")
    capture_pose(armature, scene, camera, run, 7, "splat-run-phase-a-iter588")
    capture_pose(armature, scene, camera, run, 19, "splat-run-phase-b-iter588")
    capture_pose(armature, scene, camera, jump, 18, "splat-jump-apex-iter588")
    capture_pose(armature, scene, camera, land, 1, "splat-land-impact-iter588")

    for action, name in ((idle, "Splat_Idle_Iter588"), (run, "Splat_Run_Iter588"),
                         (jump, "Splat_Jump_Iter588"), (land, "Splat_Land_Iter588")):
        export_action(armature, action, name)

    def action_curve_count(action):
        # Blender 5.x stores curves in a channel bag attached to an action slot.
        slot = armature.animation_data.action_slot
        bag = action.layers[0].strips[0].channelbag(slot)
        return len(bag.fcurves) if bag is not None else 0

    print("Authored Splat actions:", [(a.name, tuple(a.frame_range), action_curve_count(a))
                                      for a in (idle, run, jump, land)])
    print("Candidate blend:", candidate_blend)
    print("Animation FBXs:", CANDIDATE_DIR)
    print("Pose review images:", REVIEW_DIR)


if __name__ == "__main__":
    main()
