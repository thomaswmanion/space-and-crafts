import bpy
import json
import math
import os
from mathutils import Vector, Matrix
from mathutils.kdtree import KDTree

ROOT = r'E:\Users\thoma\git\space-and-crafts-3'
SOURCE_FBX = ROOT + r'\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\20261030_224550_splat-iter1337_01a1900f\FBX\walking_fbx.fbx'
REMESH_FBX = ROOT + r'\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\Splat_300k_Remesh_Iter1365.fbx'
TEXTURE_DIR = ROOT + r'\Assets\SpaceAndCrafts\Art\Characters\Splat\MeshyWork\Splat_300k_Remesh_Iter1365_textures'
OUT_DIR = ROOT + r'\Assets\SpaceAndCrafts\Art\Characters\Splat\AnimationCandidates\Iter1370'
os.makedirs(OUT_DIR, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SOURCE_FBX, automatic_bone_orientation=False)
armature = next(obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE')
source = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH' and len(obj.vertex_groups) > 0)
walk_action = armature.animation_data.action if armature.animation_data else None
source_mesh_obj_name = source.name
source_matrix_world = source.matrix_world.copy()
source_inverse_world = source_matrix_world.inverted()
source_triangles = sum(max(0,len(poly.vertices)-2) for poly in source.data.polygons)
source_world = [source.matrix_world @ v.co for v in source.data.vertices]
source_tree = KDTree(len(source_world))
for index, point in enumerate(source_world):
    source_tree.insert(point, index)
source_tree.balance()
source_min = Vector(tuple(min(point[i] for point in source_world) for i in range(3)))
source_max = Vector(tuple(max(point[i] for point in source_world) for i in range(3)))
source_dims = source_max - source_min

bpy.ops.import_scene.fbx(filepath=REMESH_FBX, automatic_bone_orientation=False)
candidate = next(obj for obj in bpy.context.scene.objects
                 if obj.type == 'MESH' and obj != source and len(obj.vertex_groups) == 0)
candidate_world = [candidate.matrix_world @ v.co for v in candidate.data.vertices]
candidate_min = Vector(tuple(min(point[i] for point in candidate_world) for i in range(3)))
candidate_max = Vector(tuple(max(point[i] for point in candidate_world) for i in range(3)))
candidate_dims = candidate_max - candidate_min
if min(candidate_dims) <= 0 or min(source_dims) <= 0:
    raise RuntimeError('Invalid character mesh bounds; refusing weight mapping.')
scale = source_dims.z / candidate_dims.z
if max(abs(source_dims[i] / candidate_dims[i] - scale) for i in range(3)) > 0.003:
    raise RuntimeError('Uniform-scale bounds do not align; refusing weight mapping.')
translation = (source_min + source_max - (candidate_min + candidate_max) * scale) * 0.5
aligned = [point * scale + translation for point in candidate_world]

# Transfer the existing Meshy skin weights to each remesh vertex through its
# closest source vertex. The surfaces share the same upright frame and nearly
# coincide after a single uniform scale and ground-centering transform.
group_names = [group.name for group in source.vertex_groups]
new_groups = [candidate.vertex_groups.new(name=name) for name in group_names]
source_groups_by_index = {index: group.name for index, group in enumerate(source.vertex_groups)}
assignments = {name: {} for name in group_names}
weight_counts = [0 for _ in group_names]
nearest_distances = []
unweighted_target_vertices = 0
for target_index, point in enumerate(aligned):
    _, source_index, distance = source_tree.find(point)
    nearest_distances.append(distance)
    source_vertex = source.data.vertices[source_index]
    if not source_vertex.groups:
        unweighted_target_vertices += 1
        continue
    for assignment in source_vertex.groups:
        group_name = source_groups_by_index.get(assignment.group)
        if group_name is None or assignment.weight <= 0.0001:
            continue
        # VertexGroup.add accepts one shared weight per batch. Group near-
        # identical weights into 8-bit bins; max error is below 0.002.
        bucket = max(1, min(255, int(round(assignment.weight * 255.0))))
        assignments[group_name].setdefault(bucket, []).append(target_index)
        weight_counts[assignment.group] += 1
    if not source_vertex.groups:
        unweighted_target_vertices += 1

for name, group in zip(group_names, new_groups):
    for bucket, indices in assignments[name].items():
        group.add(indices, bucket / 255.0, 'REPLACE')

# Reuse the source texture and UVs carried by the remesh.
base_color_path = os.path.join(TEXTURE_DIR, 'base_color.png')
if not os.path.exists(base_color_path):
    raise RuntimeError('Remesh base-color texture is missing; refusing to export.')
material = candidate.data.materials[0] if candidate.data.materials else bpy.data.materials.new('Splat_Iter1365_BaseColor')
material.name = 'Splat_Iter1365_300k_SkinnedCandidate'
material.use_nodes = True
nodes = material.node_tree.nodes
links = material.node_tree.links
principled = next((node for node in nodes if node.type == 'BSDF_PRINCIPLED'), None)
if principled is None:
    principled = nodes.new('ShaderNodeBsdfPrincipled')
output = next((node for node in nodes if node.type == 'OUTPUT_MATERIAL'), None)
if output is None:
    output = nodes.new('ShaderNodeOutputMaterial')
if not principled.outputs['BSDF'].is_linked:
    links.new(principled.outputs['BSDF'], output.inputs['Surface'])
tex = nodes.new('ShaderNodeTexImage')
tex.name = 'Splat_Iter1365_BaseColor'
tex.image = bpy.data.images.load(base_color_path, check_existing=True)
tex.image.colorspace_settings.name = 'sRGB'
links.new(tex.outputs['Color'], principled.inputs['Base Color'])
principled.inputs['Roughness'].default_value = 0.42
material.diffuse_color = (1.0, 1.0, 1.0, 1.0)
candidate.data.materials.clear()
candidate.data.materials.append(material)

# Bake the fitted world-space coordinates into the mesh, then bind it to the
# existing full-body skeleton. The new model remains a distinct candidate file.
for vertex, point in zip(candidate.data.vertices, aligned):
    # The source FBX's mesh object carries the Y-to-Z axis rotation. Keep that
    # same object basis while placing the target at the source's world surface;
    # otherwise the unchanged bone weights are interpreted in a different
    # mesh-local coordinate frame and visibly shear the body.
    vertex.co = source_inverse_world @ point
candidate.data.update()
candidate.name = 'ART_SplatBody_Iter1365_RigBound_Iter1370'
candidate.parent = None
candidate.matrix_world = source_matrix_world
modifier = candidate.modifiers.new('Splat_Meshy_SkinRig_Iter1370', 'ARMATURE')
modifier.object = armature
modifier.use_vertex_groups = True
modifier.use_deform_preserve_volume = False

# Preserve only the candidate mesh and its compatible armature/action; drop the
# original source mesh to avoid double rendering.
bpy.data.objects.remove(source, do_unlink=True)
for obj in list(bpy.context.scene.objects):
    if obj != candidate and obj != armature and obj.type not in {'CAMERA', 'LIGHT'}:
        bpy.data.objects.remove(obj, do_unlink=True)

scene = bpy.context.scene
scene.frame_start = int(walk_action.frame_range[0]) if walk_action else 1
scene.frame_end = int(walk_action.frame_range[1]) if walk_action else 1
if walk_action:
    armature.animation_data_create()
    armature.animation_data.action = walk_action

# Add a simple neutral support plane and studio lights for a review render.
plane_mesh = bpy.data.meshes.new('ReviewSupportPlaneMesh')
plane_mesh.from_pydata([(-5,-5,0),(5,-5,0),(5,5,0),(-5,5,0)], [], [(0,1,2,3)])
plane_mesh.update()
plane = bpy.data.objects.new('ReviewSupportPlane', plane_mesh)
scene.collection.objects.link(plane)
plane_mat = bpy.data.materials.new('Review_Matte_Gray')
plane_mat.diffuse_color = (0.22,0.24,0.28,1)
plane_mat.use_nodes = True
plane_mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(0.22,0.24,0.28,1)
plane_mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=0.85
plane.data.materials.append(plane_mat)

world=scene.world or bpy.data.worlds.new('SplatCandidateReviewWorld')
scene.world=world
world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.42,0.47,0.56,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=0.5

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def add_area(name, location, energy, size):
    data=bpy.data.lights.new(name, 'AREA')
    data.energy=energy; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj)
    obj.location=location; aim(obj,(0,0,0.9)); return obj
add_area('Key', (2.5,-3.2,3.8), 450, 3.0)
add_area('Fill', (-3.5,-1.5,2.2), 260, 3.4)
add_area('Rim', (1.0,2.8,3.0), 520, 2.5)
cam_data=bpy.data.cameras.new('SplatCandidateReviewCamera'); cam=bpy.data.objects.new('SplatCandidateReviewCamera',cam_data); scene.collection.objects.link(cam)
cam.location=(0,-4.5,1.22); aim(cam,(0,0,0.9)); cam_data.lens=55; scene.camera=cam
scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=1024; scene.render.resolution_y=1024; scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard'; scene.view_settings.look='Medium High Contrast'; scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False

# Diagnostics: nearest source-vertex weight mapping quality and candidate bounds.
def evaluated_bounds(frame):
    scene.frame_set(frame)
    deps=bpy.context.evaluated_depsgraph_get(); ev=candidate.evaluated_get(deps); mesh=ev.to_mesh()
    pts=[candidate.matrix_world@v.co for v in mesh.vertices]
    result={'min':[min(p[i] for p in pts) for i in range(3)],'max':[max(p[i] for p in pts) for i in range(3)]}
    ev.to_mesh_clear(); return result
nearest_sorted=sorted(nearest_distances)
def pct(q): return nearest_sorted[round((len(nearest_sorted)-1)*q)]
report={
 'iteration':1370,
 'source_mesh':source_mesh_obj_name,
 'target_mesh':candidate.name,
 'source_triangles':source_triangles,
 'target_triangles':sum(max(0,len(p.vertices)-2) for p in candidate.data.polygons),
 'fit_uniform_scale':scale,
 'fit_translation':[float(x) for x in translation],
 'source_world_bounds':{'min':[float(x) for x in source_min],'max':[float(x) for x in source_max]},
 'target_bounds_after_fit':{'min':[float(min(p[i] for p in aligned)) for i in range(3)],'max':[float(max(p[i] for p in aligned)) for i in range(3)]},
 'nearest_source_vertex_distance_m':{'median':pct(.5),'p90':pct(.9),'p99':pct(.99),'max':nearest_sorted[-1]},
 'target_vertices_without_source_weights':unweighted_target_vertices,
 'vertex_group_assignment_counts':dict(zip(group_names,weight_counts)),
 'walk_action':walk_action.name if walk_action else None,
 'walk_action_frames':list(walk_action.frame_range) if walk_action else None,
}
if armature.animation_data:
    armature.animation_data.action = None
armature.data.pose_position = 'REST'
report['rest_pose_bounds']=evaluated_bounds(scene.frame_start)
armature.data.pose_position = 'POSE'
if walk_action:
    armature.animation_data_create()
    armature.animation_data.action = walk_action
if walk_action and scene.frame_end > scene.frame_start:
    report['walk_pose_bounds']=evaluated_bounds(scene.frame_start + max(1,int((scene.frame_end-scene.frame_start)*0.25)))
report_path=os.path.join(OUT_DIR,'Splat_Iter1370_RigBindingReport.json')
with open(report_path,'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
print('ITER1370_WEIGHT_TRANSFER_REPORT',json.dumps(report,indent=2))

# Render one neutral and one walk sample (isolated Blender preview, not game play).
armature.animation_data.action = None
armature.data.pose_position = 'REST'
scene.frame_set(scene.frame_start)
scene.render.filepath=os.path.join(OUT_DIR,'Splat_Iter1370_bind.png')
bpy.ops.render.render(write_still=True)
armature.data.pose_position = 'POSE'
if walk_action:
    armature.animation_data_create()
    armature.animation_data.action = walk_action
for suffix, frame in [('walk25',scene.frame_start+max(1,int((scene.frame_end-scene.frame_start)*0.25)))]:
    scene.frame_set(min(frame,scene.frame_end))
    scene.render.filepath=os.path.join(OUT_DIR,f'Splat_Iter1370_{suffix}.png')
    bpy.ops.render.render(write_still=True)

blend_path=os.path.join(OUT_DIR,'Splat_Meshy_Iter1365_RigBound_Iter1370.blend')
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
# Export only the candidate and existing armature, with the active walk action.
bpy.ops.object.select_all(action='DESELECT'); candidate.select_set(True); armature.select_set(True); bpy.context.view_layer.objects.active=armature
fbx_path=os.path.join(OUT_DIR,'Splat_Meshy_Iter1365_RigBound_Iter1370.fbx')
bpy.ops.export_scene.fbx(filepath=fbx_path,use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
print('ITER1370_OUTPUT',blend_path,fbx_path,report_path)
