import bpy, os
from mathutils import Vector
ROOT=os.path.abspath("Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown")
BLEND=os.path.join(ROOT,"20261101_rainbow-spider-rig-candidate-iter1407","RainbowSpider_RigCandidate.blend")
OUT=os.path.abspath("Logs/meshy-spider-angle-review-iter1408")
os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=BLEND)
scene=bpy.context.scene
cam=next(o for o in scene.objects if o.type=="CAMERA")
arm=next(o for o in scene.objects if o.type=="ARMATURE")
arm.animation_data.action=bpy.data.actions.get("Spider_Idle")
scene.frame_set(16)
scene.render.resolution_x=700; scene.render.resolution_y=700; scene.render.resolution_percentage=100
for elevation in (0.45,1.4,2.4,3.2):
    cam.location=(0.0,elevation,4.5)
    cam.rotation_euler=(Vector((0.0,-0.08,0.0))-cam.location).to_track_quat("-Z","Y").to_euler()
    for light in [o for o in scene.objects if o.type=="LIGHT"]:
        light.rotation_euler=(Vector((0.0,0.0,0.0))-light.location).to_track_quat("-Z","Y").to_euler()
    scene.camera=cam
    scene.render.filepath=os.path.join(OUT,f"idle_elevation_{elevation:.1f}.png")
    bpy.ops.render.render(write_still=True)
print("ANGLE_REVIEW_DONE",OUT)
