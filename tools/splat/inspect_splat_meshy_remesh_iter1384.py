"""Read-only structural audit of the user-approved 300K Splat remesh."""
import json
import os
import sys

import bpy
from mathutils import Vector


ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
DEFAULT_FOLDER = ROOT + r"\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1384"
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
FBX = ARGS[0] if ARGS else DEFAULT_FOLDER + r"\Splat_300K_Meshy_Remesh.fbx"
REPORT = ARGS[1] if len(ARGS) > 1 else DEFAULT_FOLDER + r"\Splat_300K_Meshy_Remesh_Audit.json"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=True)
bpy.context.view_layer.update()

meshes = []
for obj in (obj for obj in bpy.context.scene.objects if obj.type == "MESH"):
    mesh = obj.data
    group_stats = []
    if obj.vertex_groups:
        for group in obj.vertex_groups:
            count = 0
            weight_sum = 0.0
            for vertex in mesh.vertices:
                for assignment in vertex.groups:
                    if assignment.group == group.index:
                        count += 1
                        weight_sum += assignment.weight
                        break
            if count:
                group_stats.append({
                    "name": group.name,
                    "weighted_vertices": count,
                    "mean_weight": weight_sum / count,
                })

    mesh.calc_loop_triangles()
    meshes.append({
        "object": obj.name,
        "vertices": len(mesh.vertices),
        "edges": len(mesh.edges),
        "polygons": len(mesh.polygons),
        "triangles": len(mesh.loop_triangles),
        "uv_layers": [layer.name for layer in mesh.uv_layers],
        "materials": [mat.name if mat else None for mat in mesh.materials],
        "armature_modifiers": [
            modifier.object.name if modifier.object else None
            for modifier in obj.modifiers if modifier.type == "ARMATURE"
        ],
        "vertex_groups": group_stats,
        "world_bounds": {
            "min": [min((obj.matrix_world @ Vector(co))[i] for co in obj.bound_box) for i in range(3)],
            "max": [max((obj.matrix_world @ Vector(co))[i] for co in obj.bound_box) for i in range(3)],
        },
    })

armatures = []
for obj in (obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"):
    armatures.append({
        "object": obj.name,
        "bones": [bone.name for bone in obj.data.bones],
        "action": obj.animation_data.action.name if obj.animation_data and obj.animation_data.action else None,
    })

actions = []
for action in bpy.data.actions:
    actions.append({
        "name": action.name,
        "frame_range": list(action.frame_range),
        "layers": len(action.layers),
    })

report = {
    "source": FBX,
    "mesh_objects": meshes,
    "armatures": armatures,
    "actions": actions,
    "notes": [
        "Structural import audit only; no animation quality, gameplay integration, or floor contact is implied.",
        "Triangle count is measured from imported FBX geometry and may differ from Meshy's requested target polycount.",
    ],
}
os.makedirs(os.path.dirname(REPORT), exist_ok=True)
with open(REPORT, "w", encoding="utf-8") as out:
    json.dump(report, out, indent=2)
print("SPLAT_REMESH_AUDIT", json.dumps({
    "meshes": [{k: item[k] for k in ("object", "vertices", "triangles", "uv_layers", "armature_modifiers")} for item in meshes],
    "armatures": [{"object": arm["object"], "bone_count": len(arm["bones"])} for arm in armatures],
    "actions": actions,
    "report": REPORT,
}))
