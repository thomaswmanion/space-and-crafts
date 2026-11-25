import bpy
import bmesh
import numpy as np
from mathutils import Matrix, Vector
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree

RIGGED_FBX = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\20261030_224550_splat-iter1337_01a1900f\FBX\rigged_character.fbx"
REMESH_FBX = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\Splat_300k_Remesh_Iter1365.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=RIGGED_FBX, automatic_bone_orientation=False)
source_obj = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.vertex_groups)
source_points = np.array([(source_obj.matrix_world @ v.co)[:] for v in source_obj.data.vertices], dtype=np.float64)
source_tree = KDTree(len(source_points))
for index, p in enumerate(source_points):
    source_tree.insert(Vector(p), index)
source_tree.balance()

source_bm = bmesh.new()
source_bm.from_mesh(source_obj.data)
source_bm.transform(source_obj.matrix_world)
source_bm.normal_update()
source_surface = BVHTree.FromBMesh(source_bm)

bpy.ops.import_scene.fbx(filepath=REMESH_FBX, automatic_bone_orientation=False)
candidate_obj = next(obj for obj in bpy.context.scene.objects
                     if obj.type == "MESH" and obj != source_obj and not obj.vertex_groups)
candidate_points = np.array([(candidate_obj.matrix_world @ v.co)[:] for v in candidate_obj.data.vertices], dtype=np.float64)
sample_points = candidate_points[::max(1, len(candidate_points) // 10000)]

def summarize(values):
    values = sorted(float(value) for value in values)
    def pct(q):
        return values[min(len(values) - 1, round((len(values) - 1) * q))]
    return f"n={len(values)} median={pct(.50):.6f}m p90={pct(.90):.6f}m p99={pct(.99):.6f}m max={values[-1]:.6f}m"

print("ITER1368_RIG_SURFACE_REGISTRATION_START")
print(f"rigged_mesh={source_obj.name!r} vertices={len(source_points)} vertex_groups={len(source_obj.vertex_groups)}")
print(f"remesh_mesh={candidate_obj.name!r} vertices={len(candidate_points)} sample={len(sample_points)}")

axis_maps = {
    "Yup_to_Zup_negative90X": np.array([[1, 0, 0], [0, 0, 1], [0, -1, 0]], dtype=np.float64),
    "Yup_to_Zup_positive90X": np.array([[1, 0, 0], [0, 0, -1], [0, 1, 0]], dtype=np.float64),
}
best = None
for axis_name, axis in axis_maps.items():
    initial = sample_points @ axis
    for yaw_degrees in (0, 90, 180, 270):
        yaw = np.radians(yaw_degrees)
        yaw_matrix = np.array([[np.cos(yaw), np.sin(yaw), 0],
                               [-np.sin(yaw), np.cos(yaw), 0],
                               [0, 0, 1]], dtype=np.float64)
        base = initial @ yaw_matrix
        source_min = source_points.min(axis=0)
        source_max = source_points.max(axis=0)
        base_min = base.min(axis=0)
        base_max = base.max(axis=0)
        scale = float((source_max[2] - source_min[2]) / (base_max[2] - base_min[2]))
        translation = (source_min + source_max) * .5 - scale * (base_min + base_max) * .5
        rotation = np.identity(3)
        for iteration in range(16):
            aligned = scale * (base @ rotation) + translation
            matched_indices = [source_tree.find(Vector(p))[1] for p in aligned]
            matched = source_points[matched_indices]
            x_mean = base.mean(axis=0)
            y_mean = matched.mean(axis=0)
            x_centered = base - x_mean
            y_centered = matched - y_mean
            u, singular, vt = np.linalg.svd(x_centered.T @ y_centered)
            rotation = u @ vt
            if np.linalg.det(rotation) < 0:
                u[:, -1] *= -1
                rotation = u @ vt
            scale = float(singular.sum() / np.square(x_centered).sum())
            translation = y_mean - scale * (x_mean @ rotation)
        aligned = scale * (base @ rotation) + translation
        point_distances = [source_tree.find(Vector(p))[2] for p in aligned]
        ranked = sorted(point_distances)
        median = ranked[len(ranked) // 2]
        print(f"seed={axis_name},yaw={yaw_degrees} scale={scale:.6f} "
              f"translation={tuple(round(v, 5) for v in translation)} "
              f"rotation={tuple(tuple(round(float(v), 5) for v in row) for row in rotation)} "
              f"vertex_distances={summarize(ranked)}")
        if best is None or median < best[0]:
            best = (median, base, rotation.copy(), scale, translation.copy(), axis_name, yaw_degrees)

_, base, rotation, scale, translation, axis_name, yaw_degrees = best
aligned_all = scale * ((candidate_points @ axis_maps[axis_name]) @
                       np.array([[np.cos(np.radians(yaw_degrees)), np.sin(np.radians(yaw_degrees)), 0],
                                 [-np.sin(np.radians(yaw_degrees)), np.cos(np.radians(yaw_degrees)), 0],
                                 [0, 0, 1]], dtype=np.float64) @ rotation) + translation
step = max(1, len(aligned_all) // 5000)
surface_distances = []
for p in aligned_all[::step]:
    nearest = source_surface.find_nearest(Vector(p))
    if nearest is not None:
        surface_distances.append(nearest[3])
print(f"best_seed={axis_name},yaw={yaw_degrees} scale={scale:.6f}")
print("nearest_rigged_surface_distances=" + summarize(surface_distances))
print("ITER1368_RIG_SURFACE_REGISTRATION_END")
source_bm.free()
