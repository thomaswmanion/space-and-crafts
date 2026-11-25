from pathlib import Path
from collections import defaultdict, Counter
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/rigged_character.glb"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(MODEL))
obj = next(o for o in bpy.context.scene.objects if o.type == "MESH" and o.name == "char1")
armature = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
mesh = obj.data
for name in ("Hips", "LeftShoulder", "RightShoulder", "LeftArm", "RightArm", "LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg"):
    bone = armature.data.bones.get(name)
    if bone:
        matrix = armature.matrix_world @ bone.matrix_local
        print("REGION_BONE", name,
              "head", tuple(round(v, 3) for v in armature.matrix_world @ bone.head_local),
              "tail", tuple(round(v, 3) for v in armature.matrix_world @ bone.tail_local),
              "axes", [tuple(round(matrix.to_3x3().col[a][k], 3) for k in range(3)) for a in range(3)])

print("REGION_MATERIALS", [(i, m.name, tuple(round(x, 3) for x in m.diffuse_color)) for i, m in enumerate(mesh.materials)])
print("REGION_COLOR_ATTRIBUTES", [(a.name, a.domain, a.data_type) for a in mesh.color_attributes])

parent = list(range(len(mesh.vertices)))
rank = bytearray(len(parent))

def find(i):
    while parent[i] != i:
        parent[i] = parent[parent[i]]
        i = parent[i]
    return i

def union(a, b):
    a, b = find(a), find(b)
    if a == b:
        return
    if rank[a] < rank[b]:
        a, b = b, a
    parent[b] = a
    if rank[a] == rank[b]:
        rank[a] += 1

for edge in mesh.edges:
    union(*edge.vertices)
components = defaultdict(list)
for vertex in mesh.vertices:
    components[find(vertex.index)].append(vertex.index)
face_mats = defaultdict(Counter)
for face in mesh.polygons:
    face_mats[find(face.vertices[0])][face.material_index] += 1
groups = {group.index: group.name for group in obj.vertex_groups}
rows = []
for component, ids in components.items():
    pts = [obj.matrix_world @ mesh.vertices[i].co for i in ids]
    center = sum(pts, Vector()) / len(pts)
    bbox_min = tuple(round(min(p[k] for p in pts), 3) for k in range(3))
    bbox_max = tuple(round(max(p[k] for p in pts), 3) for k in range(3))
    weights = Counter()
    for i in ids:
        for influence in mesh.vertices[i].groups:
            weights[groups.get(influence.group, str(influence.group))] += influence.weight
    material_counts = face_mats[component]
    mats = sorted(material_counts.items(), key=lambda row: row[1], reverse=True)
    colors = []
    for attribute in mesh.color_attributes:
        if attribute.domain == "POINT":
            samples = [attribute.data[i].color for i in ids]
            colors.append((attribute.name, tuple(round(sum(c[k] for c in samples) / len(samples), 3) for k in range(4))))
    rows.append((len(ids), component, tuple(round(center[k], 3) for k in range(3)), bbox_min, bbox_max, mats,
                 colors, weights.most_common(4)))
for row in sorted(rows, reverse=True)[:120]:
    print("REGION", row)
print("REGION_TOTALS", len(mesh.vertices), len(mesh.polygons), len(components),
      "material_faces", dict(Counter(p.material_index for p in mesh.polygons)))
