"""Capture small shoulder-pivot pose variants on the isolated Meshy Splat rig."""
import bpy, json, os
from mathutils import Quaternion

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BLEND = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1389\Splat_RotationOnly_Gait_Iter1389.blend"
OUT_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1390"
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=BLEND)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
rig.animation_data_clear()
trial_defs = [
    ("rest", None, 0, 0),
    ("x_same_pos", (1, 0, 0), .22, .22),
    ("x_same_neg", (1, 0, 0), -.22, -.22),
    ("x_opposed", (1, 0, 0), .22, -.22),
    ("y_same_pos", (0, 1, 0), .22, .22),
    ("y_same_neg", (0, 1, 0), -.22, -.22),
    ("y_opposed", (0, 1, 0), .22, -.22),
    ("z_same_pos", (0, 0, 1), .22, .22),
    ("z_same_neg", (0, 0, 1), -.22, -.22),
    ("z_opposed", (0, 0, 1), .22, -.22),
]
for index, (label, axis, left, right) in enumerate(trial_defs, 1):
    for bone in rig.pose.bones:
        bone.rotation_mode = "QUATERNION"
        bone.rotation_quaternion = Quaternion((1, 0, 0), 0)
        bone.location = (0, 0, 0)
        bone.scale = (1, 1, 1)
    if axis is not None:
        rig.pose.bones["LeftShoulder"].rotation_quaternion = Quaternion(axis, left)
        rig.pose.bones["RightShoulder"].rotation_quaternion = Quaternion(axis, right)
    scene.frame_set(1)
    bpy.context.view_layer.update()
    path = os.path.join(OUT_DIR, f"Splat_Iter1390_{index:02d}_{label}.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("ITER1390_POSE", label, "left_rad", left, "right_rad", right, "image", os.path.basename(path))
with open(os.path.join(OUT_DIR, "Splat_Iter1390_PoseSweep.json"), "w", encoding="utf-8") as handle:
    json.dump({"source": os.path.basename(BLEND), "variants": [
        {"name": name, "axis": axis, "left_radians": left, "right_radians": right}
        for name, axis, left, right in trial_defs
    ], "limitations": "Offline static pose sweep only; no skeleton rest-pose edit, gameplay capture, or human review."}, handle, indent=2)
