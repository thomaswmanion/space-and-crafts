"""Create isolated Hips-core skin candidate with a topology-smoothed color boundary."""
import bpy, json, os, numpy as np
from mathutils import Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
IN_BLEND = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1372\Splat_Meshy_Iter1365_RigBound_WalkGrounded_Iter1372.blend"
OUT_DIR = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1388"
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=IN_BLEND)
scene = bpy.context.scene
arm = next(o for o in scene.objects if o.type == "ARMATURE")
mesh_obj = next(o for o in scene.objects if o.type == "MESH" and "RigBound" in o.name)
mesh = mesh_obj.data
hips_group = mesh_obj.vertex_groups.get("Hips")
if hips_group is None:
    raise RuntimeError("Expected Hips group; refusing candidate weight edit")
images = [node.image for material in mesh.materials if material and material.use_nodes
          for node in material.node_tree.nodes if node.type == "TEX_IMAGE" and node.image]
if not images:
    raise RuntimeError("Expected embedded base-color image; refusing candidate weight edit")
image = images[0]
if image.size[0] > 1024 or image.size[1] > 1024:
    image.scale(min(image.size[0], 1024), min(image.size[1], 1024))
w, h = image.size
pixels = np.empty(w * h * 4, dtype=np.float32)
image.pixels.foreach_get(pixels)
pixels = pixels.reshape(h, w, 4)

uv_layer = mesh.uv_layers.active
if uv_layer is None:
    raise RuntimeError("Expected UV map; refusing candidate weight edit")
uv_flat = np.empty(len(mesh.loops) * 2, dtype=np.float32)
uv_layer.data.foreach_get("uv", uv_flat)
uv = uv_flat.reshape(-1, 2)
loop_vertices = np.empty(len(mesh.loops), dtype=np.int32)
mesh.loops.foreach_get("vertex_index", loop_vertices)
xy = np.floor(uv * np.array([w - 1, h - 1], dtype=np.float32)).astype(np.int32)
xy = np.clip(xy, [0, 0], [w - 1, h - 1])
rgb = pixels[xy[:, 1], xy[:, 0], :3]
r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
labels = np.full(len(mesh.loops), 3, dtype=np.int8)
minimum = np.minimum(np.minimum(r, g), b)
maximum = np.maximum(np.maximum(r, g), b)
labels[(minimum > .62) & ((maximum - minimum) < .22)] = 0  # off-white skin
labels[(b > r * 1.35) & (b > g * .9) & (r < .45)] = 1  # violet regions
labels[maximum < .16] = 2  # face features

nverts = len(mesh.vertices)
votes = np.bincount(labels.astype(np.int64) * nverts + loop_vertices, minlength=4 * nverts).reshape(4, nverts)
dominant = votes.argmax(axis=0)
counts = votes.sum(axis=0).clip(1)
white_fraction = votes[0] / counts
dark_fraction = votes[2] / counts

# Hips is the closest existing near-root control on this rig. Weight only the
# light/dark face/body texture area, with a soft body-volume envelope. The violet
# limbs and cap retain their original controls and the old influences fade out
# continuously rather than being deleted at a paint-color edge.
coords = np.empty(nverts * 3, dtype=np.float32)
mesh.vertices.foreach_get("co", coords)
coords = coords.reshape(-1, 3)
world_matrix = np.array(mesh_obj.matrix_world, dtype=np.float64)
homogeneous = np.ones((nverts, 4), dtype=np.float64)
homogeneous[:, :3] = coords
world = (homogeneous @ world_matrix.T)[:, :3]
x, y, z = world.T
core = np.sqrt((x / .66) ** 2 + (y / .56) ** 2 + ((z - .95) / .66) ** 2)
radial = np.clip((1.36 - core) / .40, 0.0, 1.0)
spatial = (np.abs(x) < .72) & (np.abs(y) < .62) & (z > .30) & (z < 1.60)
body_t = .82 * radial * np.clip((white_fraction - .12) / .78, 0.0, 1.0) * spatial
body_t = np.maximum(body_t, .70 * radial * np.clip(dark_fraction / .45, 0.0, 1.0) * spatial)
body_t = np.clip(body_t, 0.0, .84)

# Construct four-ring transition band around off-white/violet boundaries on the
# connected shell. Only these vertices are topology-smoothed after the Hips blend.
edge_flat = np.empty(len(mesh.edges) * 2, dtype=np.int32)
mesh.edges.foreach_get("vertices", edge_flat)
edges = edge_flat.reshape(-1, 2)
is_white = dominant == 0
is_violet = dominant == 1
boundary_edges = ((is_white[edges[:, 0]] & is_violet[edges[:, 1]]) |
                  (is_violet[edges[:, 0]] & is_white[edges[:, 1]]))
boundary = np.zeros(nverts, dtype=bool)
boundary_vertices = edges[boundary_edges]
boundary[boundary_vertices.ravel()] = True
transition = boundary.copy()
ring = boundary.copy()
for _ in range(4):
    near_edges = ring[edges[:, 0]] | ring[edges[:, 1]]
    next_ring = np.zeros(nverts, dtype=bool)
    next_ring[edges[near_edges].ravel()] = True
    next_ring &= ~transition
    transition |= next_ring
    ring = next_ring

groups = {group.name: group for group in mesh_obj.vertex_groups}
group_names = [group.name for group in mesh_obj.vertex_groups]
group_index_to_name = {group.index: group.name for group in mesh_obj.vertex_groups}
group_names_by_index = [group_index_to_name[index] for index in sorted(group_index_to_name)]
name_to_dense = {name: i for i, name in enumerate(group_names_by_index)}
weight = np.zeros((nverts, len(group_names_by_index)), dtype=np.float32)
for vertex in mesh.vertices:
    row = weight[vertex.index]
    for item in vertex.groups:
        name = group_index_to_name[item.group]
        row[name_to_dense[name]] = item.weight
hips_col = name_to_dense["Hips"]
old_hips = weight[:, hips_col].copy()
weight[:, hips_col] = old_hips + body_t * (1.0 - old_hips)
for col in range(weight.shape[1]):
    if col != hips_col:
        weight[:, col] *= (1.0 - body_t)

# Jacobi/Laplacian blend only in a narrow band crossing white-to-violet seams.
# Preserve all other original weights. Normalize each smoothed row afterward.
degree = np.bincount(edges.ravel(), minlength=nverts).astype(np.float32).clip(1.0)
for _ in range(3):
    neighbor_sum = np.zeros_like(weight)
    np.add.at(neighbor_sum, edges[:, 0], weight[edges[:, 1]])
    np.add.at(neighbor_sum, edges[:, 1], weight[edges[:, 0]])
    average = neighbor_sum / degree[:, None]
    weight[transition] = .50 * weight[transition] + .50 * average[transition]
row_sum = weight.sum(axis=1)
weight /= np.maximum(row_sum[:, None], 1e-8)

# Keep at most four influences per vertex and quantize to 8-bit normalized bins,
# batching additions by bone/value so the large candidate remains practical.
assignment = {name: {} for name in group_names_by_index}
influence_counts = {name: 0 for name in group_names_by_index}
for vertex_index in range(nverts):
    row = weight[vertex_index]
    selected = np.flatnonzero(row > 1e-4)
    if len(selected) > 4:
        selected = selected[np.argsort(row[selected])[-4:]]
    raw = {int(col): float(row[col]) for col in selected}
    total = sum(raw.values()) or 1.0
    raw = {col: value / total * 255.0 for col, value in raw.items()}
    units = {col: int(np.floor(value)) for col, value in raw.items()}
    remain = 255 - sum(units.values())
    order = sorted(raw, key=lambda col: raw[col] - units[col], reverse=True)
    for col in order[:max(0, remain)]:
        units[col] += 1
    for col, amount in units.items():
        if amount:
            name = group_names_by_index[col]
            assignment[name].setdefault(amount, []).append(vertex_index)
            influence_counts[name] += 1
for name, group in groups.items():
    group.remove(list(range(nverts)))
    for amount, indices in assignment[name].items():
        group.add(indices, amount / 255.0, "REPLACE")

mesh_obj.name = "ART_SplatBody_Iter1388_ColorBoundarySkinCandidate"
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.view_settings.view_transform = "AgX"
world_data = bpy.data.worlds.new("SplatIter1388World")
world_data.use_nodes = True
world_data.node_tree.nodes["Background"].inputs[0].default_value = (.63, .69, .76, 1)
world_data.node_tree.nodes["Background"].inputs[1].default_value = .8
scene.world = world_data
lo = Vector((min((mesh_obj.matrix_world @ Vector(c)).z for c in mesh_obj.bound_box),) * 3)
points = [mesh_obj.matrix_world @ Vector(c) for c in mesh_obj.bound_box]
center = sum(points, Vector()) / len(points)
lo_z = min(p.z for p in points)
span_x = max(p.x for p in points) - min(p.x for p in points)
span_y = max(p.y for p in points) - min(p.y for p in points)
floor_mesh = bpy.data.meshes.new("SupportPlane1388")
floor_mesh.from_pydata([(-4, -4, lo_z), (4, -4, lo_z), (4, 4, lo_z), (-4, 4, lo_z)], [], [(0, 1, 2, 3)])
floor = bpy.data.objects.new("SupportPlane1388", floor_mesh)
scene.collection.objects.link(floor)
floor_mat = bpy.data.materials.new("SupportPlane1388_Mat")
floor_mat.diffuse_color = (.23, .29, .36, 1)
floor.data.materials.append(floor_mat)
key_data = bpy.data.lights.new("Key1388", "AREA")
key_data.energy = 900; key_data.shape = "DISK"; key_data.size = 4
key = bpy.data.objects.new("Key1388", key_data); scene.collection.objects.link(key)
key.location = center + Vector((3, -4, 5)); key.rotation_euler = (center - key.location).to_track_quat("-Z", "Y").to_euler()
fill_data = bpy.data.lights.new("Fill1388", "AREA")
fill_data.energy = 450; fill_data.size = 5
fill = bpy.data.objects.new("Fill1388", fill_data); scene.collection.objects.link(fill)
fill.location = center + Vector((-4, -2, 1.5)); fill.rotation_euler = (center - fill.location).to_track_quat("-Z", "Y").to_euler()
camera_data = bpy.data.cameras.new("ReviewCamera1388")
camera = bpy.data.objects.new("ReviewCamera1388", camera_data); scene.collection.objects.link(camera)
scene.camera = camera; camera.data.type = "ORTHO"; camera.data.ortho_scale = max(span_x, span_y) * 1.8
camera.location = center + Vector((0, -5, .55)); camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Z").to_euler()

frames = [1, 9, 17, 25]
for frame in frames:
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(OUT_DIR, f"Splat_Iter1388_walk_{frame:02d}.png")
    bpy.ops.render.render(write_still=True)

report = {
    "iteration": 1388,
    "source": "Iter1372 grounded walk candidate",
    "candidate_vertices": nverts,
    "body_vertices_with_hips_bias_over_0_05": int((body_t > .05).sum()),
    "hips_bias_mean_selected": float(body_t[body_t > .05].mean()),
    "white_violet_boundary_vertex_count": int(boundary.sum()),
    "four_ring_smoothed_vertex_count": int(transition.sum()),
    "weight_influence_counts_after_top4_quantization": influence_counts,
    "render_frames": frames,
    "limitations": "Offline Blender skin-weight candidate and preview only; no Unity import, gameplay, varied-platform test, or human play. Candidate is isolated and not approved for integration.",
}
with open(os.path.join(OUT_DIR, "Splat_Iter1388_ColorBoundaryWeightReport.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
print("ITER1388_REPORT", json.dumps(report, sort_keys=True))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Splat_Meshy_Iter1388_ColorBoundarySkinCandidate.blend"))
