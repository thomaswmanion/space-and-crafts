"""Offline weighted-sole height audit for the isolated Iter1389 Blender gait."""
import bpy, json, os, numpy as np

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BLEND = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1389\Splat_RotationOnly_Gait_Iter1389.blend"
OUT = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1389\Splat_Iter1389_SoleContactAudit.json"
bpy.ops.wm.open_mainfile(filepath=BLEND)
scene = bpy.context.scene
mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH" and "Iter1370_RigBound" in obj.name or obj.type == "MESH" and "RigBound" in obj.name)
mesh = mesh_obj.data
groups = {group.name: group.index for group in mesh_obj.vertex_groups}
foot_regions = {}
for name in ("LeftFoot", "LeftToeBase", "RightFoot", "RightToeBase"):
    if name not in groups:
        raise RuntimeError(f"Missing expected foot group {name}")
    foot_regions[name] = np.asarray([
        vertex.index for vertex in mesh.vertices
        if any(item.group == groups[name] and item.weight >= .25 for item in vertex.groups)
    ], dtype=np.int32)
if not len(foot_regions["LeftFoot"]) or not len(foot_regions["RightFoot"]):
    raise RuntimeError("No high-weight foot regions found; refusing to report contact")
rest_z = np.asarray([(mesh_obj.matrix_world @ vertex.co).z for vertex in mesh.vertices], dtype=np.float64)
support_z = float(rest_z.min())
results = []
for frame in (1, 5, 9, 13, 17, 21, 25, 29, 33):
    scene.frame_set(frame)
    depsgraph = bpy.context.evaluated_depsgraph_get()
    depsgraph.update()
    evaluated = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)
    try:
        coords = np.empty(len(evaluated_mesh.vertices) * 3, dtype=np.float32)
        evaluated_mesh.vertices.foreach_get("co", coords)
        coords = coords.reshape(-1, 3)
        matrix = np.array(evaluated.matrix_world, dtype=np.float64)
        homogeneous = np.ones((len(coords), 4), dtype=np.float64)
        homogeneous[:, :3] = coords
        world_z = (homogeneous @ matrix.T)[:, 2]
        feet = {}
        for name, indices in foot_regions.items():
            values = world_z[indices]
            feet[name] = {
                "weighted_vertices": int(len(indices)),
                "minimum_sole_z_m": float(values.min()),
                "offset_from_rest_support_m": float(values.min() - support_z),
            }
        results.append({"frame": frame, "feet": feet})
    finally:
        evaluated.to_mesh_clear()
report = {
    "iteration": 1389,
    "source": os.path.basename(BLEND),
    "reference_support_z_m": support_z,
    "weight_threshold": .25,
    "frames": results,
    "limitations": "Offline evaluated Blender skinned mesh and a plane at the rest-pose bounding-box minimum. Weighted-vertex minima are sole proxies, not Unity collision/contact data; this does not verify varied supports or gameplay.",
}
with open(OUT, "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
print("ITER1389_SOLE_AUDIT", json.dumps(report, sort_keys=True))
