import bpy
import os

src = os.path.abspath(
    "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/"
    "20261031_223339_rainbow-kingdom-spider-enemy_01a1952b/RainbowKingdomSpider.glb"
)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=src)
mesh = next(o for o in bpy.context.scene.objects if o.type == "MESH")
parent = list(range(len(mesh.data.vertices)))
size = [1] * len(parent)

def find(x):
    while parent[x] != x:
        parent[x] = parent[parent[x]]
        x = parent[x]
    return x

for edge in mesh.data.edges:
    a, b = find(edge.vertices[0]), find(edge.vertices[1])
    if a != b:
        if size[a] < size[b]:
            a, b = b, a
        parent[b] = a
        size[a] += size[b]

components = {}
for v in mesh.data.vertices:
    components.setdefault(find(v.index), []).append(v.co)
ranked = sorted(components.values(), key=len, reverse=True)
for i, verts in enumerate(ranked[:30]):
    xs = [v.x for v in verts]
    ys = [v.y for v in verts]
    zs = [v.z for v in verts]
    print(f"COMP {i} verts={len(verts)} bounds=({min(xs):.3f},{min(ys):.3f},{min(zs):.3f})..({max(xs):.3f},{max(ys):.3f},{max(zs):.3f})")
print(f"COMPONENT_COUNT={len(components)} VERTEX_COUNT={len(mesh.data.vertices)}")
