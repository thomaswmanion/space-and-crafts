"""Render a neutral offline preview of the isolated Meshy remesh candidate."""
import bpy
import sys
from mathutils import Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
DEFAULT_FOLDER = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1384"
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
FBX = ARGS[0] if ARGS else DEFAULT_FOLDER + r"\Splat_300K_Meshy_Remesh.fbx"
OUT = ARGS[1] if len(ARGS) > 1 else DEFAULT_FOLDER + r"\Splat_300K_Meshy_Remesh_Front.png"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=False)
bpy.context.view_layer.update()
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if not meshes:
    raise RuntimeError("Imported FBX has no mesh objects")

world_points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
center = sum(world_points, Vector()) / len(world_points)
max_extent = max(max(p[i] for p in world_points) - min(p[i] for p in world_points) for i in range(3))

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = OUT
scene.render.film_transparent = False
scene.render.image_settings.color_mode = "RGBA"
scene.view_settings.view_transform = "AgX"

scene.world = bpy.data.worlds.new("PreviewWorld")
scene.world.color = (0.07, 0.08, 0.11)

def area_light(name, loc, energy, size):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    obj.rotation_euler = (center - obj.location).to_track_quat("-Z", "Y").to_euler()

area_light("Key", center + Vector((-2.6, 3.2, 3.8)), 550, 3.2)
area_light("Fill", center + Vector((2.8, 1.8, 2.2)), 300, 3.8)
area_light("Rim", center + Vector((0.0, 3.0, -2.6)), 420, 2.8)

cam_data = bpy.data.cameras.new("PreviewCamera")
camera = bpy.data.objects.new("PreviewCamera", cam_data)
scene.collection.objects.link(camera)
camera.location = center + Vector((0.0, max_extent * 0.04, max_extent * 3.0))
camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.type = "ORTHO"
camera.data.ortho_scale = max_extent * 1.23
scene.camera = camera

bpy.ops.render.render(write_still=True)
print("SPLAT_REMESH_PREVIEW", OUT)
