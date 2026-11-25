import bpy, os
src=os.path.abspath('Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/20261031_223339_rainbow-kingdom-spider-enemy_01a1952b/RainbowKingdomSpider.glb')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
m=next(o for o in bpy.context.scene.objects if o.type=='MESH')
vs=[v.co for v in m.data.vertices]
print('BOUNDS',tuple(round(min(getattr(v,a) for v in vs),4) for a in 'xyz'),tuple(round(max(getattr(v,a) for v in vs),4) for a in 'xyz'))
for threshold in [-0.45,-0.5,-0.55,-0.6,-0.62,-0.64,-0.66]:
 p=[v for v in vs if v.z<threshold and abs(v.x)>0.45]
 print('Z',threshold,'n',len(p))
 # bins x side, y clusters .2 wide
 for side in [-1,1]:
  q=[v for v in p if v.x*side>0]
  bins={}
  for v in q: bins.setdefault(round(v.y/0.1)*0.1,[]).append(v)
  print('side',side,[(k,len(x),round(min(v.z for v in x),3),round(min(abs(v.x) for v in x),3),round(max(abs(v.x) for v in x),3)) for k,x in sorted(bins.items())])
