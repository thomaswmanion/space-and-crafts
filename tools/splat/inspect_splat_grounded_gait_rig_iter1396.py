"""Read-only audit of the isolated Iter1395 Meshy Splat gait rig."""
import bpy
import json
import os

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
SOURCE = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1395\Splat_FootSupportGait_Iter1395.blend"
OUTPUT = ROOT + r"\Logs\splat-gait-rig-audit-iter1396.json"
GAIT_REPORT = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1395\Splat_Iter1395_FootSupportGaitReport.json"
bpy.ops.wm.open_mainfile(filepath=SOURCE)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
mesh = next(obj for obj in scene.objects if obj.type == "MESH" and "Splat" in obj.name)
action = rig.animation_data.action if rig.animation_data else None
group_names = {group.index: group.name for group in mesh.vertex_groups}
bones = []
for bone in rig.data.bones:
    bones.append({
        "name": bone.name,
        "parent": bone.parent.name if bone.parent else None,
        "head": [round(value, 5) for value in bone.head_local],
        "tail": [round(value, 5) for value in bone.tail_local],
        "length": round(bone.length, 5),
        "deform": bone.use_deform,
    })
curves = []
if action:
    for layer in action.layers:
        for strip in layer.strips:
            for slot in action.slots:
                bag = strip.channelbag(slot)
                if not bag:
                    continue
                for curve in bag.fcurves:
                    curves.append({
                        "path": curve.data_path,
                        "index": curve.array_index,
                        "keys": len(curve.keyframe_points),
                    })
with open(GAIT_REPORT, "r", encoding="utf-8") as handle:
    gait_report = json.load(handle)
contact_vertices = {}
for sample in gait_report["frame_data"]:
    for side in ("Left", "Right"):
        for phase in ("before", "after"):
            vertex_index = sample[phase][side].get("vertex")
            if vertex_index is None:
                continue
            contact_vertices[int(vertex_index)] = side
contact_weights = []
for vertex_index, side in sorted(contact_vertices.items()):
    vertex = mesh.data.vertices[vertex_index]
    point = mesh.matrix_world @ vertex.co
    weights = sorted(
        ({"bone": group_names.get(item.group, str(item.group)), "weight": round(item.weight, 6)}
         for item in vertex.groups),
        key=lambda item: item["weight"], reverse=True
    )
    contact_weights.append({
        "vertex": vertex_index,
        "reported_side": side,
        "world_position": [round(value, 6) for value in point],
        "weights": weights,
    })
report = {
    "iteration": 1396,
    "source": os.path.relpath(SOURCE, ROOT),
    "rig": rig.name,
    "mesh": mesh.name,
    "action": action.name if action else None,
    "bones": bones,
    "action_curves": curves,
    "mesh_vertex_groups": [group.name for group in mesh.vertex_groups],
    "sampled_weighted_sole_vertices": contact_weights,
    "limitations": "Read-only skeleton, action, and contact-weight inventory. No weights, animation, game assets, or active scene were changed.",
}
with open(OUTPUT, "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
print("ITER1396_RIG_AUDIT", json.dumps(report, sort_keys=True))
