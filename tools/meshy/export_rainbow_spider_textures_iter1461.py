import bpy, os
blend=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Commissioned\Enemies\MeshySpenddown\20261101_rainbow-spider-rig-candidate-iter1461\RainbowSpider_RigCandidate.blend'
out=os.path.dirname(blend)
bpy.ops.wm.open_mainfile(filepath=blend)
for image in bpy.data.images:
 if image.name=='Image_0': name='Spider_BaseColor.png'
 elif image.name=='Image_1': name='Spider_MetallicRoughness.png'
 elif image.name=='Image_2': name='Spider_Normal.png'
 else: continue
 image.filepath_raw=os.path.join(out,name)
 image.file_format='PNG'
 image.save()
 print('SAVED',name,tuple(image.size))
