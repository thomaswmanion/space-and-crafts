from mathutils import Vector
from pathlib import Path
import bpy
import math

ROOT = Path(__file__).resolve().parents[2]
BASELINE = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1357/Splat_Meshy_Iter1337_BodyRooted.fbx"
REPAIRED = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1363/Splat_Meshy_Iter1337_FootSeated.fbx"
OUT_DIR = ROOT / "Logs/splat-foot-seat-review-iter1363"
OUT_DIR.mkdir(parents=True, exist_ok=True)
OUTPUT = OUT_DIR / "mild-leg-flex-side-by-side.png"

bpy.ops.wm.read_factory_settings(use_empty=True)
characters = []
for label, filepath, offset in (("Before", BASELINE, -1.05), ("FootSeated", REPAIRED, 1.05)):
    bpy.ops.import_scene.fbx(filepath=str(filepath))
    armature = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"][-1]
    mesh = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.vertex_groups][-1]
    armature.location.x += offset
    # A restrained asymmetrical gait pose exposes whether the shoe shell
    # follows the ankle pivot or incorrectly rotates around the knee.
    for name, degrees in (("LeftUpLeg", -9.0), ("LeftLeg", 16.0),
                          ("RightUpLeg", 9.0), ("RightLeg", -5.0)):
        bone = armature.pose.bones.get(name)
        if bone is not None:
            bone.rotation_mode = "XYZ"
            bone.rotation_euler.x = math.radians(degrees)
    characters.append((label, armature, mesh))

# Simple matte contact plane, shadow key, and fill make the actual sole gap
# legible; this is an isolated Blender comparison, not an in-game render.
bpy.ops.mesh.primitive_plane_add(size=20.0, location=(0.0, 0.0, -0.006))
floor = bpy.context.object
floor.name = "PreviewSupportPlane"
floor_mat = bpy.data.materials.new("Preview matte blue gray")
floor_mat.diffuse_color = (0.25, 0.32, 0.38, 1.0)
floor_mat.use_nodes = True
floor_mat.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.25, 0.32, 0.38, 1.0)
floor_mat.node_tree.nodes.get("Principled BSDF").inputs["Roughness"].default_value = 0.9
floor.data.materials.append(floor_mat)

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()

def area(name, position, power, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = power
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = position
    aim(obj, (0.0, 0.0, 0.8))

area("Key", (-3.0, -4.0, 5.5), 620.0, 4.0, (1.0, 0.88, 0.76))
area("Fill", (3.0, -2.5, 3.0), 360.0, 3.5, (0.70, 0.82, 1.0))
area("Rim", (0.0, 2.8, 4.5), 480.0, 3.0, (0.82, 0.9, 1.0))

camera_data = bpy.data.cameras.new("Splat comparison camera")
camera = bpy.data.objects.new("Splat comparison camera", camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (0.0, -8.0, 1.45)
aim(camera, (0.0, -0.05, 0.88))
camera_data.type = "ORTHO"
camera_data.ortho_scale = 4.55
bpy.context.scene.camera = camera

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(OUTPUT)
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new("Splat comparison world")
scene.world.use_nodes = True
scene.world.node_tree.nodes.get("Background").inputs["Color"].default_value = (0.52, 0.58, 0.66, 1.0)
scene.render.image_settings.color_mode = "RGBA"
scene.view_settings.view_transform = "AgX"
scene.camera.data.lens = 50
scene.render.filepath = str(OUTPUT)
scene.render.engine = "BLENDER_EEVEE"
scene.eevee.taa_render_samples = 96

depsgraph = bpy.context.evaluated_depsgraph_get()
for label, armature, mesh in characters:
    evaluated = mesh.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh()
    points = [evaluated.matrix_world @ vertex.co for vertex in evaluated_mesh.vertices]
    print("POSE_BOUNDS", label,
          "min_z", round(min(point.z for point in points), 4),
          "mesh_vertices", len(points))
    group_indices = {group.name: group.index for group in mesh.vertex_groups}
    for side in ("LeftFoot", "RightFoot"):
        group_index = group_indices.get(side)
        weighted = [points[vertex.index].z for vertex in mesh.data.vertices
                    if any(membership.group == group_index and membership.weight >= 0.5
                           for membership in vertex.groups)] if group_index is not None else []
        print("POSE_FOOT_WEIGHTED_MIN", label, side,
              round(min(weighted), 4) if weighted else "no vertices",
              "vertices", len(weighted))
    evaluated.to_mesh_clear()

bpy.ops.render.render(write_still=True)
print("CONTACT_PREVIEW", OUTPUT)
