from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/rigged_character.glb"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(MODEL))
scene = bpy.context.scene
print("SPLAT_MESHY_INSPECT model", MODEL.name)
for obj in scene.objects:
    if obj.type == "ARMATURE":
        print("ARMATURE", obj.name, "loc", tuple(round(x, 5) for x in obj.location), "rot", tuple(round(x, 5) for x in obj.rotation_euler), "scale", tuple(round(x, 5) for x in obj.scale))
        for bone in obj.data.bones:
            head = obj.matrix_world @ bone.head_local
            tail = obj.matrix_world @ bone.tail_local
            print("BONE", bone.name, "parent", bone.parent.name if bone.parent else "-", "head", tuple(round(x, 4) for x in head), "tail", tuple(round(x, 4) for x in tail))
    elif obj.type == "MESH":
        print("MESH", obj.name, "verts", len(obj.data.vertices), "polys", len(obj.data.polygons), "loc", tuple(round(x, 5) for x in obj.location), "scale", tuple(round(x, 5) for x in obj.scale), "bounds", [(round(v, 4) for v in obj.matrix_world @ __import__('mathutils').Vector(corner)) for corner in obj.bound_box])
for action in bpy.data.actions:
    print("ACTION", action.name, tuple(round(float(x), 3) for x in action.frame_range))
    for layer in action.layers:
        for strip in layer.strips:
            for slot in action.slots:
                bag = strip.channelbag(slot)
                if bag:
                    for curve in bag.fcurves:
                        print("CURVE", action.name, curve.data_path, curve.array_index,
                              [(round(k.co.x, 3), round(k.co.y, 4)) for k in curve.keyframe_points])
