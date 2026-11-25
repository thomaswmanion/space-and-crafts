import bpy
p=r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Enemies\Pulpal\Meshy_Iter1994\Pulpal_Rigged.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=p)
a=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
for b in a.data.bones: print(b.name,'parent=',b.parent.name if b.parent else '-', 'head=',tuple(round(v,3) for v in b.head_local),'tail=',tuple(round(v,3) for v in b.tail_local))

