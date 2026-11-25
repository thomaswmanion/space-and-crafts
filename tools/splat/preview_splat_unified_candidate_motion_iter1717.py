"""Render selected poses from the locally authored Splat candidate actions."""
import bpy
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
BLEND=ROOT/'docs/art/splat-unified-candidate-iter1717/Splat_UnifiedRig_Candidate_Iter1717.blend'
OUT=ROOT/'Logs/score-comparison/splat-unified-candidate-iter1717'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BLEND))
scene=bpy.context.scene
scene.render.resolution_x=768
scene.render.resolution_y=768
scene.render.resolution_percentage=100
scene.cycles.samples=20
scene.render.image_settings.file_format='PNG'
rig=bpy.data.objects.get('Splat_UnifiedRig')
if rig is None or rig.animation_data is None:
    raise RuntimeError('Splat armature/actions were not saved')
samples=[('idle','Splat_Idle',61,0.0),
         ('run-left-stance','Splat_Run',1,0.0),
         ('run-right-stance','Splat_Run',13,0.0),
         ('jump-anticipation','Splat_Jump',1,0.0),
         ('jump-apex','Splat_Jump',22,0.24),
         ('jump-descent','Splat_Jump',34,0.10),
         ('land-impact','Splat_Land',6,0.0),
         ('land-recovery','Splat_Land',12,0.0),
         ('land-settle','Splat_Land',24,0.0)]
for label, action_name, frame, lift in samples:
    action=bpy.data.actions.get(action_name)
    if action is None:
        raise RuntimeError(f'Missing {action_name} action')
    rig.animation_data.action=action
    rig.location.z=lift
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    scene.render.filepath=str(OUT/f'{label}.png')
    bpy.ops.render.render(write_still=True)
print('SPLAT_ACTION_PREVIEWS_OK '+', '.join(label for label,_,_,_ in samples))


