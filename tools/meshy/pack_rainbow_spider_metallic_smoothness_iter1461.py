import bpy, os
from array import array
blend=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Commissioned\Enemies\MeshySpenddown\20261101_rainbow-spider-rig-candidate-iter1461\RainbowSpider_RigCandidate.blend'
out=os.path.dirname(blend)
bpy.ops.wm.open_mainfile(filepath=blend)
src=bpy.data.images['Image_1']
w,h=src.size
source=array('f',src.pixels[:])
packed=array('f',[0.0])*(w*h*4)
for i in range(w*h):
 j=i*4
 packed[j]=source[j+2]
 packed[j+1]=0.0
 packed[j+2]=0.0
 packed[j+3]=1.0-source[j+1]
im=bpy.data.images.new('Spider_MetallicSmoothness',width=w,height=h,alpha=True,float_buffer=False)
im.pixels[:]=packed
im.filepath_raw=os.path.join(out,'Spider_MetallicSmoothness.png')
im.file_format='PNG'
im.save()
print('PACKED',w,h,'source blue metallic, inverse green roughness to smoothness alpha')
