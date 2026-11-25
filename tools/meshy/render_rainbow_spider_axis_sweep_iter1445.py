"""Render six orthographic directions of the source spider to identify its authored axes."""
import bpy
import os
from mathutils import Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
SRC = os.path.join(ROOT, "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/"
                   "MeshySpenddown/20261031_223339_rainbow-kingdom-spider-enemy_01a1952b/"
                   "RainbowKingdomSpider.glb")
OUTDIR = os.path.join(ROOT, "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/"
                      "MeshySpenddown/20261101_rainbow-spider-rig-candidate-iter1445")
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=SRC)
mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
points = [mesh.matrix_world @ Vector(point) for point in mesh.bound_box]
center = sum(points, Vector()) / len(points)
extents = Vector((max(p.x for p in points)-min(p.x for p in points),
                  max(p.y for p in points)-min(p.y for p in points),
                  max(p.z for p in points)-min(p.z for p in points)))
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 16
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.camera = bpy.data.objects.new("AxisReviewCamera", bpy.data.cameras.new("AxisReviewCamera"))
scene.collection.objects.link(scene.camera)
scene.camera.data.type = "ORTHO"
scene.camera.data.ortho_scale = max(extents.x, extents.y, extents.z) * 1.55
scene.camera.data.lens = 52
scene.camera.data.clip_start = 0.01
scene.camera.data.clip_end = 100
scene.world.color = (0.18, 0.18, 0.18)
target = center
axis_vectors = {
    "plusX": Vector((1,0,0)), "minusX": Vector((-1,0,0)),
    "plusY": Vector((0,1,0)), "minusY": Vector((0,-1,0)),
    "plusZ": Vector((0,0,1)), "minusZ": Vector((0,0,-1)),
}
os.makedirs(OUTDIR, exist_ok=True)
for label, axis in axis_vectors.items():
    scene.camera.location = target + axis * 2.8
    scene.camera.rotation_euler = (target - scene.camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(OUTDIR, f"Axis_{label}_Iter1445.png")
    bpy.ops.render.render(write_still=True)
    print("RENDER", label, scene.render.filepath)
print("EXTENTS", tuple(extents), "CENTER", tuple(center))
