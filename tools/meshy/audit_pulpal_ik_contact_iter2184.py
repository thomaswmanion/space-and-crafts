import bpy, csv, os
from mathutils import Vector

blend = r'E:\Users\thoma\git\space-and-crafts-3\Logs\score-comparison\pulpal-meshy-animation-review-iter2180\authored-ik-trial-iter2184\Pulpal_LeafLanePress_IK_Iter2184.blend'
out_dir = os.path.dirname(blend)
summary_path = os.path.join(out_dir, 'CONTACT_AUDIT.csv')
bpy.ops.wm.open_mainfile(filepath=blend)
scene = bpy.context.scene
arm = next(o for o in scene.objects if o.type == 'ARMATURE')
mesh = next(o for o in scene.objects if o.type == 'MESH')
plane_z = 0.0106  # review-only idle foot-proxy plane from Iter2180
groups = {g.name: g.index for g in mesh.vertex_groups}
foot_indices = {groups[n] for n in ('LeftFoot', 'LeftToeBase', 'RightFoot', 'RightToeBase') if n in groups}
hand_indices = {groups[n] for n in ('LeftHand', 'RightHand') if n in groups}

def weighted_min_z(vertices, indices, world_matrix):
    z_values = []
    for v in vertices:
        if sum(g.weight for g in v.groups if g.group in indices) >= .25:
            z_values.append((world_matrix @ v.co).z)
    return min(z_values) if z_values else float('nan')

def weighted_z_range(vertices, indices, world_matrix):
    z_values = []
    for v in vertices:
        if sum(g.weight for g in v.groups if g.group in indices) >= .25:
            z_values.append((world_matrix @ v.co).z)
    return (min(z_values), max(z_values)) if z_values else (float('nan'), float('nan'))

with open(summary_path, 'w', newline='', encoding='utf-8') as f:
    w = csv.writer(f)
    w.writerow(('frame','root_z','left_foot_bone_tail_z','right_foot_bone_tail_z','weighted_foot_vertex_min_z','foot_min_minus_reference_plane','weighted_hand_vertex_min_z','weighted_hand_vertex_max_z','hand_min_vertex_minus_leaf_lane_top','left_hand_bone_tail_z','right_hand_bone_tail_z','hand_min_tail_minus_reference_plane'))
    for frame in range(scene.frame_start, scene.frame_end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        ev_mesh = ev.to_mesh()
        verts = [v for v in ev_mesh.vertices]
        def tail_z(name):
            return (arm.matrix_world @ arm.pose.bones[name].tail).z
        foot_min = weighted_min_z(verts, foot_indices, ev.matrix_world)
        hand_min, hand_max = weighted_z_range(verts, hand_indices, ev.matrix_world)
        left_hand = tail_z('LeftHand')
        right_hand = tail_z('RightHand')
        w.writerow((frame, arm.location.z, tail_z('LeftFoot'), tail_z('RightFoot'), foot_min, foot_min-plane_z, hand_min, hand_max, hand_min-.12, left_hand, right_hand, min(left_hand,right_hand)-plane_z))
        ev.to_mesh_clear()

rows=[]
with open(summary_path, newline='', encoding='utf-8') as f:
    rows=list(csv.DictReader(f))
print('CONTACT_AUDIT', summary_path, 'frames=',len(rows), 'plane_z=',plane_z)
for frame in (1,14,24,31,38,48,62,90):
    r=rows[frame-1]
    print('SAMPLE',frame,'foot_delta=',round(float(r['foot_min_minus_reference_plane']),4),'hand_vertex_min_minus_lane_top=',round(float(r['hand_min_vertex_minus_leaf_lane_top']),4),'hand_bone_tail_minus_ground=',round(float(r['hand_min_tail_minus_reference_plane']),4))
foot=[float(r['foot_min_minus_reference_plane']) for r in rows]
print('FOOT_VERTEX_DELTA_RANGE',round(min(foot),4),round(max(foot),4),'m')
