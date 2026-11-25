"""Author/render a review-only Ground Slam on Pomperstein's existing rig."""
import bpy, csv, math, os
from mathutils import Euler, Vector

ROOT = r"E:\Users\thoma\git\space-and-crafts-3"
ASSET = os.path.join(ROOT, "Assets/SpaceAndCrafts/Art/Enemies/Pomperstein/Meshy_Iter1997")
OUT = os.path.join(ROOT, "Logs/score-comparison/pomperstein-authored-ground-slam-iter2201")
SRC = os.path.join(ASSET, "Pomperstein_Rigged.fbx")
FBX = os.path.join(OUT, "Pomperstein_GroundSlam_Authored_Iter2201.fbx")
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC, use_anim=False)
scene = bpy.context.scene
scene.render.fps = 30
scene.frame_start, scene.frame_end = 1, 72
arm = next(o for o in scene.objects if o.type == "ARMATURE")
meshes = [o for o in scene.objects if o.type == "MESH" and o.find_armature() == arm]
if not meshes:
    raise RuntimeError("No Pomperstein skinned mesh imported")
mesh = max(meshes, key=lambda o: len(o.data.vertices))
required = ("Hips", "Spine", "Spine01", "Spine02", "LeftArm", "RightArm", "LeftForeArm", "RightForeArm")
if any(arm.pose.bones.get(name) is None for name in required):
    raise RuntimeError("Required Pomperstein torso/arm bones missing")

scene.frame_set(1)
bpy.context.view_layer.update()
rest = {pb.name: (pb.location.copy(), pb.rotation_quaternion.copy() if pb.rotation_mode == "QUATERNION" else pb.rotation_euler.to_quaternion(), pb.scale.copy()) for pb in arm.pose.bones}
for pb in arm.pose.bones:
    pb.rotation_mode = "QUATERNION"
anim = arm.animation_data_create()
if anim.action is not None and len(anim.action.slots):
    action = anim.action.copy()
else:
    action = bpy.data.actions.new("Pomperstein_Authored_GroundSlam_Iter2201")
    action.slots.new(id_type="OBJECT", name=arm.name)
action.name = "Pomperstein_Authored_GroundSlam_Iter2201"
action.use_fake_user = True
anim.action = action
anim.action_slot = action.slots[0]

def set_pose(frame, squat=0, pitch=0.0, arm_raise=0.0, elbow=0.0, hop=0.0, twist=0.0):
    scene.frame_set(frame)
    for pb in arm.pose.bones:
        loc, quat, scale = rest[pb.name]
        pb.location, pb.rotation_quaternion, pb.scale = loc, quat, scale
    hips = arm.pose.bones["Hips"]
    # Hips local Y is nearly vertical; armature object scale is 0.01.
    hips.location.y -= squat
    hips.location.y += hop
    hips.rotation_quaternion = rest["Hips"][1] @ Euler((pitch, 0.0, twist), "XYZ").to_quaternion()
    for name, factor in (("Spine", .46), ("Spine01", .32), ("Spine02", .22)):
        pb = arm.pose.bones[name]
        pb.rotation_quaternion = rest[name][1] @ Euler((pitch * factor, 0.0, twist * factor), "XYZ").to_quaternion()
    for name, sign in (("LeftArm", 1.0), ("RightArm", -1.0)):
        pb = arm.pose.bones[name]
        pb.rotation_quaternion = rest[name][1] @ Euler((0.0, 0.0, sign * arm_raise), "XYZ").to_quaternion()
    for name, sign in (("LeftForeArm", 1.0), ("RightForeArm", -1.0)):
        pb = arm.pose.bones[name]
        pb.rotation_quaternion = rest[name][1] @ Euler((0.0, 0.0, sign * elbow), "XYZ").to_quaternion()
    for pb in arm.pose.bones:
        pb.keyframe_insert(data_path="location", frame=frame, group=pb.name)
        pb.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=pb.name)
        pb.keyframe_insert(data_path="scale", frame=frame, group=pb.name)

# 2.4-second wind-up, compact hop, two-arm impact, brace, and clear recovery.
keys = [
    (1,0,0.00,0.00,0.00,0,0), (8,2,-.04,.45,.30,0,-.04),
    (15,8,-.12,1.05,.62,0,-.08), (19,5,-.04,.85,.45,5,-.04),
    (22,0,.12,-.12,.22,-2,0), (25,7,.22,-.65,-.25,-2,.02),
    (31,5,.16,-.42,-.12,0,.01), (40,1,.06,.08,.16,0,0),
    (52,0,0,0,0,0,0), (72,0,0,0,0,0,0),
]
for args in keys:
    set_pose(*args)
channelbag = action.layers[0].strips[0].channelbags[0]
for fc in channelbag.fcurves:
    for key in fc.keyframe_points:
        key.interpolation = "BEZIER"
        key.handle_left_type = "AUTO_CLAMPED"
        key.handle_right_type = "AUTO_CLAMPED"

groups = {g.name:g.index for g in mesh.vertex_groups}
foot_ids = {groups[n] for n in ("LeftFoot","LeftToeBase","RightFoot","RightToeBase") if n in groups}
def frame_metrics(frame):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    em = ev.to_mesh()
    try:
        z = [(ev.matrix_world @ v.co).z for v in em.vertices]
        foot = [z[v.index] for v in em.vertices if sum(g.weight for g in mesh.data.vertices[v.index].groups if g.group in foot_ids) >= .25]
        return min(z), min(foot) if foot else float("nan"), max(z)
    finally:
        ev.to_mesh_clear()
base_floor = frame_metrics(1)[1]
if math.isnan(base_floor):
    raise RuntimeError("No foot/toe weighted vertices for grounded reference")
with open(os.path.join(OUT,"CONTACT_AUDIT.csv"),"w",newline="",encoding="utf-8") as f:
    writer=csv.writer(f); writer.writerow(("frame","body_min_z","weighted_foot_min_z","foot_delta_vs_idle","body_min_delta_vs_idle_floor","body_max_z"))
    for frame in range(1,73):
        body, foot, top = frame_metrics(frame)
        writer.writerow((frame,body,foot,foot-base_floor if not math.isnan(foot) else "",body-base_floor,top))

# Neutral, consistently lit staged renders; no game materials are changed.
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x=560; scene.render.resolution_y=560; scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG"
scene.world=scene.world or bpy.data.worlds.new("PompersteinReviewWorld_Iter2201")
scene.world.color=(.08,.08,.08)
floor_mesh=bpy.data.meshes.new("NeutralFloorMesh_Iter2201")
floor_mesh.from_pydata([(-10,-10,base_floor),(10,-10,base_floor),(10,10,base_floor),(-10,10,base_floor)],[],[(0,1,2,3)])
floor=bpy.data.objects.new("NeutralFloor_Iter2201",floor_mesh); scene.collection.objects.link(floor)
floor_mat=bpy.data.materials.new("NeutralFloorMat_Iter2201"); floor_mat.diffuse_color=(.66,.70,.76,1); floor_mesh.materials.append(floor_mat)
def point(obj,target): obj.rotation_euler=(Vector(target)-obj.location).to_track_quat("-Z","Y").to_euler()
cam_data=bpy.data.cameras.new("PompersteinReviewCamera_Iter2201"); cam=bpy.data.objects.new("PompersteinReviewCamera_Iter2201",cam_data); scene.collection.objects.link(cam)
cam.location=(1.35,-5.6,2.2); cam.data.type="ORTHO"; cam.data.ortho_scale=2.8; point(cam,(0,0,.95)); scene.camera=cam
for name,loc,energy,size in (("Key",(3,-4,4),950,4),("Fill",(-3,-2,2),550,4),("Rim",(0,4,3),700,3)):
    data=bpy.data.lights.new(name+"_Iter2201","AREA"); obj=bpy.data.objects.new(name+"_Iter2201",data); scene.collection.objects.link(obj)
    obj.location=loc; data.energy=energy; data.shape="DISK"; data.size=size; point(obj,(0,0,.9))
for frame in (1,8,15,19,22,25,31,40,52):
    scene.frame_set(frame); bpy.context.view_layer.update(); scene.render.filepath=os.path.join(OUT,f"pose_{frame:02d}.png"); bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,"Pomperstein_GroundSlam_Authored_Iter2201.blend"))
bpy.ops.object.select_all(action="DESELECT"); arm.select_set(True); mesh.select_set(True); bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=FBX,use_selection=True,object_types={"ARMATURE","MESH"},add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.0,
    path_mode="COPY",embed_textures=True,axis_forward="-Z",axis_up="Y")
print("POMPERSTEIN_AUTHORED_GROUNDSLAM",FBX,"frames=72","bones=",len(arm.data.bones),"floor_z=",base_floor)
