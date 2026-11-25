import bpy
p=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Commissioned\Enemies\MeshySpenddown\20261101_rainbow-spider-rig-candidate-iter1461\RainbowSpider_RigCandidate.blend'
bpy.ops.wm.open_mainfile(filepath=p)
for im in bpy.data.images:
 print('IMAGE',repr(im.name),'SIZE',tuple(im.size),'PATH',repr(im.filepath),'PACKED',bool(im.packed_file),'SOURCE',im.source)
for m in bpy.data.materials:
 print('MAT',m.name)
 if m.use_nodes:
  for n in m.node_tree.nodes:
   if n.type=='TEX_IMAGE': print('NODE',n.name, 'IMAGE',n.image.name if n.image else None)
