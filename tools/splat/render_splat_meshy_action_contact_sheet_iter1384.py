"""Create offline pose contact sheets for the isolated Iter1384 Meshy FBXs."""
import os
import sys
from array import array

import bpy
from mathutils import Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
FOLDER = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1384"
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if len(ARGS) < 3:
    raise RuntimeError("Usage: <fbx> <action substring> <output png> [front|back]")
FBX, ACTION_MATCH, OUTPUT = ARGS[:3]
VIEW_SIDE = ARGS[3].lower() if len(ARGS) > 3 else "front"
TEXTURE = FOLDER + r"\Splat_Meshy_ImageTo3D_textures\base_color.png"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=True)
scene = bpy.context.scene
scene.world = bpy.data.worlds.new("SplatCandidateWorld")
scene.world.color = (0.08, 0.09, 0.12)

mesh_objs = [obj for obj in scene.objects if obj.type == "MESH"]
armatures = [obj for obj in scene.objects if obj.type == "ARMATURE"]
if not mesh_objs or not armatures:
    raise RuntimeError(f"Expected a skinned mesh and armature; found {len(mesh_objs)} meshes, {len(armatures)} armatures")

for obj in mesh_objs:
    for mat in obj.data.materials:
        if not mat:
            continue
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        shader = next((node for node in nodes if node.type == "BSDF_PRINCIPLED"), None)
        if shader is None:
            continue
        image_node = next((node for node in nodes if node.type == "TEX_IMAGE" and node.image), None)
        if image_node is None and os.path.isfile(TEXTURE):
            image_node = nodes.new("ShaderNodeTexImage")
            image_node.image = bpy.data.images.load(TEXTURE, check_existing=True)
            mat.node_tree.links.new(image_node.outputs["Color"], shader.inputs["Base Color"])
        shader.inputs["Roughness"].default_value = 0.58

armature = armatures[0]
if armature.animation_data is None:
    raise RuntimeError("Imported armature has no animation data")
actions = list(bpy.data.actions)
action = next((candidate for candidate in actions if ACTION_MATCH.lower() in candidate.name.lower()), None)
if action is None:
    raise RuntimeError(f"Action substring {ACTION_MATCH!r} not found: {[candidate.name for candidate in actions]}")
armature.animation_data.action = action
scene.frame_start = int(action.frame_range[0])
scene.frame_end = int(action.frame_range[1])
start, end = action.frame_range
sample_frames = [round(start + (end - start) * t) for t in (0.0, 0.24, 0.50, 0.76, 1.0)]

all_corners = [obj.matrix_world @ Vector(corner) for obj in mesh_objs for corner in obj.bound_box]
base_min_y = min(point.y for point in all_corners)
base_center_y = (min(point.y for point in all_corners) + max(point.y for point in all_corners)) * 0.5
base_center_z = sum(point.z for point in all_corners) / len(all_corners)
base_width = max(point.x for point in all_corners) - min(point.x for point in all_corners)
base_height = max(point.y for point in all_corners) - min(point.y for point in all_corners)
spacing = max(base_width * 1.2, 1.25)
temporary_objects = []
render_paths = []

for index, frame in enumerate(sample_frames):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    offset = 0.0
    for obj in mesh_objs:
        evaluated = obj.evaluated_get(depsgraph)
        baked_mesh = evaluated.to_mesh()
        snapshot_mesh = baked_mesh.copy()
        evaluated.to_mesh_clear()
        snapshot = bpy.data.objects.new(f"Pose_{index}_{obj.name}", snapshot_mesh)
        scene.collection.objects.link(snapshot)
        snapshot.matrix_world = obj.matrix_world.copy()
        snapshot.location.x += offset
        temporary_objects.append(snapshot)

scene.frame_set(scene.frame_start)
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 360
scene.render.resolution_y = 360
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.view_settings.view_transform = "AgX"

def area_light(name, loc, energy, size):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    obj.rotation_euler = (Vector((0.0, base_center_y, base_center_z)) - obj.location).to_track_quat("-Z", "Y").to_euler()

area_light("Key", Vector((-2.5, base_center_y + 2.8, base_center_z + 3.2)), 520, 3.0)
area_light("Fill", Vector((2.8, base_center_y + 1.5, base_center_z + 2.2)), 300, 3.2)
area_light("Rim", Vector((0.0, base_center_y + 2.6, base_center_z - 2.0)), 380, 2.5)

floor_mesh = bpy.data.meshes.new("PreviewGroundMesh")
floor_mesh.from_pydata([(-10, base_min_y, -10), (10, base_min_y, -10), (10, base_min_y, 10), (-10, base_min_y, 10)], [], [(0, 1, 2, 3)])
floor = bpy.data.objects.new("PreviewGround", floor_mesh)
scene.collection.objects.link(floor)
floor_mat = bpy.data.materials.new("PreviewGroundMaterial")
floor_mat.diffuse_color = (0.18, 0.20, 0.24, 1.0)
floor.data.materials.append(floor_mat)

cam_data = bpy.data.cameras.new("PreviewCamera")
camera = bpy.data.objects.new("PreviewCamera", cam_data)
scene.collection.objects.link(camera)
view_distance = max(base_width, base_height) * 4.5
view_offsets = {
    "front": Vector((0.0, 0.0, view_distance)),
    "back": Vector((0.0, 0.0, -view_distance)),
    "left": Vector((view_distance, 0.0, 0.0)),
    "right": Vector((-view_distance, 0.0, 0.0)),
}
camera.location = Vector((0.0, base_center_y, base_center_z)) + view_offsets.get(VIEW_SIDE, view_offsets["front"])
camera.rotation_euler = (Vector((0.0, base_center_y, base_center_z)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.type = "ORTHO"
camera.data.ortho_scale = max(base_width, base_height) * 1.3
scene.camera = camera

for index, frame in enumerate(sample_frames):
    for snapshot in temporary_objects:
        snapshot.hide_render = True
    for snapshot in temporary_objects[index * len(mesh_objs):(index + 1) * len(mesh_objs)]:
        snapshot.hide_render = False
    scene.render.filepath = OUTPUT.replace(".png", f"_frame{index + 1}.png")
    bpy.ops.render.render(write_still=True)
    render_paths.append(scene.render.filepath)

images = [bpy.data.images.load(path, check_existing=False) for path in render_paths]
sheet = bpy.data.images.new("SplatActionContactSheet", width=360 * len(images), height=360, alpha=True)
pixels = array("f", [0.0]) * (360 * 360 * 4 * len(images))
for index, image in enumerate(images):
    block = array("f", [0.0]) * (360 * 360 * 4)
    image.pixels.foreach_get(block)
    for y in range(360):
        src = y * 360 * 4
        dst = (y * 360 * len(images) + index * 360) * 4
        pixels[dst:dst + 360 * 4] = block[src:src + 360 * 4]
sheet.pixels.foreach_set(pixels)
sheet.filepath_raw = OUTPUT
sheet.file_format = "PNG"
sheet.save()
print("SPLAT_ACTION_PREVIEW", {"action": action.name, "frame_range": list(action.frame_range), "sample_frames": sample_frames, "view": VIEW_SIDE, "output": OUTPUT})
