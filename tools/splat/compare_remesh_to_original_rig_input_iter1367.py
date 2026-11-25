import bpy
import bmesh
from mathutils.bvhtree import BVHTree

RIG_INPUT = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\Splat_Meshy_Iter1337_RigInput.glb"
REMESH_FBX = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\Splat_300k_Remesh_Iter1365.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=RIG_INPUT)
source_meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
source_trees = []
source_points = []
for obj in source_meshes:
    mesh = obj.data
    mesh.calc_loop_triangles()
    points = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
    source_points.extend(points)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.transform(obj.matrix_world)
    bm.normal_update()
    source_trees.append(BVHTree.FromBMesh(bm))
    print(f"rig_input_mesh={obj.name!r} vertices={len(mesh.vertices)} triangles={len(mesh.loop_triangles)} "
          f"groups={len(obj.vertex_groups)} materials={[s.material.name if s.material else '<none>' for s in obj.material_slots]}")

def nearest_surface_distance(point):
    return min(tree.find_nearest(point)[3] for tree in source_trees)

bpy.ops.import_scene.fbx(filepath=REMESH_FBX, automatic_bone_orientation=False)
remesh_meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj not in source_meshes]
print("ITER1367_REMESH_TO_RIG_INPUT_START")
print(f"rig_input_meshes={len(source_meshes)} remesh_meshes={len(remesh_meshes)}")
for obj in remesh_meshes:
    mesh = obj.data
    mesh.calc_loop_triangles()
    points = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
    mins = tuple(min(point[i] for point in points) for i in range(3))
    maxs = tuple(max(point[i] for point in points) for i in range(3))
    print(f"remesh_mesh={obj.name!r} vertices={len(mesh.vertices)} triangles={len(mesh.loop_triangles)} "
          f"world_min={tuple(round(v, 5) for v in mins)} world_max={tuple(round(v, 5) for v in maxs)}")
    step = max(1, len(points) // 5000)
    distances = sorted(nearest_surface_distance(point) for point in points[::step])
    def percentile(q):
        return distances[min(len(distances) - 1, round((len(distances) - 1) * q))]
    print(f"raw_axis_nearest_rig_input_surface_sample={len(distances)} median_m={percentile(.50):.6f} "
          f"p90_m={percentile(.90):.6f} p99_m={percentile(.99):.6f} max_sample_m={distances[-1]:.6f}")
print(f"rig_input_total_vertices={len(source_points)}")
print("ITER1367_REMESH_TO_RIG_INPUT_END")
