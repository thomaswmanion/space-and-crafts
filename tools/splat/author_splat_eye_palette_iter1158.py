import bpy
import os
import shutil
from collections import Counter

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
SOURCE_BLEND = os.path.join(ROOT, "docs", "art", "splat-foot-rig-iter1137", "Splat_Character_Iter1137.blend")
OUT_DIR = os.path.join(ROOT, "docs", "art", "splat-eye-palette-iter1158")
ASSET_DIR = os.path.join(ROOT, "Assets", "SpaceAndCrafts", "Resources", "Characters")
OUT_BLEND = os.path.join(OUT_DIR, "Splat_Character_Iter1158.blend")
OUT_FBX = os.path.join(ASSET_DIR, "Splat_Rigged_Candidate_Iter1158.fbx")
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SOURCE_BLEND)
body = bpy.data.objects["ART_SplatBody_Iter378"]
color = body.data.color_attributes.get("SplatArtColor")
if color is None or color.domain != "POINT":
    raise RuntimeError("Expected point-domain SplatArtColor vertex paint.")

# Cool only the warm tan eye-rim paint on the front of the fused head. The spatial
# gate protects the chest emblem, boot accents, blush, and all geometry/weights.
changed = []
old_colors = Counter()
for vertex, entry in zip(body.data.vertices, color.data):
    x, y, z = vertex.co
    r, g, b, a = entry.color
    is_front_eye_region = y < -0.33 and abs(x) < 0.30 and 0.78 < z < 1.42
    is_warm_cream = r > 0.75 and g > 0.70 and b > 0.55 and (r - g) < 0.16 and (g - b) > 0.08
    if is_front_eye_region and is_warm_cream:
        old_colors[tuple(round(v, 3) for v in (r, g, b))] += 1
        entry.color = (0.94, 0.91, 1.0, a)
        changed.append((vertex.index, tuple(vertex.co)))

if len(changed) < 100:
    raise RuntimeError(f"Refusing to export: only {len(changed)} eye-rim vertices matched.")
print("[Iter1158] eye rim recolor vertices", len(changed), "source colors", old_colors.most_common(8))
print("[Iter1158] changed bounds", tuple(round(min(p[1][i] for p in changed), 4) for i in range(3)), tuple(round(max(p[1][i] for p in changed), 4) for i in range(3)))
print("[Iter1158] foot bones", [bone.name for bone in bpy.data.objects["Splat_SkinRig"].data.bones if "Foot" in bone.name])
shutil.copyfile(SOURCE_BLEND, OUT_BLEND + ".source-backup")
bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)

keep = {"Splat_SkinRig", "ART_SplatBody_Iter378", "ART_AntennaStem", "ART_AntennaLeaf"}
for obj in list(bpy.context.scene.objects):
    if obj.name not in keep:
        bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
rig = bpy.data.objects["Splat_SkinRig"]
for name in keep:
    obj = bpy.data.objects.get(name)
    if obj is not None:
        obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=OUT_FBX,
    use_selection=True, object_types={"ARMATURE", "MESH"}, use_mesh_modifiers=False,
    add_leaf_bones=False, bake_anim=False, apply_unit_scale=True,
    axis_forward="-Z", axis_up="Y", path_mode="AUTO", use_custom_props=False)
print("[Iter1158] exported", OUT_FBX)