import bpy
import math
import bmesh
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree

RIGGED_FBX = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\20261030_224550_splat-iter1337_01a1900f\FBX\rigged_character.fbx"
REMESH_FBX = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\Splat_300k_Remesh_Iter1365.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=RIGGED_FBX, automatic_bone_orientation=False)
rigged_objects = list(bpy.context.scene.objects)
rigged_meshes = [obj for obj in rigged_objects if obj.type == "MESH"]
rigged_armatures = [obj for obj in rigged_objects if obj.type == "ARMATURE"]

def report_mesh(label, obj):
    mesh = obj.data
    mesh.calc_loop_triangles()
    world = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
    mins = tuple(min(p[i] for p in world) for i in range(3))
    maxs = tuple(max(p[i] for p in world) for i in range(3))
    print(f"{label} mesh={obj.name!r} vertices={len(mesh.vertices)} triangles={len(mesh.loop_triangles)} "
          f"groups={len(obj.vertex_groups)} materials={[s.material.name if s.material else '<none>' for s in obj.material_slots]} "
          f"world_min={tuple(round(v, 5) for v in mins)} world_max={tuple(round(v, 5) for v in maxs)}")
    return world

print("ITER1366_REMESH_RIG_COMPARISON_START")
print(f"rigged_fbx_meshes={len(rigged_meshes)} armatures={len(rigged_armatures)}")
for obj in rigged_meshes:
    report_mesh("rig_source", obj)
for armature in rigged_armatures:
    print(f"armature={armature.name!r} bones={len(armature.data.bones)} root={armature.data.bones[0].name if armature.data.bones else '<none>'}")

source = next((obj for obj in rigged_meshes if obj.vertex_groups), None)
if source is None:
    print("NO_WEIGHTED_SOURCE_MESH")
else:
    source_world = [source.matrix_world @ vertex.co for vertex in source.data.vertices]
    tree = KDTree(len(source_world))
    for index, point in enumerate(source_world):
        tree.insert(point, index)
    tree.balance()
    source_bmesh = bmesh.new()
    source_bmesh.from_mesh(source.data)
    source_bmesh.transform(source.matrix_world)
    source_bmesh.normal_update()
    surface_tree = BVHTree.FromBMesh(source_bmesh)

bpy.ops.import_scene.fbx(filepath=REMESH_FBX, automatic_bone_orientation=False)
remesh_meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj not in rigged_meshes]
print(f"remesh_fbx_meshes={len(remesh_meshes)}")
for obj in remesh_meshes:
    points = report_mesh("remesh", obj)
    if source is not None:
        # The rig FBX is Z-up at 1.7m; the remesh FBX is Y-up at about
        # 1.3m. Normalize the candidate to the rig's axis, height, and base
        # before comparing the two surfaces (metrics only; no mesh is altered).
        # Fit the candidate's axis-aligned bounds to the rig source for a
        # generous upper-bound comparison. Any per-axis fitting is analysis
        # only and is not baked into the asset.
        scale_x = 2.21442 / 1.90304
        scale_y = 1.51350 / 1.46093
        height_scale = 1.7 / 1.30063
        rig_space_points = [
            ((point.x + 0.00043) * scale_x,
             -(point.z - 0.001225) * scale_y,
             (point.y + 0.65007) * height_scale)
            for point in points
        ]
        distances = []
        surface_distances = []
        step = max(1, len(rig_space_points) // 5000)
        for point in rig_space_points[::step]:
            _, _, distance = tree.find(point)
            distances.append(distance)
            nearest = surface_tree.find_nearest(point)
            if nearest is not None:
                surface_distances.append(nearest[3])
        distances.sort()
        def percentile(q):
            return distances[min(len(distances) - 1, round((len(distances) - 1) * q))]
        print(f"per_axis_bounds_normalized_nearest_source_vertex_sample={len(distances)} median_m={percentile(.50):.6f} "
              f"p90_m={percentile(.90):.6f} p99_m={percentile(.99):.6f} max_sample_m={distances[-1]:.6f}")
        surface_distances.sort()
        print(f"per_axis_bounds_normalized_nearest_source_surface_sample={len(surface_distances)} "
              f"median_m={surface_distances[len(surface_distances) // 2]:.6f} "
              f"p90_m={surface_distances[min(len(surface_distances) - 1, round((len(surface_distances) - 1) * .90))]:.6f} "
              f"p99_m={surface_distances[min(len(surface_distances) - 1, round((len(surface_distances) - 1) * .99))]:.6f}")
        if rigged_armatures:
            print(f"source_armature_groups={len(source.vertex_groups)} sample_groups={source.vertex_groups[0].name!r}")

print("ITER1366_REMESH_RIG_COMPARISON_END")
if source is not None:
    source_bmesh.free()
