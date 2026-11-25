import bpy,os
p=os.path.abspath("Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261031_223339_rainbow-kingdom-spider-enemy_01a1952b/RainbowKingdomSpider.glb")
bpy.ops.object.select_all(action="SELECT"); bpy.ops.object.delete(use_global=False); bpy.ops.import_scene.gltf(filepath=p)
m=next(o for o in bpy.context.scene.objects if o.type=="MESH").data.materials[0]
out=os.path.abspath("Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261101_rainbow-spider-angle-review-iter1408")
os.makedirs(out,exist_ok=True)
for n in m.node_tree.nodes:
 if n.type=="TEX_IMAGE" and n.image:
  n.image.filepath_raw=os.path.join(out,n.image.name+".png")
  n.image.file_format="PNG"
  n.image.save()
  print("SAVED",n.image.filepath_raw,n.image.size[:])