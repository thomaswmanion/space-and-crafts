from collections import defaultdict
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/rigged_character.glb"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(MODEL))
scene = bpy.context.scene
mesh = next(obj for obj in scene.objects if obj.type == "MESH" and obj.name == "char1")
armature = next(obj for obj in scene.objects if obj.type == "ARMATURE")

for name in ("LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
             "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"):
    bone = armature.data.bones.get(name)
    if bone is not None:
        print("FOOT_BONE", name,
              "head", tuple(round(x, 4) for x in bone.head_local),
              "tail", tuple(round(x, 4) for x in bone.tail_local))

parent = list(range(len(mesh.data.vertices)))
rank = bytearray(len(parent))

def find(index):
    while parent[index] != index:
        parent[index] = parent[parent[index]]
        index = parent[index]
    return index

def union(a, b):
    a, b = find(a), find(b)
    if a == b:
        return
    if rank[a] < rank[b]:
        a, b = b, a
    parent[b] = a
    if rank[a] == rank[b]:
        rank[a] += 1

for edge in mesh.data.edges:
    union(*edge.vertices)

components = defaultdict(list)
for vertex in mesh.data.vertices:
    components[find(vertex.index)].append(vertex)

group_names = {group.index: group.name for group in mesh.vertex_groups}
for vertices in components.values():
    coordinates = [mesh.matrix_world @ vertex.co for vertex in vertices]
    bounds = [
        (min(point[axis] for point in coordinates), max(point[axis] for point in coordinates))
        for axis in range(3)
    ]
    center = tuple(sum(point[axis] for point in coordinates) / len(coordinates) for axis in range(3))
    if bounds[2][0] > 0.30 or bounds[2][1] > 0.45 or abs(center[0]) < 0.14 or center[1] > 0.25:
        continue
    weights = defaultdict(float)
    for vertex in vertices:
        for membership in vertex.groups:
            weights[group_names[membership.group]] += membership.weight
    top_weights = sorted(weights.items(), key=lambda item: item[1], reverse=True)[:6]
    print("LOW_LIMB_REGION", len(vertices),
          "center", tuple(round(x, 4) for x in center),
          "bounds", tuple((round(a, 4), round(b, 4)) for a, b in bounds),
          "groups", [(name, round(weight, 2)) for name, weight in top_weights])

bpy.ops.wm.quit_blender()
