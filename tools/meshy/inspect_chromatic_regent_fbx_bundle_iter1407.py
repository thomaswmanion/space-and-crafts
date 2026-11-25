import bpy
import os

path = os.path.abspath(
    "Assets/SpaceAndCrafts/Art/Commissioned/WorldBosses/"
    "20261101_chromatic-regent-animation-bundle/ChromaticRegent_AnimationBundle.fbx"
)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=path)
arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
print(f"FBX_ROUNDTRIP armatures={len(arms)} meshes={len(meshes)} actions={len(bpy.data.actions)}")
for arm in arms:
    action = arm.animation_data.action if arm.animation_data else None
    print(f"ARMATURE {arm.name} bones={len(arm.data.bones)} active_action={action.name if action else None}")
print("ACTIONS=" + repr(sorted((a.name, tuple(a.frame_range)) for a in bpy.data.actions)))
print("MESHES=" + repr([(m.name, len(m.data.vertices), len(m.vertex_groups)) for m in meshes]))

