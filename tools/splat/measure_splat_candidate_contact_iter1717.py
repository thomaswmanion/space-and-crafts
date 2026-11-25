"""Measure foot-weighted sole distance for authored Iter1717 Blender poses."""
import bpy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BLEND = ROOT / "docs/art/splat-unified-candidate-iter1717/Splat_UnifiedRig_Candidate_Iter1717.blend"
OUT = ROOT / "Logs/score-comparison/splat-unified-candidate-iter1717/sole-contact-measurements.txt"
bpy.ops.wm.open_mainfile(filepath=str(BLEND))
scene = bpy.context.scene
rig = bpy.data.objects["Splat_UnifiedRig"]
core = bpy.data.objects["Splat_UnifiedBodyAndLimbs"]
depsgraph = bpy.context.evaluated_depsgraph_get()

# Samples cover one planted side per run half, the jump path, and all landing beats.
samples = [
    ("idle", "Splat_Idle", 61, 0.0),
    ("run-left-stance", "Splat_Run", 1, 0.0),
    ("run-right-stance", "Splat_Run", 13, 0.0),
    ("jump-anticipation", "Splat_Jump", 1, 0.0),
    ("jump-apex", "Splat_Jump", 22, 0.24),
    ("jump-descent", "Splat_Jump", 34, 0.10),
    ("land-impact", "Splat_Land", 6, 0.0),
    ("land-recovery", "Splat_Land", 12, 0.0),
    ("land-settle", "Splat_Land", 24, 0.0),
]
groups = {name: core.vertex_groups[name].index for name in ("Foot_L", "Foot_R")}
lines = ["scope=offline Blender pose sampling; no Unity import, motor, production asset or gameplay verification"]
lines.append("model=Splat_UnifiedBodyAndLimbs; vertices=%d; ground_plane_z=0.00000m" % len(core.data.vertices))
for label, action_name, frame, preview_lift in samples:
    action = bpy.data.actions.get(action_name)
    rig.animation_data.action = action
    rig.location.z = preview_lift
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    depsgraph.update()
    evaluated = core.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh()
    minima = {}
    for side, group_index in groups.items():
        zs = []
        for vertex in core.data.vertices:
            weight = next((g.weight for g in vertex.groups if g.group == group_index), 0.0)
            if weight >= 0.25:
                world = evaluated.matrix_world @ evaluated_mesh.vertices[vertex.index].co
                zs.append(world.z)
        minima[side] = min(zs) if zs else float("nan")
    all_min = min((evaluated.matrix_world @ v.co).z for v in evaluated_mesh.vertices)
    evaluated.to_mesh_clear()
    lines.append(
        f"pose={label} clip={action_name} frame={frame} externalPreviewLift={preview_lift:.3f}m "
        f"leftSoleMin={minima['Foot_L']:.5f}m rightSoleMin={minima['Foot_R']:.5f}m "
        f"meshMin={all_min:.5f}m leftToPlane={minima['Foot_L']-preview_lift:.5f}m "
        f"rightToPlane={minima['Foot_R']-preview_lift:.5f}m"
    )

OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("SPLAT_ITER1717_CONTACT_REPORT " + str(OUT))
