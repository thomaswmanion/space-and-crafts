from pathlib import Path
from mathutils import Vector
import bpy, math

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter2000_Meshy71/Splat_Iter2000_Remeshed_Rigged.glb'
OUT_DIR = ROOT / 'Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter2000_Meshy71/Review/Iter2246_BodyCore'
OUT = OUT_DIR / 'Splat_Iter2000_BodyCore_Rigged_Iter2246.glb'
OUT_FBX = OUT_DIR / 'Splat_Iter2000_BodyCore_Rigged_Iter2246.fbx'
REPORT = ROOT / 'Logs/splat-iter2000-bodycore-authoring-iter2246.log'
CENTER = Vector((0.0, 0.0, 0.415))
RADII = Vector((0.232, 0.245, 0.245))
INNER, OUTER = 0.72, 1.08
LIMB_LOCK = {'LeftUpLeg','RightUpLeg','LeftLeg','RightLeg','LeftFoot','RightFoot','LeftToeBase','RightToeBase','LeftHand','RightHand','LeftForeArm','RightForeArm'}

def smoothstep(x):
    x=max(0.0,min(1.0,x)); return x*x*(3.0-2.0*x)

def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE))
    scene=bpy.context.scene
    mesh=next(o for o in scene.objects if o.type=='MESH' and o.name=='char1')
    arm=next(o for o in scene.objects if o.type=='ARMATURE')
    arm.data.pose_position='REST'
    if arm.animation_data: arm.animation_data_clear()
    # This is a confirmed unweighted helper sphere, not part of Splat's render mesh.
    helpers=[o for o in scene.objects if o.type=='MESH' and o is not mesh]
    helper_report=[]
    for helper in helpers:
        helper_report.append((helper.name, len(helper.data.vertices), tuple(round(v,4) for v in helper.dimensions), len(helper.vertex_groups), len(helper.data.materials)))
        bpy.data.objects.remove(helper, do_unlink=True)
    if mesh.find_armature() is None:
        raise RuntimeError('Source skinned mesh lacks its armature modifier')
    hips=arm.data.bones.get('Hips')
    if hips is None: raise RuntimeError('Rig has no Hips edit bone')
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.objects.active=arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    hips=arm.data.edit_bones.get('Hips')
    if arm.data.edit_bones.get('BodyCore') is not None: raise RuntimeError('BodyCore already exists')
    core=arm.data.edit_bones.new('BodyCore')
    inv=arm.matrix_world.inverted()
    core.head=inv @ CENTER
    core.tail=inv @ (CENTER + Vector((0.0,0.0,0.08)))
    core.parent=hips
    core.use_connect=False
    core.use_deform=True
    bpy.ops.object.mode_set(mode='OBJECT')
    groups={g.name:g for g in mesh.vertex_groups}
    body=groups.get('BodyCore') or mesh.vertex_groups.new(name='BodyCore')
    by_index={g.index:g for g in mesh.vertex_groups}
    changed=0; rigid=0; blended=0; skipped_limbs=0; affected=[]
    for v in mesh.data.vertices:
        p=mesh.matrix_world @ v.co
        q=Vector(((p.x-CENTER.x)/RADII.x,(p.y-CENTER.y)/RADII.y,(p.z-CENTER.z)/RADII.z))
        r=q.length
        if r>=OUTER: continue
        existing={by_index[i.group].name:i.weight for i in v.groups if i.group in by_index and i.weight>0.001}
        if not existing: continue
        dominant=max(existing, key=existing.get)
        if dominant in LIMB_LOCK and r>0.55:
            skipped_limbs+=1; continue
        core_w=1.0 if r<=INNER else 1.0-smoothstep((r-INNER)/(OUTER-INNER))
        if core_w<=0: continue
        remapped={name:weight*(1.0-core_w) for name,weight in existing.items() if name!='BodyCore'}
        remapped['BodyCore']=core_w
        remapped={name:weight for name,weight in remapped.items() if weight>0.001}
        # Unity's default four-influence skin budget: keep BodyCore and the strongest three other weights.
        core_value=remapped.pop('BodyCore',0.0)
        keep=sorted(remapped.items(),key=lambda item:item[1],reverse=True)[:3]
        total=core_value+sum(weight for _,weight in keep)
        if total<=1e-6: continue
        normalized={'BodyCore':core_value/total}
        normalized.update({name:weight/total for name,weight in keep})
        for inf in list(v.groups):
            group=by_index.get(inf.group)
            if group is not None: group.remove([v.index])
        for name,weight in normalized.items():
            if weight>0.001: groups.get(name,body).add([v.index],weight,'REPLACE')
        changed+=1
        if core_value>=0.999: rigid+=1
        else: blended+=1
        affected.append(p)
    if changed<1000: raise RuntimeError(f'Too few body vertices were remapped: {changed}')
    OUT_DIR.mkdir(parents=True,exist_ok=True)
    scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT')
    mesh.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.gltf(filepath=str(OUT),export_format='GLB',use_selection=True,export_animations=False,export_skins=True,export_apply=False,export_image_format='AUTO')
    bpy.ops.export_scene.fbx(filepath=str(OUT_FBX),use_selection=True,object_types={'ARMATURE','MESH'},bake_anim=False,add_leaf_bones=False,path_mode='COPY',embed_textures=True)
    minp=tuple(round(min(p[i] for p in affected),4) for i in range(3)); maxp=tuple(round(max(p[i] for p in affected),4) for i in range(3))
    REPORT.parent.mkdir(parents=True,exist_ok=True)
    REPORT.write_text('\n'.join([
        'Splat Iter2000 review-only body-core weight candidate.',
        f'Source: {SOURCE}', f'Output GLB: {OUT}', f'Output FBX: {OUT_FBX}',
        f'Removed unweighted helper meshes: {helper_report}',
        f'Added deform bone BodyCore under Hips, centered at {tuple(CENTER)} in world/rest pose.',
        f'Body ellipsoid radii={tuple(RADII)}; rigid inner radius={INNER}; blended edge ends at {OUTER}.',
        f'Remapped vertices={changed}; rigid BodyCore={rigid}; blended={blended}; preserved limb vertices={skipped_limbs}.',
        f'Remapped world bounds min={minp}, max={maxp}.',
        'Geometry/material source surfaces are retained; weights only are changed. No production asset or scene is modified.',
        'This is an authored review candidate. Animation deformation, contact, runtime import, gameplay, and human play remain unverified.'
    ])+'\n',encoding='utf-8')
    print(REPORT.read_text())

if __name__=='__main__': main()
