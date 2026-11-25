import bpy
import os

base = os.path.abspath(
    "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/MeshySpenddown/"
    "20261031_223817_rainbow-kingdom-chromatic-rege_01a1952f/exports"
)
paths = [
    "rig-and-locomotion/rigged_character.glb",
    "rig-and-locomotion/walking_glb.glb",
    "rig-and-locomotion/running_glb.glb",
    "attack/animation_glb.glb",
    "hit-reaction/animation_glb.glb",
    "knockdown/animation_glb.glb",
]
for relative in paths:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=os.path.join(base, relative))
    arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    print(f"FILE {relative} armatures={len(arms)} meshes={len(meshes)}")
    for arm in arms:
        action = arm.animation_data.action if arm.animation_data else None
        bone_names = [b.name for b in arm.data.bones]
        slots = len(action.slots) if action is not None and hasattr(action, "slots") else -1
        print(f" ARM {arm.name} bones={len(arm.data.bones)} action={action.name if action else None} "
              f"range={tuple(action.frame_range) if action else None} slots={slots} names={bone_names}")
        if action is not None:
            print(f" ACTION_API layers={len(action.layers)} frame_range={tuple(action.frame_range)} "
                  f"has_legacy_fcurves={hasattr(action, 'fcurves')}")
            for layer in action.layers:
                for strip in layer.strips:
                    for bag in strip.channelbags:
                        paths = [fc.data_path for fc in bag.fcurves[:8]]
                        slot_info = (bag.slot.identifier, bag.slot.target_id_type) if bag.slot else None
                        print(f"  SLOT={slot_info} curves={len(bag.fcurves)} paths={paths}")
    print(f" MESH_OBJECTS={[(m.name, len(m.data.vertices), m.parent.name if m.parent else None,
                          len(m.vertex_groups), m.hide_viewport, m.hide_render,
                          tuple(round(v, 3) for v in m.location),
                          tuple(round(v, 3) for v in m.dimensions)) for m in meshes]}")
