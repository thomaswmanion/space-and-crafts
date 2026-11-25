import bpy, os
p=os.path.abspath("Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261031_223339_rainbow-kingdom-spider-enemy_01a1952b/RainbowKingdomSpider.glb")
bpy.ops.object.select_all(action="SELECT"); bpy.ops.object.delete(use_global=False); bpy.ops.import_scene.gltf(filepath=p)
for o in bpy.context.scene.objects:
 if o.type=="MESH":
  print("MESH",o.name,"verts",len(o.data.vertices),"uv",[(u.name,len(u.data)) for u in o.data.uv_layers])
  for m in o.data.materials:
   print("MAT",m.name,"nodes",m.use_nodes)
   if m.use_nodes:
    for n in m.node_tree.nodes:
     if n.type=="TEX_IMAGE": print("IMAGE",n.image.name if n.image else None, n.image.size[:] if n.image else None)
     if n.type=="BSDF_PRINCIPLED": print("BSDF",{i.name:(i.default_value[:] if hasattr(i.default_value,"__len__") else i.default_value) for i in n.inputs if i.name in ["Base Color","Metallic","Roughness"]})