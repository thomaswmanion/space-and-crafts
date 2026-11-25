"""Inspect and render the original paid spider mesh in a clear front view."""
import bpy
import math
import os
from mathutils import Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
SRC = os.path.join(ROOT, "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/"
                   "MeshySpenddown/20261031_223339_rainbow-kingdom-spider-enemy_01a1952b/"
                   "RainbowKingdomSpider.glb")
OUT = os.path.join(ROOT, "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/"
                   "MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1445/"
                   "Spider_StaticFront_Iter1445.png")

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=SRC)
mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
corners = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
minimum = Vector((min(p.x for p in corners), min(p.y for p in corners), min(p.z for p in corners)))
maximum = Vector((max(p.x for p in corners), max(p.y for p in corners), max(p.z for p in corners)))
center = (minimum + maximum) * 0.5
print("SPIDER", mesh.name, "verts", len(mesh.data.vertices), "polys", len(mesh.data.polygons),
      "location", tuple(mesh.location), "rotation", tuple(mesh.rotation_euler),
      "scale", tuple(mesh.scale), "bounds", tuple(minimum), tuple(maximum))
for material in mesh.data.materials:
    print("MATERIAL", material.name, "nodes", material.use_nodes)
    if material.use_nodes:
        for node in material.node_tree.nodes:
            if node.type == "TEX_IMAGE" and node.image:
                print("IMAGE", node.image.name, node.image.size[:])

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 24
scene.render.resolution_x = 800
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.world.color = (0.14, 0.16, 0.20)
scene.camera = bpy.data.objects.new("ReviewCamera", bpy.data.cameras.new("ReviewCamera"))
scene.collection.objects.link(scene.camera)
scene.camera.data.type = "ORTHO"
scene.camera.data.ortho_scale = max(maximum.x - minimum.x, maximum.y - minimum.y) * 1.9
scene.camera.location = (center.x, center.y + 0.06, maximum.z + 2.8)
direction = center - scene.camera.location
scene.camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
scene.camera.data.lens = 52
scene.camera.data.dof.use_dof = False
scene.camera.data.clip_start = 0.01
scene.camera.data.clip_end = 100

def area(name, location, power, size):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = power
    data.shape = "DISK"
    data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = (center - obj.location).to_track_quat("-Z", "Y").to_euler()

area("Key", (center.x - 2.0, center.y + 2.1, maximum.z + 2.0), 450, 2.4)
area("Fill", (center.x + 2.0, center.y + 0.7, maximum.z + 1.0), 250, 2.0)
area("Rim", (center.x + 0.2, maximum.y + 1.5, center.z - 1.0), 380, 1.8)
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = OUT
os.makedirs(os.path.dirname(OUT), exist_ok=True)
bpy.ops.render.render(write_still=True)
print("RENDER", OUT)
