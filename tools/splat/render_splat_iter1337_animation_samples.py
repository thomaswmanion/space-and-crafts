"""Render key frames from Meshy Splat's landed, idle, and jump GLBs."""

from pathlib import Path
from mathutils import Vector
import bpy


ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f"
INPUTS = [
    ("IdleJump", BASE / "animation_glb.glb", ("Idle", "Regular_Jump")),
    ("Landing", BASE / "LandingAnimation/animation_glb.glb", ("retarget_clip",)),
]
OUT = BASE / "AnimationReview"
OUT.mkdir(parents=True, exist_ok=True)


def configure_scene(scene, corners):
    minimum = Vector((min(v[i] for v in corners) for i in range(3)))
    maximum = Vector((max(v[i] for v in corners) for i in range(3)))
    target = (minimum + maximum) * 0.5
    extent = maximum - minimum
    radius = max(extent.length, 1.0)

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 700
    scene.render.resolution_y = 700
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.view_settings.view_transform = "AgX"
    scene.render.film_transparent = False

    world = bpy.data.worlds.new("SplatAnimationReviewWorld")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.76, 0.81, 0.9, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.65

    camera_data = bpy.data.cameras.new("SplatAnimationReviewCamera")
    camera = bpy.data.objects.new("SplatAnimationReviewCamera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = max(extent.z * 1.5, extent.x * 1.7, extent.y * 1.7, 2.2)
    camera.location = target + Vector((-4.5, -6.0, 2.7)).normalized() * (radius * 2.7)
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()

    key_data = bpy.data.lights.new("SplatAnimationReviewKey", type="AREA")
    key_data.energy = 900
    key_data.shape = "DISK"
    key_data.size = radius * 2
    key = bpy.data.objects.new(key_data.name, key_data)
    scene.collection.objects.link(key)
    key.location = target + Vector((radius * 2, -radius * 3, radius * 3))
    key.rotation_euler = (target - key.location).to_track_quat("-Z", "Y").to_euler()

    fill_data = bpy.data.lights.new("SplatAnimationReviewFill", type="AREA")
    fill_data.energy = 450
    fill_data.shape = "DISK"
    fill_data.size = radius * 2.5
    fill = bpy.data.objects.new(fill_data.name, fill_data)
    scene.collection.objects.link(fill)
    fill.location = target + Vector((-radius * 2, radius * 2, radius * 1.5))
    fill.rotation_euler = (target - fill.location).to_track_quat("-Z", "Y").to_euler()


for label, glb, wanted_actions in INPUTS:
    if not glb.is_file():
        raise FileNotFoundError(glb)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(glb))
    scene = bpy.context.scene
    meshes = [obj for obj in scene.objects if obj.type == "MESH"]
    armatures = [obj for obj in scene.objects if obj.type == "ARMATURE"]
    if not meshes or not armatures:
        raise RuntimeError(f"{glb.name} did not import a skinned character")
    armature = armatures[0]
    corners = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    configure_scene(scene, corners)

    action_by_name = {action.name: action for action in bpy.data.actions}
    print(f"SPLAT_ACTIONS {label} " + "; ".join(
        f"{action.name}:{tuple(action.frame_range)}" for action in bpy.data.actions
    ))
    for action_name in wanted_actions:
        action = action_by_name.get(action_name)
        if action is None:
            print(f"SPLAT_ACTION_MISSING {label} {action_name}")
            continue
        armature.animation_data_create()
        armature.animation_data.action = action
        start, end = map(float, action.frame_range)
        fractions = (0.0, 0.25, 0.5, 0.75, 1.0)
        samples = sorted(set(int(round(start + (end - start) * f)) for f in fractions))
        for frame in samples:
            scene.frame_set(frame)
            bpy.context.view_layer.update()
            scene.render.filepath = str(OUT / f"{label}_{action_name}_{frame:04d}.png")
            bpy.ops.render.render(write_still=True)
            scales = []
            for bone_name in ("Hips", "Spine", "Spine01", "Spine02", "Head", "LeftUpLeg", "RightUpLeg"):
                bone = armature.pose.bones.get(bone_name)
                if bone is not None:
                    scales.append(f"{bone_name}={tuple(round(v, 4) for v in bone.scale)}")
            print(f"SPLAT_SAMPLE {label}/{action_name} frame={frame} " + " ".join(scales))
