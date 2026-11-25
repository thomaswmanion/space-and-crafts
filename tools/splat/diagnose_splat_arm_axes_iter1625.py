import bpy
from mathutils import Quaternion

blend_path = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1623\Splat_ReadableXYFootLock_Run_Iter1623.blend"
bpy.ops.wm.open_mainfile(filepath=blend_path)
scene = bpy.context.scene
scene.frame_set(9)
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
bpy.context.view_layer.update()

for side in ("Left", "Right"):
    for target_name, parent_name in ((side + "Shoulder", side + "ForeArm"),
                                     (side + "Arm", side + "ForeArm")):
        bone = rig.pose.bones[target_name]
        endpoint = rig.pose.bones[parent_name]
        base = bone.rotation_quaternion.copy()
        before = endpoint.tail.copy()
        rows = []
        for axis_index, axis in enumerate(((1, 0, 0), (0, 1, 0), (0, 0, 1))):
            for angle in (-0.5, 0.5):
                bone.rotation_quaternion = base @ Quaternion(axis, angle)
                bpy.context.view_layer.update()
                after = endpoint.tail.copy()
                rows.append(("XYZ"[axis_index], angle, round(after.z - before.z, 3),
                             round(after.x - before.x, 3), round(after.y - before.y, 3)))
        bone.rotation_quaternion = base
        print("ARM_AXIS", target_name, "endpoint", endpoint.name, "base", tuple(round(v, 3) for v in before), rows)
