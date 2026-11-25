"""Inspect the exported Splat FBX hierarchy, skin, UVs, and image references."""

import os
import bpy


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX_PATH = os.path.join(PROJECT, "Assets", "SpaceAndCrafts", "Resources", "Characters", "Splat_Rigged.fbx")


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX_PATH, automatic_bone_orientation=False)

armatures = [obj for obj in bpy.data.objects if obj.type == "ARMATURE"]
meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
print("SPLAT_FBX_IMPORT", FBX_PATH, sep="\n")
for armature in armatures:
    print("ARMATURE", armature.name, "bones", len(armature.data.bones))
    for bone in armature.data.bones:
        print(" BONE", bone.name)
for mesh in meshes:
    print(
        "MESH",
        mesh.name,
        "vertices", len(mesh.data.vertices),
        "groups", len(mesh.vertex_groups),
        "uv_layers", len(mesh.data.uv_layers),
        "active_uv", mesh.data.uv_layers.active.name if mesh.data.uv_layers.active else "none",
    )
    for material in mesh.data.materials:
        print(" MATERIAL", material.name)
        if material.node_tree is None:
            continue
        for node in material.node_tree.nodes:
            if node.type == "TEX_IMAGE" and node.image is not None:
                print("  IMAGE", node.image.name, node.image.filepath)

print("ROOTS", [obj.name for obj in bpy.data.objects if obj.parent is None])
