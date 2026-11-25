import bpy

MODEL = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\Splat_300K_Remesh_Iter1397_Corrected.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=MODEL, automatic_bone_orientation=False)

mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
total_vertices = total_polygons = total_triangles = 0
print("ITER1397_CORRECTED_REMESH_AUDIT_START")
print(f"model={MODEL}")
print(f"mesh_objects={len(mesh_objects)} armatures={len(armatures)}")
for obj in mesh_objects:
    mesh = obj.data
    mesh.calc_loop_triangles()
    vertices = len(mesh.vertices)
    polygons = len(mesh.polygons)
    triangles = len(mesh.loop_triangles)
    total_vertices += vertices
    total_polygons += polygons
    total_triangles += triangles
    groups = [group.name for group in obj.vertex_groups]
    materials = [slot.material.name if slot.material else "<none>" for slot in obj.material_slots]
    uv_layers = [layer.name for layer in mesh.uv_layers]
    print(
        f"mesh={obj.name!r} vertices={vertices} polygons={polygons} triangles={triangles} "
        f"vertex_groups={len(groups)} groups={groups[:30]} materials={materials} uv_layers={uv_layers}"
    )
for armature in armatures:
    print(f"armature={armature.name!r} bones={len(armature.data.bones)} "
          f"names={[bone.name for bone in armature.data.bones]}")
print(f"total_vertices={total_vertices} total_polygons={total_polygons} total_triangles={total_triangles}")
print("ITER1397_CORRECTED_REMESH_AUDIT_END")
