from pathlib import Path
import bpy
root=Path(__file__).resolve().parents[2]
p=root/'Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter2000_Meshy71/Splat_Iter2000_Remeshed_Rigged.glb'
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=str(p))
for o in bpy.context.scene.objects:
 print(o.name,o.type,'hidden',o.hide_get(),'hide_render',o.hide_render,'hide_viewport',o.hide_viewport,'display',o.display_type,'parent',o.parent.name if o.parent else '-', 'verts',len(o.data.vertices) if o.type=='MESH' else '-')
