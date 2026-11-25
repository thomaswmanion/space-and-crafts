"""Render the saved Splat skin with a hidden-rear cylindrical UV layout."""

import os
import bpy
import numpy as np
from mathutils import Vector


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT, "Assets", "SpaceAndCrafts", "Art", "Characters", "Splat", "Splat_Rigged.blend")
OUTPUT_DIR = os.path.join(PROJECT, "docs", "art", "splat-rigged-preview", "uv-review")


os.makedirs(OUTPUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=BLEND_PATH)
body = bpy.data.objects.get("ART_SplatBody")
if body is None or body.type != "MESH":
    raise RuntimeError("Splat fused body mesh is missing from the saved blend")

mesh = body.data
saved_uv = mesh.uv_layers.active
if saved_uv is None:
    raise RuntimeError("Saved Splat blend has no original body UV layer")
uv = mesh.uv_layers.new(name="Review_Cylindrical_RearSeam")
positions = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
mesh.vertices.foreach_get("co", positions)
positions = positions.reshape((-1, 3))
loop_vertices = np.empty(len(mesh.loops), dtype=np.int32)
mesh.loops.foreach_get("vertex_index", loop_vertices)
loop_positions = positions[loop_vertices]

min_z = float(np.min(positions[:, 2]))
height = max(float(np.max(positions[:, 2]) - min_z), 1e-6)
uvs = np.empty((len(mesh.loops), 2), dtype=np.float32)
uvs[:, 0] = np.arctan2(loop_positions[:, 0], -loop_positions[:, 1]) / (2.0 * np.pi) + 0.5
uvs[:, 1] = (loop_positions[:, 2] - min_z) / height

polygon_count = len(mesh.polygons)
loop_starts = np.empty(polygon_count, dtype=np.int32)
loop_totals = np.empty(polygon_count, dtype=np.int32)
mesh.polygons.foreach_get("loop_start", loop_starts)
mesh.polygons.foreach_get("loop_total", loop_totals)
if int(np.sum(loop_totals)) != len(mesh.loops):
    raise RuntimeError("Polygon loop counts do not cover the Splat mesh loops")
face_for_loop = np.repeat(np.arange(polygon_count, dtype=np.int32), loop_totals)
face_min = np.full(polygon_count, np.inf, dtype=np.float32)
face_max = np.full(polygon_count, -np.inf, dtype=np.float32)
np.minimum.at(face_min, face_for_loop, uvs[:, 0])
np.maximum.at(face_max, face_for_loop, uvs[:, 0])
crosses_hidden_seam = (face_max - face_min) > 0.5
shift_loop = crosses_hidden_seam[face_for_loop] & (uvs[:, 0] < 0.5)
uvs[shift_loop, 0] += 1.0
uv.data.foreach_set("uv", uvs.ravel())

scene = bpy.context.scene
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False


def render_views(folder):
    os.makedirs(folder, exist_ok=True)
    for name, location, target in (
        ("front-three-quarter", (2.5, -5.8, 1.45), (0.0, 0.0, 1.0)),
        ("back-pack", (0.0, 5.8, 1.25), (0.0, 0.0, 1.0)),
    ):
        camera_data = bpy.data.cameras.new("UVReviewCamera_" + name)
        camera = bpy.data.objects.new("UVReviewCamera_" + name, camera_data)
        scene.collection.objects.link(camera)
        camera.location = location
        camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.type = "ORTHO"
        camera.data.ortho_scale = 2.75
        scene.camera = camera
        scene.render.filepath = os.path.join(folder, name + ".png")
        bpy.ops.render.render(write_still=True)
        bpy.data.objects.remove(camera, do_unlink=True)


material = body.data.materials[0]
shader = material.node_tree.nodes.get("Principled BSDF") if material and material.node_tree is not None else None
skin_node = material.node_tree.nodes.get("Splat hand-painted skin") if material and material.node_tree is not None else None
if shader is None or skin_node is None:
    raise RuntimeError("Splat body albedo nodes are missing from the saved blend")
uv_node = material.node_tree.nodes.new("ShaderNodeUVMap")
uv_node.uv_map = saved_uv.name
material.node_tree.links.new(uv_node.outputs["UV"], skin_node.inputs["Vector"])
render_views(os.path.join(OUTPUT_DIR, "saved-unwrap"))
uv_node.uv_map = uv.name
render_views(os.path.join(OUTPUT_DIR, "cylindrical"))
print("Rendered saved and cylindrical UV layouts without modifying the saved blend or FBX")

# Compare flat body color to isolate whether the visible patching comes from the
# surface/shading or from the albedo and its UV mapping.
base_color = shader.inputs.get("Base Color")
for link in list(material.node_tree.links):
    if link.to_node == shader and link.to_socket == base_color:
        material.node_tree.links.remove(link)
base_color.default_value = material.diffuse_color

flat_dir = os.path.join(OUTPUT_DIR, "flat-color")
render_views(flat_dir)
print("Rendered flat-color comparison to isolate the body patching")
