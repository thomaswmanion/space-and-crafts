from pathlib import Path
from collections import defaultdict
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/rigged_character.glb"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(MODEL))
obj = next(o for o in bpy.context.scene.objects if o.type == "MESH" and o.name == "char1")
mesh = obj.data
n = len(mesh.vertices)
parent = list(range(n))
rank = bytearray(n)

def find(i):
    while parent[i] != i:
        parent[i] = parent[parent[i]]
        i = parent[i]
    return i

def union(a, b):
    ra, rb = find(a), find(b)
    if ra == rb:
        return
    if rank[ra] < rank[rb]:
        ra, rb = rb, ra
    parent[rb] = ra
    if rank[ra] == rank[rb]:
        rank[ra] += 1

for edge in mesh.edges:
    union(edge.vertices[0], edge.vertices[1])

groups = defaultdict(list)
for v in mesh.vertices:
    groups[find(v.index)].append(v.index)
ordered = sorted(groups.values(), key=len, reverse=True)
names = {group.index: group.name for group in obj.vertex_groups}
matrix = obj.matrix_world
print("ISLANDS model", MODEL.name, "vertices", n, "faces", len(mesh.polygons), "connected_components", len(ordered))
for comp_index, vertex_ids in enumerate(ordered):
    xs, ys, zs = [], [], []
    weight_sums = defaultdict(float)
    for vertex_id in vertex_ids:
        v = mesh.vertices[vertex_id]
        p = matrix @ v.co
        xs.append(p.x); ys.append(p.y); zs.append(p.z)
        for element in v.groups:
            weight_sums[names.get(element.group, str(element.group))] += element.weight
    lo = (min(xs), min(ys), min(zs)); hi = (max(xs), max(ys), max(zs))
    top = sorted(weight_sums.items(), key=lambda item: item[1], reverse=True)[:5]
    center = tuple((lo[i] + hi[i]) * 0.5 for i in range(3))
    front_face = center[1] < -0.48 and center[2] > 0.82 and abs(center[0]) < 0.43
    if comp_index < 10 or front_face:
        print("COMP", comp_index, "front_face", front_face, "verts", len(vertex_ids), "bounds", tuple(round(x, 4) for x in lo), tuple(round(x, 4) for x in hi),
              "weights", [(name, round(value / len(vertex_ids), 3)) for name, value in top])
