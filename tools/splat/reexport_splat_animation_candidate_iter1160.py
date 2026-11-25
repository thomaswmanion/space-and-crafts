import bpy, os
project=r'E:\Users\thoma\git\space-and-crafts-3'
blend=os.path.join(project,'Assets','SpaceAndCrafts','Art','Animations','Authoring','Iter1160','Splat_Animations_Iter1160.blend')
outdir=os.path.join(project,'Assets','SpaceAndCrafts','Art','Animations','Authoring','Iter1160')
bpy.ops.wm.open_mainfile(filepath=blend)
scene=bpy.context.scene
armature=bpy.data.objects.get('Splat_SkinRig')
if armature is None or armature.type!='ARMATURE': raise RuntimeError('Iter1160 armature missing')
for state in ('Idle','Run','Jump','Land'):
    name=f'Splat_{state}_Iter1160'; action=bpy.data.actions.get(name)
    if action is None: raise RuntimeError(f'Missing action {name}')
    scene.frame_start=int(action.frame_range[0]); scene.frame_end=int(action.frame_range[1]); scene.frame_set(scene.frame_start)
    armature.animation_data_create(); armature.animation_data.action=action
    bpy.ops.object.select_all(action='DESELECT'); armature.select_set(True); bpy.context.view_layer.objects.active=armature
    filepath=os.path.join(outdir,name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=filepath,use_selection=True,object_types={'ARMATURE'},bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0.0,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='AUTO')
    print('[Iter1160 re-export]',state,'frames',scene.frame_start,scene.frame_end,'length',(scene.frame_end-scene.frame_start)/scene.render.fps,'path',filepath)
