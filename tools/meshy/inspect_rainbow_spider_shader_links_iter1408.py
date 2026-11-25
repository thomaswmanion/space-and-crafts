import bpy,os
p=os.path.abspath("Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261031_223339_rainbow-kingdom-spider-enemy_01a1952b/RainbowKingdomSpider.glb")
bpy.ops.object.select_all(action="SELECT"); bpy.ops.object.delete(use_global=False); bpy.ops.import_scene.gltf(filepath=p)
m=next(o for o in bpy.context.scene.objects if o.type=="MESH").data.materials[0]
for n in m.node_tree.nodes:
 print("NODE",n.name,n.type)
 if n.type=="TEX_IMAGE": print("  image",n.image.name if n.image else None)
for l in m.node_tree.links: print("LINK",l.from_node.name,l.from_socket.name,"=>",l.to_node.name,l.to_socket.name)