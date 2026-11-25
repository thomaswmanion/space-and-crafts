import bpy
bpy.ops.wm.open_mainfile(filepath=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Commissioned\Enemies\MeshySpenddown\20261101_rainbow-spider-rig-candidate-iter1461\RainbowSpider_RigCandidate.blend')
m=bpy.data.materials['Material_0']
for n in m.node_tree.nodes:
 print('NODE',n.name,n.type,[(s.name,s.is_linked) for s in n.outputs])
for l in m.node_tree.links:
 print(l.from_node.name,l.from_socket.name,'->',l.to_node.name,l.to_socket.name)
