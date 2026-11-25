import bpy
import math
import sys
from mathutils import Vector


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    if len(args) != 2:
        raise SystemExit("usage: blender --background --python render_meshy_preview.py -- model.glb output.png")
    source, output = args

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=source)

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError("GLB has no mesh objects")
    points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    verts = sum(len(obj.data.vertices) for obj in meshes)
    faces = sum(len(obj.data.polygons) for obj in meshes)
    mins = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maxs = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    center = (mins + maxs) * 0.5
    size = max(maxs - mins)

    camera_data = bpy.data.cameras.new("ReviewCamera")
    camera = bpy.data.objects.new("ReviewCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    # These Meshy character exports use a thin front/back axis. A mostly frontal
    # view keeps the face and feet upright; the previous high oblique angle made
    # flat characters look upside-down and hid their silhouette.
    camera.location = center + Vector((size * 0.18, -size * 3.0, size * 0.08))
    direction = center - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Z").to_euler()
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = size * 1.42
    bpy.context.scene.camera = camera

    def area(name, location, energy, shape, size_value, color):
        data = bpy.data.lights.new(name, "AREA")
        data.energy = energy
        data.shape = shape
        data.size = size_value
        data.color = color
        obj = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(obj)
        obj.location = center + Vector(location) * size
        obj.rotation_euler = (center - obj.location).to_track_quat("-Z", "Y").to_euler()

    # Meshy models are often only a few tenths of a Blender unit tall, so
    # double-digit wattage at close range clips their colors to white.
    area("Key", (1.4, -1.8, 1.8), 1.2, "DISK", size * 1.4, (1.0, 0.84, 0.70))
    area("Fill", (-1.8, -1.0, 0.5), 0.75, "DISK", size * 1.8, (0.56, 0.72, 1.0))
    area("Rim", (0.4, 1.5, 1.4), 1.5, "DISK", size * 1.2, (0.65, 0.52, 1.0))

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.color = (0.035, 0.045, 0.085)
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.render.filepath = output
    scene.camera.data.lens = 50
    bpy.ops.render.render(write_still=True)
    print(f"PREVIEW={output}; MESH_OBJECTS={len(meshes)}; VERTICES={verts}; FACES={faces}; BOUNDS={tuple(round(v,4) for v in maxs-mins)}")


main()
