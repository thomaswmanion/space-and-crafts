import bpy
import math
import os
from mathutils import Euler

bundle = os.path.abspath(
    "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/"
    "20261101_chromatic-regent-animation-bundle/ChromaticRegent_AnimationBundle.blend"
)
out = os.path.join(os.path.dirname(bundle), "IdleVariants")
os.makedirs(out, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=bundle)
scene = bpy.context.scene
arm = next(o for o in scene.objects if o.type == "ARMATURE")
action = bpy.data.actions.get("ChromaticRegent_Idle")
arm.animation_data.action = action
arm.animation_data.action_slot = action.slots[0]
scene.frame_set(31)
arm.animation_data.action = None
bpy.context.view_layer.update()
rest = {name: arm.pose.bones[name].rotation_quaternion.copy()
        for name in ("LeftArm", "RightArm", "LeftForeArm", "RightForeArm")}
camera = scene.camera
scene.render.resolution_x = 520
scene.render.resolution_y = 520

trials = [
    ("X_opp", (1, 0, 0), 1, -1),
    ("X_same", (1, 0, 0), 1, 1),
    ("Z_opp", (0, 0, 1), 1, -1),
    ("Z_same", (0, 0, 1), 1, 1),
    ("Y_opp", (0, 1, 0), 1, -1),
    ("Y_same", (0, 1, 0), 1, 1),
]
for label, axis, left_sign, right_sign in trials:
    radians = math.radians(65)
    delta_l = Euler(tuple(c * radians for c in axis)).to_quaternion()
    delta_r = Euler(tuple(c * radians for c in axis)).to_quaternion()
    arm.pose.bones["LeftArm"].rotation_quaternion = rest["LeftArm"] @ (delta_l if left_sign > 0 else delta_l.inverted())
    arm.pose.bones["RightArm"].rotation_quaternion = rest["RightArm"] @ (delta_r if right_sign > 0 else delta_r.inverted())
    # Add a tiny matching elbow softness only; keep all limbs connected to the rig.
    for side, sign in (("Left", left_sign), ("Right", right_sign)):
        pb = arm.pose.bones[side + "ForeArm"]
        pb.rotation_quaternion = rest[pb.name] @ Euler((0, 0, math.radians(8 * sign))).to_quaternion()
    bpy.context.view_layer.update()
    scene.render.filepath = os.path.join(out, label + ".png")
    bpy.ops.render.render(write_still=True)
