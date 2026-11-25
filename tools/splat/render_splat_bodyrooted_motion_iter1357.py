from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1357/Splat_Meshy_Iter1337_BodyRooted.glb"
OUT = ROOT / "Logs/splat-bodyrooted-motion-iter1357"
OUT.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(MODEL))
scene = bpy.context.scene
meshes = [obj for obj in scene.objects if obj.type == "MESH"]
armatures = [obj for obj in scene.objects if obj.type == "ARMATURE"]
for rig in armatures:
    rig.data.pose_position = "REST"
    rig.animation_data_clear()

points = []
for obj in meshes:
    for corner in obj.bound_box:
        points.append(obj.matrix_world @ Vector(corner))
lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
center = (lo + hi) * 0.5
span = hi - lo
print("SPLAT_REST bounds", tuple(round(x, 4) for x in lo), tuple(round(x, 4) for x in hi), "span", tuple(round(x, 4) for x in span))
for obj in meshes:
    print("SPLAT_REST mesh", obj.name, "verts", len(obj.data.vertices), "faces", len(obj.data.polygons), "materials", [m.name if m else None for m in obj.data.materials], "armature", any(m.type == "ARMATURE" for m in obj.modifiers))

world = bpy.data.worlds.new("SplatRestWorld")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.63, 0.69, 0.76, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.8
scene.world = world
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 960
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.view_settings.view_transform = "AgX"

floor_mesh = bpy.data.meshes.new("SupportPlane")
floor_mesh.from_pydata([(-6, -6, lo.z), (6, -6, lo.z), (6, 6, lo.z), (-6, 6, lo.z)], [], [(0, 1, 2, 3)])
floor = bpy.data.objects.new("SupportPlane", floor_mesh)
scene.collection.objects.link(floor)
mat = bpy.data.materials.new("SupportPlaneMat")
mat.diffuse_color = (0.23, 0.29, 0.36, 1)
floor.data.materials.append(mat)

key_data = bpy.data.lights.new("Key", "AREA")
key_data.energy = 900
key_data.shape = "DISK"
key_data.size = 4
key = bpy.data.objects.new("Key", key_data)
scene.collection.objects.link(key)
key.location = center + Vector((3, -4, 5))
key.rotation_euler = (center - key.location).to_track_quat("-Z", "Y").to_euler()
fill_data = bpy.data.lights.new("Fill", "AREA")
fill_data.energy = 450
fill_data.size = 5
fill = bpy.data.objects.new("Fill", fill_data)
scene.collection.objects.link(fill)
fill.location = center + Vector((-4, -2, 1.5))
fill.rotation_euler = (center - fill.location).to_track_quat("-Z", "Y").to_euler()

camera_data = bpy.data.cameras.new("ReviewCamera")
camera = bpy.data.objects.new("ReviewCamera", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.data.type = "ORTHO"
camera.data.ortho_scale = max(span.x, span.z) * 1.8
camera.location = center + Vector((0, -5, 0.55))
camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Z").to_euler()
scene.render.filepath = str(OUT / "bind_pose_front.png")
bpy.ops.render.render(write_still=True)
camera.location = center + Vector((-3.6, -4.4, 0.4))
camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Z").to_euler()
scene.render.filepath = str(OUT / "bind_pose_three_quarter.png")
bpy.ops.render.render(write_still=True)

rig = armatures[0]
rig.data.pose_position = "POSE"
def set_pose(values):
    for bone in rig.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = (0.0, 0.0, 0.0)
    for name, axes in values.items():
        bone = rig.pose.bones.get(name)
        if bone is None:
            raise RuntimeError(f"Missing pose bone {name}")
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = axes
    bpy.context.view_layer.update()

poses = {
    "walk_a": {
        "LeftShoulder": (0.0, 0.0, 0.28), "RightShoulder": (0.0, 0.0, -0.28),
        "LeftUpLeg": (0.32, 0.0, 0.0), "RightUpLeg": (-0.32, 0.0, 0.0),
        "LeftLeg": (-0.12, 0.0, 0.0), "RightLeg": (0.12, 0.0, 0.0),
    },
    "walk_b": {
        "LeftShoulder": (0.0, 0.0, -0.28), "RightShoulder": (0.0, 0.0, 0.28),
        "LeftUpLeg": (-0.32, 0.0, 0.0), "RightUpLeg": (0.32, 0.0, 0.0),
        "LeftLeg": (0.12, 0.0, 0.0), "RightLeg": (-0.12, 0.0, 0.0),
    },
    "run_a": {
        "LeftShoulder": (0.0, 0.0, 0.42), "RightShoulder": (0.0, 0.0, -0.42),
        "LeftUpLeg": (0.48, 0.0, 0.0), "RightUpLeg": (-0.48, 0.0, 0.0),
        "LeftLeg": (-0.22, 0.0, 0.0), "RightLeg": (0.22, 0.0, 0.0),
    },
    "jump_apex": {
        "LeftShoulder": (0.0, 0.0, 0.10), "RightShoulder": (0.0, 0.0, -0.10),
        "LeftUpLeg": (-0.22, 0.0, 0.0), "RightUpLeg": (-0.22, 0.0, 0.0),
        "LeftLeg": (0.34, 0.0, 0.0), "RightLeg": (0.34, 0.0, 0.0),
    },
}
for name, values in poses.items():
    set_pose(values)
    camera.location = center + Vector((0, -5, 0.55))
    camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Z").to_euler()
    scene.render.filepath = str(OUT / f"{name}_front.png")
    bpy.ops.render.render(write_still=True)
    print("SPLAT_BODYROOTED_POSE", name, "bones", sorted(values))
