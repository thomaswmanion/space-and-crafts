import bpy
import math
import os
from mathutils import Vector


SRC = os.path.abspath(
    "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/"
    "20261031_223339_rainbow-kingdom-spider-enemy_01a1952b/RainbowKingdomSpider.glb"
)
OUT = os.path.abspath(
    "Assets/SpaceAndCrafts/Art/Commissioned/Enemies/MeshySpenddown/"
    "20261101_rainbow-spider-rig-candidate-iter1446"
)
os.makedirs(OUT, exist_ok=True)

# Start clean, then import the delivered single cohesive Meshy surface.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=SRC)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
if len(meshes) != 1:
    raise RuntimeError(f"Expected one imported cohesive mesh; got {len(meshes)}")
mesh = meshes[0]
mesh.name = "RainbowKingdomSpider_MeshySurface"
mesh.data.name = "RainbowKingdomSpider_CohesiveMesh"

# Inspection established local Z is vertical and the face points toward -Y (verified by Iter1445 axis sweep).
# Feet are grounded near Z=-0.8; the face-forward rows run along local -Y.
feet_z = [-0.602, -0.434, -0.293, -0.170]
arm_data = bpy.data.armatures.new("RainbowSpider_8LegRig")
arm = bpy.data.objects.new("RainbowSpider_8LegRig", arm_data)
bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
mesh.select_set(False)
bpy.ops.object.mode_set(mode="EDIT")
root = arm_data.edit_bones.new("Body")
root.head = (0.0, 0.0, 0.0)
root.tail = (0.0, 0.0, 0.35)

leg_specs = []
for side in (-1, 1):
    for row, zfoot in enumerate(feet_z):
        tag = "L" if side < 0 else "R"
        prefix = f"Leg_{tag}_{row}"
        hip = Vector((side * 0.16, zfoot + 0.16, -0.17))
        knee = Vector((side * 0.43, zfoot + 0.055, -0.27))
        ankle = Vector((side * 0.44, zfoot, -0.66))
        toe = Vector((side * 0.43, zfoot - 0.015, -0.81))
        chain = []
        parent = root
        for suffix, start, end in (
            ("Upper", hip, knee), ("Lower", knee, ankle), ("Foot", ankle, toe)
        ):
            bone = arm_data.edit_bones.new(prefix + "_" + suffix)
            bone.head, bone.tail, bone.parent = start, end, parent
            bone.use_connect = suffix != "Upper"
            chain.append((bone.name, Vector(start), Vector(end)))
            parent = bone
        target = arm_data.edit_bones.new(prefix + "_IK")
        target.head = toe
        target.tail = toe + Vector((0, 0, 0.07))
        target.parent = root
        target.use_deform = False
        leg_specs.append({"tag": tag, "row": row, "z": zfoot,
                          "side": side, "hip": hip, "knee": knee,
                          "ankle": ankle, "toe": toe,
                          "chain": chain, "target": target.name})
bpy.ops.object.mode_set(mode="OBJECT")

# Smooth distance-based weights keep the face/body on the root and feather each
# limb into the nearest articulated chain, without splitting the source surface.
groups = {b.name: mesh.vertex_groups.new(name=b.name) for b in arm_data.bones}
segments = []
for leg in leg_specs:
    for bname, a, b in leg["chain"]:
        segments.append((leg, bname, a, b))
verts = mesh.data.vertices
for v in verts:
    p = v.co.copy()
    core_metric = math.sqrt((p.x / 0.31) ** 2 + (p.y / 0.39) ** 2 + (max(0.0, -p.z) / 0.36) ** 2)
    core = max(0.0, min(1.0, 1.0 - (core_metric - 0.72) / 0.82))
    bone_scores = {}
    for leg, bname, a, b in segments:
        ab = b - a
        t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-8)))
        d = (p - (a + t * ab)).length
        if d < 0.34:
            # Smoothly blend neighboring segments around each articulated joint
            # instead of switching every vertex abruptly to its nearest bone.
            score = math.exp(-((d / 0.145) ** 2))
            bone_scores[bname] = bone_scores.get(bname, 0.0) + score
    body_score = 0.12 + core * 3.2
    limb_scores = {name: score * (1.0 - 0.92 * core)
                   for name, score in bone_scores.items()}
    total = body_score + sum(limb_scores.values())
    groups["Body"].add([v.index], body_score / total, "REPLACE")
    for name, score in limb_scores.items():
        if score / total > 0.001:
            groups[name].add([v.index], score / total, "REPLACE")
# glTF skinning (and common game runtimes) evaluate four influences per vertex.
# Keep the strongest four and normalize them to avoid silent exporter truncation.
keep_by_group = {g.name: [] for g in mesh.vertex_groups}
drop_by_group = {g.name: [] for g in mesh.vertex_groups}
for v in verts:
    ordered = sorted(v.groups, key=lambda item: item.weight, reverse=True)
    kept = ordered[:4]
    dropped = ordered[4:]
    total = sum(item.weight for item in kept)
    for item in kept:
        keep_by_group[mesh.vertex_groups[item.group].name].append((v.index, item.weight / total))
    for item in dropped:
        drop_by_group[mesh.vertex_groups[item.group].name].append(v.index)
for group in mesh.vertex_groups:
    if drop_by_group[group.name]:
        group.remove(drop_by_group[group.name])
    for index, weight in keep_by_group[group.name]:
        group.add([index], weight, "REPLACE")
mesh.parent = arm
mod = mesh.modifiers.new("RainbowSpider_Skin", "ARMATURE")
mod.object = arm
mod.use_deform_preserve_volume = True

# Two-bone IK on each leg. The authored actions key only nondeforming IK goals
# and the body bone; the source surface remains one continuous skinned mesh.
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
for pose_bone in arm.pose.bones:
    # Every action keys rotation_euler for each pose bone. Keeping some bones in
    # Blender's default quaternion mode created duplicate rotation channels and
    # exporter warnings, even for bones the authored pose never rotates.
    pose_bone.rotation_mode = "XYZ"
for leg in leg_specs:
    lower = arm.pose.bones[leg["chain"][1][0]]
    ik = lower.constraints.new("IK")
    ik.name = leg["chain"][0][0] + "_GroundContactIK"
    ik.target = arm
    ik.subtarget = leg["target"]
    ik.chain_count = 2
    ik.use_stretch = False
    ik.influence = 1.0
bpy.ops.object.mode_set(mode="OBJECT")

scene = bpy.context.scene
scene.render.fps = 30
scene.frame_start = 1

def key_pose(action_name, frames, pose_fn):
    scene.frame_start = frames[0]
    scene.frame_end = frames[-1]
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    act = bpy.data.actions.new(action_name)
    # Blender drops unused actions on save unless they have an owner or fake
    # user. Each pose clip is reviewed/exported separately, so keep the whole
    # authored set durable in the saved source file.
    act.use_fake_user = True
    arm.animation_data_create()
    arm.animation_data.action = act
    for f in frames:
        scene.frame_set(f)
        pose_fn(f, frames)
        bpy.context.view_layer.update()
        for pb in arm.pose.bones:
            pb.keyframe_insert(data_path="location", frame=f, group=pb.name)
            pb.keyframe_insert(data_path="rotation_euler", frame=f, group=pb.name)
    # Blender 5.1 stores curves inside layered Action strips; keyframe_insert
    # creates interpolated curves with Blender's default smooth Bezier handles.
    return act

def reset_goals():
    for leg in leg_specs:
        pb = arm.pose.bones[leg["target"]]
        pb.location = (0, 0, 0)
        pb.rotation_euler = (0, 0, 0)
    body = arm.pose.bones["Body"]
    body.location = (0, 0, 0)
    body.rotation_euler = (0, 0, 0)

def idle_pose(f, frames):
    reset_goals()
    t = (f - frames[0]) / (frames[-1] - frames[0])
    arm.pose.bones["Body"].location.z = 0.012 * math.sin(t * math.tau)
    arm.pose.bones["Body"].rotation_euler.z = 0.018 * math.sin(t * math.tau)
    for i, leg in enumerate(leg_specs):
        pb = arm.pose.bones[leg["target"]]
        pb.location.z = 0.008 * math.sin(t * math.tau + i * 0.55)

def walk_pose(f, frames):
    reset_goals()
    t = (f - frames[0]) / (frames[-1] - frames[0])
    arm.pose.bones["Body"].location.z = 0.025 + 0.018 * (0.5 - 0.5 * math.cos(t * math.tau * 2))
    arm.pose.bones["Body"].rotation_euler.z = 0.025 * math.sin(t * math.tau)
    for leg in leg_specs:
        # Alternating tripod pattern: opposing pairs trade support and swing.
        group = (leg["row"] % 2) ^ (1 if leg["side"] > 0 else 0)
        phase = t * math.tau + group * math.pi
        stride = 0.075 * math.sin(phase)
        lift = max(0.0, math.sin(phase)) ** 1.5 * 0.09
        pb = arm.pose.bones[leg["target"]]
        pb.location.y = -stride
        pb.location.z = lift

def attack_pose(f, frames):
    reset_goals()
    t = (f - frames[0]) / (frames[-1] - frames[0])
    anticipation = max(0.0, min(1.0, t / 0.34))
    strike = max(0.0, min(1.0, (t - 0.34) / 0.24))
    recover = max(0.0, min(1.0, (t - 0.58) / 0.42))
    body = arm.pose.bones["Body"]
    body.location.z = -0.08 * anticipation + 0.10 * strike * (1 - recover)
    body.location.y = -0.13 * strike * (1 - recover)
    body.rotation_euler.x = -0.10 * strike * (1 - recover)
    for leg in leg_specs:
        pb = arm.pose.bones[leg["target"]]
        if leg["row"] == 0:
            pb.location.z = 0.13 * max(0.0, min(1.0, t / 0.34))
            pb.location.y = -0.15 * max(0.0, min(1.0, (t - 0.34) / 0.18))
        else:
            pb.location.z = -0.03 * anticipation

def hit_pose(f, frames):
    reset_goals()
    t = (f - frames[0]) / (frames[-1] - frames[0])
    pulse = math.sin(min(1.0, t / 0.7) * math.pi) * (1.0 if t < 0.7 else 0.0)
    body = arm.pose.bones["Body"]
    body.location.z = 0.11 * pulse
    body.rotation_euler.z = 0.12 * pulse
    body.rotation_euler.x = 0.10 * pulse
    for leg in leg_specs:
        arm.pose.bones[leg["target"]].location.z = 0.06 * pulse

def defeat_pose(f, frames):
    reset_goals()
    t = (f - frames[0]) / (frames[-1] - frames[0])
    settle = max(0.0, min(1.0, t / 0.72))
    body = arm.pose.bones["Body"]
    body.location.z = -0.16 * settle
    body.rotation_euler.x = 0.25 * settle
    body.rotation_euler.z = 0.10 * math.sin(t * math.pi) * settle
    for leg in leg_specs:
        pb = arm.pose.bones[leg["target"]]
        pb.location.z = -0.06 * settle
        pb.location.y = (-0.07 if leg["row"] in (0, 3) else 0.04) * settle

actions = [
    key_pose("Spider_Idle", [1, 16, 31, 46, 61], idle_pose),
    key_pose("Spider_Walk", [1, 5, 9, 13, 17, 21, 25], walk_pose),
    key_pose("Spider_Attack", [1, 6, 11, 16, 21, 26, 31], attack_pose),
    key_pose("Spider_Hit", [1, 6, 11, 16, 21, 26], hit_pose),
    key_pose("Spider_Defeat", [1, 7, 13, 19, 25, 31, 37], defeat_pose),
]

# Configure a review camera and lights for posed samples. No game engine scene
# is edited by this asset-authoring script.
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
world = scene.world or bpy.data.worlds.new("SpiderReviewWorld")
scene.world = world
world.color = (0.075, 0.075, 0.075)

def point(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()

cam_data = bpy.data.cameras.new("SpiderReviewCamera")
cam = bpy.data.objects.new("SpiderReviewCamera", cam_data)
scene.collection.objects.link(cam)
cam.location = (0.0, -4.6, 0.65)
cam.data.type = "ORTHO"
cam.data.ortho_scale = 2.7
point(cam, (0, 0.0, -0.05))
scene.camera = cam
for name, loc, energy, size in (
    ("Key", (-3, -3, 4), 850, 4), ("Fill", (3, -1, 2), 500, 4),
    ("Rim", (0, 3, 3), 650, 3),
):
    ld = bpy.data.lights.new(name, "AREA")
    lo = bpy.data.objects.new(name, ld)
    scene.collection.objects.link(lo)
    lo.location = loc
    ld.energy, ld.shape, ld.size = energy, "DISK", size
    point(lo, (0, 0, 0))

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "RainbowSpider_RigCandidate.blend"))

# Render separated action poses at authored key points for deformation review.
for action, frame in ((actions[0], 16), (actions[1], 9), (actions[2], 21),
                      (actions[3], 11), (actions[4], 25)):
    arm.animation_data.action = action
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    scene.render.filepath = os.path.join(OUT, action.name + ".png")
    bpy.ops.render.render(write_still=True)

# Export an engine-neutral GLB with one animation per authored action.
arm.animation_data.action = actions[0]
scene.frame_set(1)
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.gltf(
    filepath=os.path.join(OUT, "RainbowSpider_RigCandidate.glb"),
    export_format="GLB", export_animations=True,
    export_animation_mode="ACTIONS", export_skins=True,
    export_yup=True, export_apply=False,
)

# FBX action-stack export is useful for Unity import; exporter bakes the IK.
bpy.ops.export_scene.fbx(
    filepath=os.path.join(OUT, "RainbowSpider_RigCandidate.fbx"),
    use_selection=True, object_types={"ARMATURE", "MESH"},
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=True, bake_anim_simplify_factor=0.0,
)
print(f"RIG_CANDIDATE_DONE mesh_vertices={len(verts)} bones={len(arm_data.bones)} actions={len(actions)}")

