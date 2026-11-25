import bpy
for path in [r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Commissioned\Hero\Splat_Hero.glb', r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Resources\Characters\Splat_Rigged_Candidate_Iter378.fbx']:
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    if path.lower().endswith('.glb'):
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        bpy.ops.import_scene.fbx(filepath=path)
    print('ASSET',path)
    for o in bpy.context.scene.objects:
        if o.type in {'MESH','ARMATURE'}:
            print('OBJECT',o.name,o.type,'loc',tuple(round(x,4) for x in o.location),'scale',tuple(round(x,4) for x in o.scale),'dims',tuple(round(x,4) for x in o.dimensions),'verts',len(o.data.vertices) if o.type=='MESH' else 'bones='+str(len(o.data.bones)))
            if o.type=='ARMATURE': print('BONES', [b.name for b in o.data.bones])
    print('END')
