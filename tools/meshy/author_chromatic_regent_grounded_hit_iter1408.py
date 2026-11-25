import bpy
import os
from mathutils import Vector, Euler

ROOT = os.path.abspath(
    "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/MeshySpenddown/"
    "20261031_223817_rainbow-kingdom-chromatic-rege_01a1952f"
)
EXPORTS = os.path.join(ROOT, "exports")
OUT = os.path.abspath(
    "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/"
    "20261101_chromatic-regent-animation-bundle-iter1408"
)
os.makedirs(OUT, exist_ok=True)

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

def import_glb(relative):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(EXPORTS, relative))
    imported = set(bpy.context.scene.objects) - before
    arms = [o for o in imported if o.type == "ARMATURE"]
    meshes = [o for o in imported if o.type == "MESH"]
    if len(arms) != 1:
        raise RuntimeError(f"{relative}: expected one armature, found {len(arms)}")
    return arms[0], meshes

clear_scene()
base_arm, base_meshes = import_glb("rig-and-locomotion/rigged_character.glb")
base_action = base_arm.animation_data.action
base_slot = base_arm.animation_data.action_slot
main_mesh = max(base_meshes, key=lambda o: len(o.data.vertices))
# The delivered clip GLBs contain unrelated tiny unrigged Icosphere objects.
# Keep only the actual skinned character mesh to avoid duplicate silhouettes.
for obj in list(bpy.context.scene.objects):
    if obj.type == "MESH" and obj != main_mesh:
        bpy.data.objects.remove(obj, do_unlink=True)
if len(main_mesh.vertex_groups) != 24:
    raise RuntimeError(f"Expected a skinned 24-group body mesh, got {len(main_mesh.vertex_groups)} groups")
if len(base_arm.data.bones) != 24:
    raise RuntimeError(f"Expected 24 bones, got {len(base_arm.data.bones)}")

actions = {}
# Capture the imported static bind-pose action for a stable lower-body reference.
scene = bpy.context.scene
base_arm.animation_data.action = base_action
base_arm.animation_data.action_slot = base_slot
scene.frame_set(int(round(base_action.frame_range[0])))
bpy.context.view_layer.update()
rest_pose = {pb.name: (pb.location.copy(), pb.rotation_quaternion.copy(), pb.scale.copy())
             for pb in base_arm.pose.bones}

def author_idle():
    idle = base_action.copy()
    idle.name = "ChromaticRegent_Idle"
    base_arm.animation_data.action = idle
    base_arm.animation_data.action_slot = idle.slots[0]
    rest = {pb.name: (pb.location.copy(), pb.rotation_quaternion.copy(), pb.scale.copy())
            for pb in base_arm.pose.bones}
    # Small spine/head breathing; this preserves the generated silhouette and
    # is an authored rig motion, not a Meshy idle clip.
    for frame, phase in ((1, 0.0), (16, 1.0), (31, 0.0), (46, -1.0), (61, 0.0)):
        bpy.context.scene.frame_set(frame)
        for pb in base_arm.pose.bones:
            loc, rot, scale = rest[pb.name]
            pb.location, pb.rotation_quaternion, pb.scale = loc, rot, scale
        for bone_name, delta in (
            ("LeftArm", Euler((0.78, 0, 0)).to_quaternion()),
            ("RightArm", Euler((-0.78, 0, 0)).to_quaternion()),
            ("LeftForeArm", Euler((0, 0, 0.14)).to_quaternion()),
            ("RightForeArm", Euler((0, 0, -0.14)).to_quaternion()),
            ("Spine", Euler((0, 0, 0.018 * phase)).to_quaternion()),
            ("Spine02", Euler((0.012 * phase, 0, 0)).to_quaternion()),
            ("Head", Euler((-0.012 * phase, 0, 0)).to_quaternion()),
        ):
            pb = base_arm.pose.bones.get(bone_name)
            if pb is not None:
                pb.rotation_quaternion = rest[pb.name][1] @ delta
        for pb in base_arm.pose.bones:
            pb.keyframe_insert(data_path="location", frame=frame, group=pb.name)
            pb.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=pb.name)
            pb.keyframe_insert(data_path="scale", frame=frame, group=pb.name)
    return idle

actions["Idle"] = (author_idle(), base_arm.animation_data.action_slot)

def import_action(label, path):
    temp_arm, temp_meshes = import_glb(path)
    action = temp_arm.animation_data.action
    slot = temp_arm.animation_data.action_slot
    if action is None or slot is None:
        raise RuntimeError(f"{path}: action/slot missing")
    if len(temp_arm.data.bones) != len(base_arm.data.bones):
        raise RuntimeError(f"{path}: rig bone count mismatch")
    expected = {b.name for b in base_arm.data.bones}
    actual = {b.name for b in temp_arm.data.bones}
    if expected != actual:
        raise RuntimeError(f"{path}: rig bone names do not match")
    # Keep the action datablock while deleting the duplicate mesh and armature.
    actions[label] = (action, slot)
    for obj in list(bpy.context.scene.objects):
        if obj not in (base_arm, main_mesh):
            bpy.data.objects.remove(obj, do_unlink=True)

import_action("Walk", "rig-and-locomotion/walking_glb.glb")
import_action("Run", "rig-and-locomotion/running_glb.glb")
import_action("Attack", "attack/animation_glb.glb")
import_action("Hit", "hit-reaction/animation_glb.glb")
import_action("Knockdown", "knockdown/animation_glb.glb")

# Keep Meshy's distinct flailing limb response but replace its extreme Hips
# translation/rotation (the part that lays the character on its back) with the
# source neutral Hips transform. Preserve authored/animated child limbs.
source_hit, source_slot = actions["Hit"]
upright_hit = source_hit.copy()
upright_hit.name = "ChromaticRegent_Hit_UprightCandidate"
upright_slot = upright_hit.slots[0]
channelbag = upright_hit.layers[0].strips[0].channelbags[0]
rest_values = {
    "location": (0.0, 0.0, 0.0),
    "rotation_quaternion": (1.0, 0.0, 0.0, 0.0),
    "scale": (1.0, 1.0, 1.0),
}
grounded_chain = {"Hips", "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
                  "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"}
for curve in channelbag.fcurves:
    if not curve.data_path.startswith('pose.bones['):
        continue
    bone_name = curve.data_path.split('"')[1]
    if bone_name not in grounded_chain:
        continue
    prop = curve.data_path.rsplit(".", 1)[-1]
    pose = rest_pose.get(bone_name)
    if pose is None:
        continue
    values = {"location": tuple(pose[0]), "rotation_quaternion": tuple(pose[1]),
              "scale": tuple(pose[2])}.get(prop)
    if values is None or curve.array_index >= len(values):
        continue
    for key in curve.keyframe_points:
        key.co.y = values[curve.array_index]
        key.handle_left.y = values[curve.array_index]
        key.handle_right.y = values[curve.array_index]
    curve.update()
actions["Hit"] = (upright_hit, upright_slot)

for name, (action, slot) in actions.items():
    action.name = "ChromaticRegent_" + name
    action.use_fake_user = True

scene = bpy.context.scene
scene.render.fps = 30
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 720
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.world = scene.world or bpy.data.worlds.new("RegentReviewWorld")
scene.world.color = (0.075, 0.075, 0.075)

def point(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()

cam_data = bpy.data.cameras.new("RegentReviewCamera")
cam = bpy.data.objects.new("RegentReviewCamera", cam_data)
scene.collection.objects.link(cam)
cam.location = (1.1, -4.8, 2.3)
cam.data.type = "ORTHO"
cam.data.ortho_scale = 3.0
point(cam, (0, 0, 0.95))
scene.camera = cam
for name, location, energy, size in (
    ("Key", (3, -4, 4), 950, 4),
    ("Fill", (-3, -2, 2), 550, 4),
    ("Rim", (0, 4, 3), 700, 3),
):
    data = bpy.data.lights.new(name, "AREA")
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    data.energy, data.shape, data.size = energy, "DISK", size
    point(obj, (0, 0, 0.9))

def set_action(label):
    action, slot = actions[label]
    base_arm.animation_data_create()
    base_arm.animation_data.action = action
    base_arm.animation_data.action_slot = slot
    low, high = action.frame_range
    frame = int(round((low + high) * 0.5))
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    return frame

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "ChromaticRegent_AnimationBundle.blend"))
for label in ("Idle", "Walk", "Run", "Attack", "Hit", "Knockdown"):
    frame = set_action(label)
    scene.render.filepath = os.path.join(OUT, "ChromaticRegent_" + label + ".png")
    bpy.ops.render.render(write_still=True)

# Unity-friendly candidate: one skinned character mesh, one armature, all named
# actions baked to FBX. No Unity scene or prefab is modified by this tool.
set_action("Idle")
bpy.ops.object.select_all(action="DESELECT")
base_arm.select_set(True)
main_mesh.select_set(True)
bpy.context.view_layer.objects.active = base_arm
bpy.ops.export_scene.fbx(
    filepath=os.path.join(OUT, "ChromaticRegent_AnimationBundle.fbx"),
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0.0,
    path_mode="COPY",
    embed_textures=True,
)
print(f"BUNDLE_DONE mesh_vertices={len(main_mesh.data.vertices)} bones={len(base_arm.data.bones)} actions={list(actions)}")




