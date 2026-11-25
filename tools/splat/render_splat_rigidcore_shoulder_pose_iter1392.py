"""Compare shoulder poses on the isolated Hips-core skin candidate."""
import bpy, json, os
from mathutils import Quaternion

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BLEND = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1388\Splat_Meshy_Iter1388_ColorBoundarySkinCandidate.blend"
OUT_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1392"
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=BLEND)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
rig.animation_data_clear()
trials = [
    ("neutral", 0.0, 0.0),
    ("both_minus_035", -.35, -.35),
    ("both_minus_060", -.60, -.60),
    ("both_minus_085", -.85, -.85),
    ("both_plus_060", .60, .60),
    ("opposed_left_minus", -.60, .60),
    ("opposed_right_minus", .60, -.60),
]
for number, (label, left_angle, right_angle) in enumerate(trials, 1):
    for bone in rig.pose.bones:
        bone.rotation_mode = "QUATERNION"
        bone.rotation_quaternion = Quaternion((1, 0, 0), 0)
        bone.location = (0, 0, 0)
        bone.scale = (1, 1, 1)
    rig.pose.bones["LeftShoulder"].rotation_quaternion = Quaternion((1, 0, 0), left_angle)
    rig.pose.bones["RightShoulder"].rotation_quaternion = Quaternion((1, 0, 0), right_angle)
    scene.frame_set(1)
    bpy.context.view_layer.update()
    path = os.path.join(OUT_DIR, f"Splat_Iter1392_{number:02d}_{label}.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("ITER1392_POSE", label, left_angle, right_angle, os.path.basename(path))
with open(os.path.join(OUT_DIR, "Splat_Iter1392_PoseSweep.json"), "w", encoding="utf-8") as handle:
    json.dump({"source": os.path.basename(BLEND), "trials": [
        {"name": name, "left_shoulder_x_radians": left, "right_shoulder_x_radians": right}
        for name, left, right in trials
    ], "limitations": "Offline static pose sweep on a rejected candidate-only weight layout. No active files or Unity scene changed."}, handle, indent=2)
