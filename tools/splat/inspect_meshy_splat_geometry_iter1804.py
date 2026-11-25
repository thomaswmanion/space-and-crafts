import bpy, os, json
src = r'E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyRig_Iter1791\rigged_character.fbx'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=src, use_anim=False)
for ob in bpy.data.objects:
    if ob.type != 'MESH': continue
    print('OBJECT', ob.name, 'verts', len(ob.data.vertices), 'dims', tuple(round(x,4) for x in ob.dimensions), 'loc', tuple(round(x,4) for x in ob.location), 'parent', ob.parent.name if ob.parent else None)
    print('MATS', [(m.name if m else None, len(ob.data.polygons)) for m in ob.data.materials])
    for mi,m in enumerate(ob.data.materials):
        if m:
            n=sum(1 for p in ob.data.polygons if p.material_index==mi)
            print(' MATERIAL', mi, m.name, 'faces', n)
    if ob.data.materials:
        for mat_i,m in enumerate(ob.data.materials):
            if not m: continue
            idx={i for p in ob.data.polygons if p.material_index==mat_i for i in p.vertices}
            if not idx: continue
            coords=[ob.matrix_world @ ob.data.vertices[i].co for i in idx]
            mins=[min(v[j] for v in coords) for j in range(3)]; maxs=[max(v[j] for v in coords) for j in range(3)]
            print(' REGION',m.name,'v',len(idx),'bounds',[(round(mins[j],4),round(maxs[j],4)) for j in range(3)])
print('ARMATURES', [(o.name, len(o.data.bones)) for o in bpy.data.objects if o.type=='ARMATURE'])
