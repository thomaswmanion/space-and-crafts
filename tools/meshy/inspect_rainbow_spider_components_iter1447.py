import bpy, bmesh, json, os
from mathutils import Vector
root = r'E:\Users\thoma\git\space-and-crafts-3'
path = os.path.join(root, 'Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261031_223339_rainbow-kingdom-spider-enemy_01a1952b/RainbowKingdomSpider.glb')
out = os.path.join(root, 'Logs/spider-source-components-iter1447.json')
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=path)
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
bm = bmesh.new(); bm.from_mesh(obj.data)
for f in bm.faces: f.tag = False
comps=[]
for seed in bm.faces:
    if seed.tag: continue
    seed.tag=True; stack=[seed]; faces=[]
    while stack:
        f=stack.pop(); faces.append(f)
        for e in f.edges:
            for n in e.link_faces:
                if not n.tag: n.tag=True; stack.append(n)
    coords=[obj.matrix_world @ f.calc_center_median() for f in faces]
    mn=Vector((min(p.x for p in coords),min(p.y for p in coords),min(p.z for p in coords)))
    mx=Vector((max(p.x for p in coords),max(p.y for p in coords),max(p.z for p in coords)))
    center=(mn+mx)*.5
    comps.append({'faces':len(faces),'center':[round(x,4) for x in center],'size':[round(x,4) for x in mx-mn]})
comps.sort(key=lambda x:x['faces'],reverse=True)
with open(out,'w') as f: json.dump({'mesh':obj.name,'faces':len(bm.faces),'vertices':len(bm.verts),'components':len(comps),'largest':comps[:80]},f,indent=2)
print('COMPONENTS',len(comps),'FACES',len(bm.faces),'REPORT',out)
bm.free()
