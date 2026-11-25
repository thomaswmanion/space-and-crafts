"""Offline flat-plane comparison for the candidate-only Iter1398 sole weights."""
import bpy
import json
import os
import sys
import numpy as np

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
BASE = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1395"
SOURCE = BASE + r"\Splat_FootSupportGait_Iter1395.blend"
arguments = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
CANDIDATE = arguments[0] if len(arguments) > 0 else BASE + r"\Iter1398_SolePatchWeights\Splat_Meshy_Iter1395_SolePatchWeights_Iter1398.blend"
OUTPUT = arguments[1] if len(arguments) > 1 else BASE + r"\Iter1398_SolePatchContactCompare.json"
REPORT = BASE + r"\Splat_Iter1395_FootSupportGaitReport.json"
with open(REPORT, "r", encoding="utf-8") as handle:
    gait = json.load(handle)
stance_sides = {entry["frame"]: entry["stance"] for entry in gait["frame_data"]}

def evaluate(path):
    bpy.ops.wm.open_mainfile(filepath=path)
    scene = bpy.context.scene
    obj = next(item for item in scene.objects if item.type == "MESH" and "RigBound" in item.name)
    depsgraph = bpy.context.evaluated_depsgraph_get()
    groups = {group.name: group.index for group in obj.vertex_groups}
    indices = {}
    for side in ("Left", "Right"):
        wanted = (groups[side + "Foot"], groups[side + "ToeBase"])
        indices[side] = np.asarray([
            vertex.index for vertex in obj.data.vertices
            if any(item.group in wanted and item.weight >= 0.25 for item in vertex.groups)
        ], dtype=np.int32)
    frames = []
    for frame in range(1, 34):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)
        try:
            flat = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
            mesh.vertices.foreach_get("co", flat)
            coords = flat.reshape(-1, 3)
            matrix = np.asarray(evaluated.matrix_world, dtype=np.float64)
            world = coords @ matrix[:3, :3].T + matrix[:3, 3]
        finally:
            evaluated.to_mesh_clear()
        sides = {}
        for side, points in indices.items():
            vertex = int(points[np.argmin(world[points, 2])])
            sides[side] = {"vertex": vertex, "point": [float(v) for v in world[vertex]]}
        side = stance_sides[frame]
        foot = sides[side]["point"]
        swing = sides["Right" if side == "Left" else "Left"]["point"]
        frames.append({"frame": frame, "stance": side, "stance_sole": foot, "swing_sole": swing})
    stance = [abs(item["stance_sole"][2]) for item in frames]
    swing_penetration = [max(0.0, -item["swing_sole"][2]) for item in frames]
    return {
        "path": path,
        "candidate_foot_vertex_counts": {side: int(len(values)) for side, values in indices.items()},
        "max_abs_stance_vertical_error_m": max(stance),
        "max_swing_penetration_m": max(swing_penetration),
        "frames": frames,
    }

results = {"baseline": evaluate(SOURCE), "sole_patch": evaluate(CANDIDATE)}
render_dir = os.path.join(os.path.dirname(OUTPUT), "Previews")
os.makedirs(render_dir, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=CANDIDATE)
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
for frame in (1, 9, 17, 21, 25, 29, 33):
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(render_dir, f"Splat_Iter1398_gait_{frame:02d}.png")
    bpy.ops.render.render(write_still=True)
results["rendered_candidate_frames"] = [1, 9, 17, 21, 25, 29, 33]
results["limitations"] = "Blender offline evaluated-mesh sampling on one flat Z=0 plane. No Unity import/runtime, varied supports, landing transition, or human-play verification."
with open(OUTPUT, "w", encoding="utf-8") as handle:
    json.dump(results, handle, indent=2)
print("ITER1398_CONTACT_COMPARE", json.dumps({
    name: {key: value for key, value in report.items() if key != "frames"}
    for name, report in results.items() if isinstance(report, dict)
}, sort_keys=True))
