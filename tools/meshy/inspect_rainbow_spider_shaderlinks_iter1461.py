import bpy
bpy.ops.wm.open_mainfile(filepath=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Commissioned\Enemies\MeshySpenddown\20261101_rainbow-spider-rig-candidate-iter1461\RainbowSpider_RigCandidate.blend')
for m in bpy.data.materials:
 if not m.use_nodes: continue
 print('MATERIAL',m.name)
 for link in m.node_tree.links:
  if link.from_node.type=='TEX_IMAGE': print(link.from_node.image.name,link.from_node.image.size[:],link.from_socket.name,'->',link.to_node.type,link.to_socket.name)
