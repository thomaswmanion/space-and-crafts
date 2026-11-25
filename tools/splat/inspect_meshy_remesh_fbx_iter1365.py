import bpy
import os

MODEL = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\Splat_300k_Remesh_Iter1365.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=MODEL, automatic_bone_orientation=False)

mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
print("ITER1365_REMesh_FBX_AUDIT_START")
print(f"model={MODEL}")
print(f"mesh_objects={len(mesh_objects)} armatures={len(armatures)}")

total_vertices = 0
total_polygons = 0
total_triangles = 0
for obj in mesh_objects:
    mesh = obj.data
    mesh.calc_loop_triangles()
    vertices = len(mesh.vertices)
    polygons = len(mesh.polygons)
    triangles = len(mesh.loop_triangles)
    total_vertices += vertices
    total_polygons += polygons
    total_triangles += triangles
    world_corners = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
    mins = tuple(min(point[i] for point in world_corners) for i in range(3))
    maxs = tuple(max(point[i] for point in world_corners) for i in range(3))
    materials = [slot.material.name if slot.material else "<none>" for slot in obj.material_slots]
    deform_groups = [group.name for group in obj.vertex_groups]
    print(
        f"mesh={obj.name!r} vertices={vertices} polygons={polygons} triangles={triangles} "
        f"world_min={tuple(round(v, 5) for v in mins)} world_max={tuple(round(v, 5) for v in maxs)} "
        f"materials={materials} vertex_groups={len(deform_groups)}"
    )

print(f"total_vertices={total_vertices} total_polygons={total_polygons} total_triangles={total_triangles}")
for armature in armatures:
    print(f"armature={armature.name!r} bones={len(armature.data.bones)}")
print("ITER1365_REMesh_FBX_AUDIT_END")
