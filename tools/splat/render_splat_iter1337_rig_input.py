"""Render both sides of the local Splat rig-input GLB for visual inspection."""

from pathlib import Path
from mathutils import Vector
import bpy


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/Splat_Meshy_Iter1337_RigInput.glb"
OUT_DIR = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
scene = bpy.context.scene
mesh_objects = [o for o in scene.objects if o.type == "MESH"]
if not mesh_objects:
    raise RuntimeError("No mesh in the rig-input GLB")

corners = [obj.matrix_world @ Vector(corner) for obj in mesh_objects for corner in obj.bound_box]
minimum = Vector((min(v[i] for v in corners) for i in range(3)))
maximum = Vector((max(v[i] for v in corners) for i in range(3)))
target = (minimum + maximum) * 0.5
extent = maximum - minimum
radius = max(extent.length, 1.0)

scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1024
scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.render.image_settings.color_mode = "RGBA"
scene.render.image_settings.color_depth = "8"
scene.view_settings.view_transform = "AgX"

world = bpy.data.worlds.new("SplatPreviewWorld")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.76, 0.81, 0.9, 1.0)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.65

camera_data = bpy.data.cameras.new("SplatPreviewCamera")
camera = bpy.data.objects.new("SplatPreviewCamera", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = max(extent.z * 1.5, extent.x * 1.7, extent.y * 1.7, 2.2)

for index, (direction, name) in enumerate([
    (Vector((4.5, 6.0, 2.7)), "three_quarter_a"),
    (Vector((-4.5, -6.0, 2.7)), "three_quarter_b"),
]):
    camera.location = target + direction.normalized() * (radius * 2.7)
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    for obj in [o for o in scene.objects if o.type == "LIGHT"]:
        bpy.data.objects.remove(obj, do_unlink=True)
    key_data = bpy.data.lights.new(f"SplatKey{index}", type="AREA")
    key_data.energy = 900
    key_data.shape = "DISK"
    key_data.size = radius * 2
    key = bpy.data.objects.new(key_data.name, key_data)
    scene.collection.objects.link(key)
    key.location = target + Vector((radius * 2, -radius * 3, radius * 3))
    key.rotation_euler = (target - key.location).to_track_quat("-Z", "Y").to_euler()
    fill_data = bpy.data.lights.new(f"SplatFill{index}", type="AREA")
    fill_data.energy = 450
    fill_data.shape = "DISK"
    fill_data.size = radius * 2.5
    fill = bpy.data.objects.new(fill_data.name, fill_data)
    scene.collection.objects.link(fill)
    fill.location = target + Vector((-radius * 2, radius * 2, radius * 1.5))
    fill.rotation_euler = (target - fill.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(OUT_DIR / f"Splat_Meshy_Iter1337_RigInput_{name}.png")
    bpy.ops.render.render(write_still=True)
    print(f"SPLAT_PREVIEW {name} {scene.render.filepath}")
