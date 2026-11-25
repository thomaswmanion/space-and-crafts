import bpy, bmesh, json, os
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=r'E:\Users\thoma\git\space-and-crafts-3'
CANDIDATE_BLEND=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1370\Splat_Meshy_Iter1365_RigBound_Iter1370.blend'
SOURCE_FBX=ROOT+r'\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\20261030_224550_splat-iter1337_01a1900f\FBX\walking_fbx.fbx'
OUT=ROOT+r'\Logs\splat-remesh-walk-weight-compare-iter1371.json'
bpy.ops.wm.open_mainfile(filepath=CANDIDATE_BLEND)
candidate=next(o for o in bpy.context.scene.objects if o.type=='MESH' and 'RigBound' in o.name)
scene=bpy.context.scene
frame=9
scene.frame_set(frame)
def evaluated_surface(obj):
 deps=bpy.context.evaluated_depsgraph_get(); ev=obj.evaluated_get(deps); mesh=ev.to_mesh(); mesh.calc_loop_triangles(); points=[obj.matrix_world@v.co for v in mesh.vertices]; bm=bmesh.new();bm.from_mesh(mesh);bm.transform(obj.matrix_world);bm.normal_update(); tree=BVHTree.FromBMesh(bm); return ev,mesh,bm,points,tree
cev,cm,cbm,cpts,ctree=evaluated_surface(candidate)
bpy.ops.import_scene.fbx(filepath=SOURCE_FBX,automatic_bone_orientation=False)
source=next(o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.vertex_groups)>0)
scene.frame_set(frame)
sev,sm,sbm,spts,stree=evaluated_surface(source)
def pct(values,q): return sorted(values)[round((len(values)-1)*q)]
stepc=max(1,len(cpts)//5000); steps=max(1,len(spts)//5000)
c2s=[stree.find_nearest(p)[3] for p in cpts[::stepc]]
s2c=[ctree.find_nearest(p)[3] for p in spts[::steps]]
report={'iteration':1371,'frame':frame,'candidate_sample_count':len(c2s),'source_sample_count':len(s2c),'candidate_to_source_m':{k:pct(c2s,q) for k,q in [('median',.5),('p90',.9),('p99',.99)]},'source_to_candidate_m':{k:pct(s2c,q) for k,q in [('median',.5),('p90',.9),('p99',.99)]},'candidate_min_world':[min(p[i] for p in cpts) for i in range(3)],'candidate_max_world':[max(p[i] for p in cpts) for i in range(3)],'source_min_world':[min(p[i] for p in spts) for i in range(3)],'source_max_world':[max(p[i] for p in spts) for i in range(3)],'note':'Offline Blender evaluated-surface comparison of the remesh with weights transferred from the matched Meshy skin. This is not a Unity runtime or human-play test.'}
with open(OUT,'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print('ITER1371',json.dumps(report,indent=2))
sev.to_mesh_clear(); cev.to_mesh_clear();sbm.free();cbm.free()
