import bpy
import math
import os
import shutil
from mathutils import Vector


ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
SOURCE_BLEND = os.path.join(ROOT, "docs", "art", "splat-candidate-iter378", "Splat_Character_Iter378.blend")
OUT_DIR = os.path.join(ROOT, "docs", "art", "splat-foot-rig-iter1137")
ASSET_DIR = os.path.join(ROOT, "Assets", "SpaceAndCrafts", "Resources", "Characters")
OUT_BLEND = os.path.join(OUT_DIR, "Splat_Character_Iter1137.blend")
OUT_FBX = os.path.join(ASSET_DIR, "Splat_Rigged_Candidate_Iter1137.fbx")

os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SOURCE_BLEND)
rig = bpy.data.objects["Splat_SkinRig"]
body = bpy.data.objects["ART_SplatBody_Iter378"]

# Add short ankle pivots that continue from the existing fused leg bones. The
# bones live inside the existing one-piece skin; they add articulation without
# adding separate boot geometry or changing the character silhouette.
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
edit_bones = rig.data.edit_bones
for side_name, side in (("L", -1.0), ("R", 1.0)):
    leg = edit_bones["Bone_Leg" + side_name]
    foot_name = "Bone_Foot" + side_name
    if foot_name in edit_bones:
        continue
    foot = edit_bones.new(foot_name)
    foot.head = leg.tail.copy()
    foot.tail = Vector((side * 0.33, -0.245, 0.10))
    foot.parent = leg
    foot.use_connect = True
    foot.use_deform = True
bpy.ops.object.mode_set(mode="OBJECT")

# Transfer only the existing leg influence around the fused shoe volume. Keep
# the remaining leg/body influence for a soft ankle seam. This leaves the mesh
# one cohesive surface and retains the original vertex paint.
def smooth01(value):
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)

for side_name, side in (("L", -1.0), ("R", 1.0)):
    leg_group = body.vertex_groups["Bone_Leg" + side_name]
    foot_group = body.vertex_groups.new(name="Bone_Foot" + side_name)
    for vertex in body.data.vertices:
        x, y, z = vertex.co
        if x * side <= 0.0:
            continue
        # Ellipsoidal, soft coverage follows the existing integrated foot. The
        # weights taper at the ankle rather than creating a hard mesh seam.
        dx = (x - side * 0.33) / 0.29
        dy = (y + 0.035) / 0.37
        dz = (z - 0.105) / 0.165
        radius = math.sqrt(dx * dx + dy * dy + dz * dz)
        coverage = smooth01((1.28 - radius) / 0.48)
        coverage *= smooth01((0.285 - z) / 0.105)
        if coverage <= 0.001:
            continue
        try:
            leg_weight = leg_group.weight(vertex.index)
        except RuntimeError:
            continue
        if coverage <= 0.001:
            continue
        residual_leg = leg_weight * (1.0 - coverage * 0.96)
        foot_weight = max(leg_weight * coverage * 0.96, 0.90 * coverage)
        other_groups = []
        other_total = 0.0
        for group in body.vertex_groups:
            if group.index == leg_group.index or group.index == foot_group.index:
                continue
            try:
                value = group.weight(vertex.index)
            except RuntimeError:
                value = 0.0
            if value > 0.001:
                other_groups.append((group, value))
                other_total += value

        remaining = max(0.0, 1.0 - residual_leg - foot_weight)
        leg_group.add([vertex.index], residual_leg, "REPLACE")
        foot_group.add([vertex.index], foot_weight, "REPLACE")
        if other_total > 0.001:
            other_scale = remaining / other_total
            for group, value in other_groups:
                group.add([vertex.index], value * other_scale, "REPLACE")

    total = 0.0
    count = 0
    for vertex in body.data.vertices:
        try:
            value = foot_group.weight(vertex.index)
        except RuntimeError:
            continue
        if value >= 0.20:
            count += 1
            total += value
    print(f"[Iter1137] {foot_group.name}: vertices>=0.20={count}, totalWeight={total:.3f}")

print("[Iter1137] bones", [bone.name for bone in rig.data.bones if "Foot" in bone.name or "Leg" in bone.name])
shutil.copyfile(SOURCE_BLEND, OUT_BLEND + ".source-backup")
bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)

keep = {"Splat_SkinRig", "ART_SplatBody_Iter378", "ART_AntennaStem", "ART_AntennaLeaf"}
for obj in list(bpy.context.scene.objects):
    if obj.name not in keep:
        bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
for name in keep:
    obj = bpy.data.objects.get(name)
    if obj is not None:
        obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=OUT_FBX,
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    use_mesh_modifiers=False,
    add_leaf_bones=False,
    bake_anim=False,
    apply_unit_scale=True,
    axis_forward="-Z",
    axis_up="Y",
    path_mode="AUTO",
    use_custom_props=False,
)
print(f"[Iter1137] exported {OUT_FBX}")
