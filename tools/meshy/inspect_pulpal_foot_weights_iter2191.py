import bpy

blend=r'E:\Users\thoma\git\space-and-crafts-3\Logs\score-comparison\pulpal-meshy-animation-review-iter2180\authored-ik-trial-iter2189\Pulpal_LeafLanePress_IK_Iter2189.blend'
bpy.ops.wm.open_mainfile(filepath=blend)
scene=bpy.context.scene
mesh=next(o for o in scene.objects if o.type=='MESH')
foot_group_names={'LeftFoot','LeftToeBase','RightFoot','RightToeBase'}
for frame in (1,31,48):
    scene.frame_set(frame);bpy.context.view_layer.update()
    ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    em=ev.to_mesh()
    rows=[]
    for v in em.vertices:
        foot_weight=sum(g.weight for g in v.groups if mesh.vertex_groups[g.group].name in foot_group_names)
        if foot_weight>=.25:
            p=ev.matrix_world@v.co
            weights=sorted(((mesh.vertex_groups[g.group].name,round(g.weight,3)) for g in v.groups),key=lambda x:-x[1])
            rows.append((p.z,v.index,foot_weight,p,weights))
    rows.sort(key=lambda x:x[0])
    print('FRAME',frame)
    for z,i,w,p,weights in rows[:8]:print('BOTTOM',i,'z',round(z,4),'footw',round(w,3),'world',tuple(round(x,3) for x in p),'weights',weights)
    ev.to_mesh_clear()
