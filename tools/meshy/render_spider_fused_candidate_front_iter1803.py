import bpy
import math
import os
from mathutils import Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BLEND = os.path.join(ROOT, "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1444/RainbowSpider_RigCandidate_Iter1444.blend")
OUT_DIR = os.path.join(ROOT, "Logs/score-comparison/spider-fused-surface-review-iter1803")
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=BLEND)
scene = bpy.context.scene
candidate = next(o for o in scene.objects if o.type == "MESH" and "FusedSurface_Iter1444" in o.name)

# Keep the source and candidate file untouched. Render only the fused candidate
# using its existing materials and source orientation (front points toward -Y).
for obj in scene.objects:
    if obj != candidate and obj.type in {"MESH", "CURVE", "FONT", "SURFACE", "META"}:
        obj.hide_render = True

bpy.context.view_layer.update()
corners = [candidate.matrix_world @ Vector(corner) for corner in candidate.bound_box]
minimum = Vector(tuple(min(p[i] for p in corners) for i in range(3)))
maximum = Vector(tuple(max(p[i] for p in corners) for i in range(3)))
center = (minimum + maximum) * 0.5
extent = maximum - minimum
span = max(extent.x, extent.y, extent.z)

for obj in list(scene.objects):
    if obj.type in {"CAMERA", "LIGHT"}:
        bpy.data.objects.remove(obj, do_unlink=True)

cam_data = bpy.data.cameras.new("Spider front review camera")
cam = bpy.data.objects.new("Spider front review camera", cam_data)
scene.collection.objects.link(cam)
cam.location = center + Vector((0.0, -span * 3.2, span * 0.55))
direction = center - cam.location
cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
cam_data.type = "ORTHO"
cam_data.ortho_scale = span * 1.45
scene.camera = cam

def add_area(name, offset, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = center + Vector(offset) * span
    data.energy = energy
    data.shape = "DISK"
    data.size = size * span
    data.color = color
    obj.rotation_euler = (center - obj.location).to_track_quat("-Z", "Y").to_euler()

add_area("Key", (-2.8, -3.5, 4.0), 720, 2.2, (1.0, 0.84, 0.72))
add_area("Fill", (3.2, -2.2, 1.5), 420, 2.8, (0.72, 0.84, 1.0))
add_area("Rim", (0.8, 2.5, 3.2), 650, 2.0, (0.88, 0.78, 1.0))

scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.render.filepath = os.path.join(OUT_DIR, "front.png")
scene.world.color = (0.055, 0.075, 0.11)
scene.view_settings.view_transform = "AgX"
scene.frame_set(1)
bpy.ops.render.render(write_still=True)
print("CANDIDATE", candidate.name, "VERTICES", len(candidate.data.vertices), "FACES", len(candidate.data.polygons), "BOUNDS", tuple(round(v, 3) for v in extent))
print("SAVED", scene.render.filepath)
