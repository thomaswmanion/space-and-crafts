from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/AnimationCandidates/Iter1347/Splat_Meshy_Iter1337_FaceSeated.fbx"
OUT = ROOT / "Logs/splat-face-seated-fbx-iter1348"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(MODEL), use_anim=False)
scene = bpy.context.scene
meshes = [obj for obj in scene.objects if obj.type == "MESH"]
rigs = [obj for obj in scene.objects if obj.type == "ARMATURE"]
for rig in rigs:
    rig.data.pose_position = "REST"
    rig.animation_data_clear()
print("FBX_INSPECT", [(o.name, len(o.data.vertices), len(o.data.polygons), tuple(round(v, 4) for v in o.dimensions), tuple(round(v, 4) for v in o.location), tuple(round(v, 4) for v in o.scale)) for o in meshes])
if not meshes:
    raise RuntimeError("Exported FBX contains no meshes")
points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
center = (lo + hi) * 0.5
span = hi - lo
print("FBX_BOUNDS", tuple(round(x, 4) for x in lo), tuple(round(x, 4) for x in hi), "span", tuple(round(x, 4) for x in span))

world = bpy.data.worlds.new("FaceSeatedWorld")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.63, 0.69, 0.76, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.8
scene.world = world
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 960
scene.render.resolution_percentage = 100
scene.view_settings.view_transform = "AgX"
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False

floor_mesh = bpy.data.meshes.new("SupportPlane")
floor_mesh.from_pydata([(-6, -6, lo.z), (6, -6, lo.z), (6, 6, lo.z), (-6, 6, lo.z)], [], [(0, 1, 2, 3)])
floor = bpy.data.objects.new("SupportPlane", floor_mesh)
scene.collection.objects.link(floor)
mat = bpy.data.materials.new("SupportPlaneMat")
mat.diffuse_color = (0.23, 0.29, 0.36, 1)
floor.data.materials.append(mat)

for name, offset in (("Key", (3, -4, 5)), ("Fill", (-4, -2, 1.5))):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = 900 if name == "Key" else 450
    data.size = 4
    light = bpy.data.objects.new(name, data)
    scene.collection.objects.link(light)
    light.location = center + Vector(offset)
    light.rotation_euler = (center - light.location).to_track_quat("-Z", "Z").to_euler()
camera_data = bpy.data.cameras.new("ReviewCamera")
camera = bpy.data.objects.new("ReviewCamera", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.data.type = "ORTHO"
camera.data.ortho_scale = max(span.x, span.z) * 1.8
for name, offset in (("front", (0, -5, 0.55)), ("three_quarter", (-3.6, -4.4, 0.4))):
    camera.location = center + Vector(offset)
    camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Z").to_euler()
    scene.render.filepath = str(OUT / f"{name}.png")
    bpy.ops.render.render(write_still=True)
