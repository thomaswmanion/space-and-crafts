from pathlib import Path
import bpy
from collections import Counter, defaultdict
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
model = root / 'Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter2000_Meshy71/Splat_Iter2000_Remeshed_Rigged.glb'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(model))
print('MODEL', model)
for obj in bpy.context.scene.objects:
    if obj.type == 'ARMATURE':
        print('ARMATURE', obj.name, 'bones', len(obj.data.bones), 'dimensions', tuple(round(v,4) for v in obj.dimensions))
        for bone in obj.data.bones:
            if any(k in bone.name.lower() for k in ('hips','spine','head','arm','hand','leg','foot','toe')):
                print('BONE', bone.name, 'parent', bone.parent.name if bone.parent else '-', 'head', tuple(round(v,4) for v in (obj.matrix_world @ bone.head_local)), 'tail', tuple(round(v,4) for v in (obj.matrix_world @ bone.tail_local)))
    elif obj.type == 'MESH':
        mesh = obj.data
        print('MESH', obj.name, 'verts', len(mesh.vertices), 'faces', len(mesh.polygons), 'bounds', tuple(round(v,4) for v in obj.dimensions), 'worldloc', tuple(round(v,4) for v in obj.matrix_world.translation))
        print('MATERIALS', [(i, m.name if m else None, tuple(round(x,3) for x in m.diffuse_color) if m else None) for i,m in enumerate(mesh.materials)])
        print('FACE_MATERIAL_COUNTS', dict(Counter(p.material_index for p in mesh.polygons)))
        groups = {g.index:g.name for g in obj.vertex_groups}
        total = Counter(); by_group = defaultdict(list)
        for v in mesh.vertices:
            for inf in v.groups:
                if inf.weight > .05:
                    name=groups.get(inf.group, str(inf.group)); total[name]+=1; by_group[name].append(v.co)
        print('GROUP_VERTEX_COUNTS', total.most_common())
        for name, pts in by_group.items():
            if name.lower() in ('hips','spine','head','leftarm','rightarm','leftforearm','rightforearm','leftupleg','rightupleg','leftleg','rightleg','leftfoot','rightfoot'):
                center=sum(pts,Vector())/len(pts)
                print('GROUP_REGION',name,'weightedVerts',len(pts),'center',tuple(round(v,4) for v in center))
        print('COLOR_ATTRIBUTES', [(a.name,a.domain,a.data_type) for a in mesh.color_attributes])
