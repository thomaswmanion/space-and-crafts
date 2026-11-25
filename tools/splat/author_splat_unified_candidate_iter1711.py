"""Author a cohesive Splat mesh/rig candidate without touching the active Unity prefab.

The white torso and purple limbs are fused into one smooth skinned surface. The cap
and face remain separate fitted details. Outputs: a .blend source, GLB candidate,
and a simple review render. This is a local authored fallback while Meshy usage is
not verifiable; it is not automatically promoted into gameplay.
"""
import bpy
import math
from mathutils import Vector, Euler
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE_OUT = ROOT / "docs/art/splat-unified-candidate-iter1711"
UNITY_OUT = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1711"
SOURCE_OUT.mkdir(parents=True, exist_ok=True)
UNITY_OUT.mkdir(parents=True, exist_ok=True)
BLEND_PATH = SOURCE_OUT / "Splat_UnifiedRig_Candidate_Iter1711.blend"
GLB_PATH = SOURCE_OUT / "Splat_UnifiedRig_Candidate_Iter1711.glb"
FBX_PATH = UNITY_OUT / "Splat_UnifiedRig_Candidate_Iter1711.fbx"
PREVIEW_PATH = ROOT / "Logs/score-comparison/splat-unified-candidate-iter1711.png"
PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)

# Clear the file and establish a predictable Z-up, meter-based authoring scene.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras,
                   bpy.data.lights, bpy.data.armatures, bpy.data.actions):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.render.resolution_x = 1100
scene.render.resolution_y = 1100
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.world.color = (0.055, 0.068, 0.12)

def material(name, color, roughness=0.55):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = roughness
    return m

white = material('Splat • porcelain white', (0.93, 0.94, 0.96), 0.42)
purple = material('Splat • rich violet', (0.16, 0.018, 0.56), 0.52)
skin = bpy.data.materials.new('Splat • seamless white and violet skin')
skin.use_nodes = True
skin_bsdf = skin.node_tree.nodes.get('Principled BSDF')
skin_bsdf.inputs['Roughness'].default_value = 0.45
skin_color = skin.node_tree.nodes.new('ShaderNodeVertexColor')
skin_color.layer_name = 'SplatColor'
skin.node_tree.links.new(skin_color.outputs['Color'], skin_bsdf.inputs['Base Color'])
eye_mat = material('Splat • deep violet eyes', (0.10, 0.035, 0.20), 0.32)
ink = material('Splat • soft black expression', (0.018, 0.012, 0.028), 0.38)
highlight = material('Splat • single eye catchlight', (1.0, 0.98, 0.95), 0.22)
ground_mat = material('Review backdrop', (0.075, 0.095, 0.16), 0.82)

def smooth_mesh(obj):
    if obj.type == 'MESH':
        for poly in obj.data.polygons:
            poly.use_smooth = True

def ellipsoid(name, location, scale, mat=None, segments=32, rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    smooth_mesh(obj)
    if mat:
        obj.data.materials.append(mat)
    return obj

def capsule_segment(name, a, b, radius_a, radius_b=None, material_hint=None):
    """Rounded organic segment with length aligned to its endpoints."""
    a, b = Vector(a), Vector(b)
    direction = b - a
    obj = ellipsoid(name, (a + b) * 0.5,
                    (max(radius_a, radius_b or radius_a), max(radius_a, radius_b or radius_a), direction.length * 0.62),
                    material_hint, segments=24, rings=16)
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = direction.to_track_quat('Z', 'Y')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    return obj

def tube_path(name, points, radii, sides=20):
    """Build a smooth, tapered tube along a curved joint path."""
    pts = [Vector(p) for p in points]
    verts, faces = [], []
    rings = []
    for i, center in enumerate(pts):
        before = pts[max(0, i-1)]
        after = pts[min(len(pts)-1, i+1)]
        tangent = (after-before).normalized()
        ref = Vector((0,1,0))
        if abs(tangent.dot(ref)) > 0.95:
            ref = Vector((1,0,0))
        u = tangent.cross(ref).normalized()
        v = tangent.cross(u).normalized()
        ring = []
        radius = radii[i]
        for j in range(sides):
            angle = (2*math.pi*j)/sides
            point = center + u*(math.cos(angle)*radius) + v*(math.sin(angle)*radius*0.82)
            ring.append(len(verts))
            verts.append(tuple(point))
        rings.append(ring)
    faces.append(tuple(reversed(rings[0])))
    for i in range(len(rings)-1):
        for j in range(sides):
            j2=(j+1)%sides
            faces.append((rings[i][j],rings[i][j2],rings[i+1][j2],rings[i+1][j]))
    faces.append(tuple(rings[-1]))
    mesh=bpy.data.meshes.new(name+' mesh')
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    smooth_mesh(obj)
    return obj

# Generate overlapping organic masses, then voxel-remesh them as one continuous
# manifold body. This avoids floating shoulder/hip pieces while preserving the
# strong white-body / violet-limb design language.
parts = [ellipsoid('body volume', (0, 0, 1.05), (0.66, 0.55, 0.66), white, 48, 32)]
segments = []
for side, suffix in ((-1, 'L'), (1, 'R')):
    arm_points = [
        (side * 0.43, 0.015, 1.06),
        (side * 0.60, 0.010, 1.04),
        (side * 0.77, -0.005, 1.01),
        (side * 0.91, -0.025, 0.98),
    ]
    # A single curved, gently swelling limb ends in a mitten-like paddle.
    arm_tube = tube_path(f'arm {suffix} continuous', arm_points,
                         [0.09,0.10,0.105,0.10])
    parts.append(arm_tube)
    hand = ellipsoid(f'mitten {suffix} fused mass', arm_points[-1], (0.25,0.145,0.15), None, 32, 20)
    parts.append(hand)
    thumb_center = (side * 0.99, -0.035, 1.085)
    thumb = ellipsoid(f'mitten thumb {suffix} fused mass', thumb_center, (0.11,0.105,0.10), None, 24, 16)
    parts.append(thumb)
    segments += [
        (f'Arm_{suffix}', arm_points[0], arm_points[2], 0.20),
        (f'Hand_{suffix}', arm_points[2], arm_points[-1], 0.19),
    ]

    leg_points = [
        (side * 0.24, 0.005, 0.60),
        (side * 0.27, 0.000, 0.44),
        (side * 0.31, -0.025, 0.29),
        (side * 0.33, -0.065, 0.14),
    ]
    leg_tube = tube_path(f'leg {suffix} continuous', leg_points,
                         [0.12,0.12,0.13,0.14])
    parts.append(leg_tube)
    toe = ellipsoid(f'toe {suffix} continuous mass', leg_points[-1], (0.18,0.23,0.14), None, 32, 20)
    parts.append(toe)
    segments += [
        (f'Leg_{suffix}', leg_points[0], leg_points[2], 0.21),
        (f'Foot_{suffix}', leg_points[2], leg_points[-1], 0.20),
    ]

# Join source volumes and use a single voxel surface with smooth transitions.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
for obj in parts:
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
core = bpy.context.object
core.name = 'Splat_UnifiedBodyAndLimbs'
core.data.materials.clear()
core.data.materials.append(skin)
remesh = core.modifiers.new('continuous soft silhouette', 'REMESH')
remesh.mode = 'VOXEL'
remesh.voxel_size = 0.010
remesh.use_smooth_shade = True
bpy.context.view_layer.objects.active = core
bpy.ops.object.modifier_apply(modifier=remesh.name)
smooth = core.modifiers.new('soft surface relaxation', 'SMOOTH')
smooth.factor = 0.72
smooth.iterations = 7
bpy.ops.object.modifier_apply(modifier=smooth.name)
smooth_mesh(core)

# Assign the white shell and purple limb regions from the authored design regions.
# The purple boundary crosses inside the body so limb color does not start at a
# visible seam on the outside of the sphere.
def segdist(point, a, b):
    p, a, b = Vector(point), Vector(a), Vector(b)
    ab = b - a
    t = max(0.0, min(1.0, (p-a).dot(ab) / max(ab.length_squared, 1e-8)))
    return (p - (a + ab*t)).length

def smoothstep(edge0, edge1, value):
    t=max(0.0,min(1.0,(value-edge0)/max(1e-6,edge1-edge0)))
    return t*t*(3.0-2.0*t)

colors=core.data.color_attributes.new(name='SplatColor',type='FLOAT_COLOR',domain='POINT')
for vertex in core.data.vertices:
    p=vertex.co
    arm_band=0.0
    # Confine the color to each limb's outward side. A pure distance blur
    # softened purple too far into the white shoulder, making the ball look
    # dirty/mottled. The color begins inside the body silhouette and reaches
    # full violet just outside its surface, keeping the fused attachment.
    for side, suffix in ((-1,'L'), (1,'R')):
        arm_near=min((segdist(p,a,b) for name,a,b,_ in segments
                      if name.endswith('_'+suffix) and name.startswith(('Arm_','Hand_'))),
                     default=999.0)
        band=(1.0-smoothstep(0.20,0.34,arm_near))
        band*=smoothstep(0.70,0.80,p.z)*(1.0-smoothstep(1.34,1.42,p.z))
        band*=smoothstep(0.62,0.68,side*p.x)
        arm_band=max(arm_band,band)
    leg_near=min((segdist(p,a,b) for name,a,b,_ in segments
                  if name.startswith(('Leg_','Foot_'))),default=999.0)
    leg_band=(1.0-smoothstep(0.17,0.30,leg_near))
    leg_band*=1.0-smoothstep(0.66,0.76,p.z)
    mix=max(arm_band,leg_band)
    rgba=(0.93*(1-mix)+0.16*mix,
          0.94*(1-mix)+0.018*mix,
          0.96*(1-mix)+0.56*mix,
          1.0)
    colors.data[vertex.index].color=rgba
for poly in core.data.polygons:
    poly.material_index=0

# Skeleton: torso is the stable round body; joints only articulate the compact
# purple limbs. No body scaling is authored in any action.
arm_data = bpy.data.armatures.new('Splat_UnifiedRig')
rig = bpy.data.objects.new('Splat_UnifiedRig', arm_data)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name, head, tail, parent=None, connect=False):
    eb = arm_data.edit_bones.new(name)
    eb.head = head
    eb.tail = tail
    if parent:
        eb.parent = arm_data.edit_bones[parent]
        eb.use_connect = connect
    return eb

bone('Root', (0,0,0), (0,0,0.15))
bone('Hips', (0,0,0.58), (0,0,0.85), 'Root')
bone('Body', (0,0,0.85), (0,0,1.23), 'Hips')
bone('Head', (0,0,1.23), (0,0,1.53), 'Body')
for side, suffix in ((-1,'L'), (1,'R')):
    bone(f'Arm_{suffix}', (side*0.43,0.015,1.04), (side*0.68,-0.005,0.99), 'Body')
    bone(f'Hand_{suffix}', (side*0.68,-0.005,0.99), (side*0.78,-0.025,0.95), f'Arm_{suffix}', True)
    bone(f'Leg_{suffix}', (side*0.24,0.005,0.60), (side*0.31,-0.01,0.29), 'Hips')
    bone(f'Foot_{suffix}', (side*0.31,-0.01,0.29), (side*0.33,-0.065,0.14), f'Leg_{suffix}', True)
bpy.ops.object.mode_set(mode='OBJECT')

for name in ['Root','Hips','Body','Head','Arm_L','Hand_L','Leg_L','Foot_L',
             'Arm_R','Hand_R','Leg_R','Foot_R']:
    core.vertex_groups.new(name=name)

def smooth_weight(distance, radius):
    t = max(0.0, min(1.0, (radius - distance) / (radius * 0.55)))
    return t*t*(3.0-2.0*t)

limb_bone_data = []
for name, a, b, radius in segments:
    limb_bone_data.append((name, Vector(a), Vector(b), radius))

for vertex in core.data.vertices:
    p = vertex.co
    limb_scores = []
    for name, a, b, radius in limb_bone_data:
        d = segdist(p, a, b)
        score = smooth_weight(d, radius * 1.35)
        if score > 0.001:
            limb_scores.append((name, score))
    torso = max(0.08, 1.0 - max((v for _,v in limb_scores), default=0.0))
    # Seat the lower shell to hips for a small landing settle, while the upper
    # white orb and face remain on Body. This is joint motion, never body scaling.
    pelvis_share = 0.55 * (1.0 - smoothstep(0.48,0.95,p.z))
    body_weight = torso * (1.0-pelvis_share)
    hips_weight = torso * pelvis_share
    total = body_weight + hips_weight + sum(v for _,v in limb_scores)
    core.vertex_groups['Body'].add([vertex.index], body_weight/total, 'REPLACE')
    core.vertex_groups['Hips'].add([vertex.index], hips_weight/total, 'REPLACE')
    for name, value in limb_scores:
        core.vertex_groups[name].add([vertex.index], value/total, 'REPLACE')

arm_mod = core.modifiers.new('Splat skin', 'ARMATURE')
arm_mod.object = rig
core.parent = rig
core.matrix_parent_inverse = rig.matrix_world.inverted()

# Cap fitted close to the back of the head. It is a clothing accessory, not an
# unrelated primitive: the crown is a trimmed shell and the rear brim sits behind.
cap = ellipsoid('Backwards cap crown', (0, 0.045, 1.69), (0.37,0.31,0.19), purple, 40, 24)
for v in cap.data.vertices:
    # trim the underside to form a wearable crown shell with a seated rim
    if v.co.z < -0.075:
        v.co.z = -0.075
cap.data.update()
brim = ellipsoid('Backwards cap rear brim', (0, 0.46, 1.63), (0.29,0.25,0.045), purple, 36, 16)
strap = ellipsoid('Cap front strap', (0, -0.25, 1.66), (0.23,0.035,0.026), purple, 32, 16)

# Face features use shallow fitted meshes against the front (-Y) of the white ball.
for side, suffix in ((-1,'L'), (1,'R')):
    eye = ellipsoid(f'Eye_{suffix}', (side*0.19,-0.492,1.34), (0.090,0.042,0.135), eye_mat, 32, 24)
    ellipsoid(f'EyeCatch_{suffix}', (side*0.19-0.020,-0.534,1.385), (0.019,0.012,0.024), highlight, 20, 12)
    brow = capsule_segment(f'Brow_{suffix}', (side*0.11,-0.489,1.53), (side*0.27,-0.471,1.55), 0.025)
    brow.data.materials.append(purple)
# Friendly, small, curved smile, on a single smooth tube.
curve = bpy.data.curves.new('Splat_SmilePath', 'CURVE')
curve.dimensions = '3D'
curve.resolution_u = 20
curve.bevel_depth = 0.018
curve.bevel_resolution = 4
spline = curve.splines.new('BEZIER')
spline.bezier_points.add(2)
for bp, co in zip(spline.bezier_points, [(-0.105,-0.507,1.20),(0,-0.532,1.16),(0.105,-0.507,1.20)]):
    bp.co = co
    bp.handle_left_type = 'AUTO'
    bp.handle_right_type = 'AUTO'
smile = bpy.data.objects.new('Splat friendly smile', curve)
bpy.context.collection.objects.link(smile)
smile.data.materials.append(ink)
bpy.context.view_layer.objects.active = smile
smile.select_set(True)
bpy.ops.object.convert(target='MESH')
smile = bpy.context.object
smile.select_set(False)

# Attach face/cap details to the head joint, with small rigid vertex groups.
def rigid_parent(obj, bone_name):
    obj.parent = rig
    vg = obj.vertex_groups.new(name=bone_name)
    for v in obj.data.vertices:
        vg.add([v.index], 1.0, 'REPLACE')
    modifier = obj.modifiers.new('Follow Splat rig', 'ARMATURE')
    modifier.object = rig
    obj.parent_type = 'OBJECT'
    obj.parent = rig
    obj.matrix_parent_inverse = rig.matrix_world.inverted()
    # Rigid accessories use the head bone through a weighted armature modifier.
    obj.vertex_groups.get(bone_name)
    return obj

for obj in list(bpy.context.scene.objects):
    if obj.name.startswith(('Backwards cap','Cap front','Eye_','EyeCatch_','Brow_','Splat friendly')):
        rigid_parent(obj, 'Head')

# Set actions on armature. Key all poses at identical sample times and interpolate
# with Bezier curves; torso/hips remain unit scale and only articulate naturally.
def action(name, length, poses):
    act = bpy.data.actions.new(name)
    rig.animation_data_create()
    rig.animation_data.action = act
    for frame, values in poses:
        scene.frame_set(frame)
        for bone_name, rotation, location in values:
            pb = rig.pose.bones[bone_name]
            if bone_name.startswith('Arm_'):
                # Arm bones are mirrored across the body. Interpret their pose
                # as an armature-space shoulder gesture, then convert that
                # rotation into each bone's local rest basis. Directly applying
                # the same Euler axis to both mirrored bones caused little or
                # asymmetric visible movement in the preview.
                target = Euler((rotation[2], rotation[1], rotation[0]), 'XYZ').to_matrix().to_4x4()
                rest = pb.bone.matrix_local.copy()
                local = rest.inverted() @ target @ rest
                pb.rotation_mode = 'QUATERNION'
                pb.rotation_quaternion = local.to_quaternion()
                rotation_path = 'rotation_quaternion'
            else:
                pb.rotation_mode = 'XYZ'
                pb.rotation_euler = rotation
                rotation_path = 'rotation_euler'
            if location is not None:
                pb.location = location
            pb.keyframe_insert(data_path=rotation_path, frame=frame, group=bone_name)
            if location is not None:
                pb.keyframe_insert(data_path='location', frame=frame, group=bone_name)
            pb.scale = (1,1,1)
            pb.keyframe_insert(data_path='scale', frame=frame, group=bone_name)
    act.use_fake_user = True
    act['review_length_frames'] = length
    return act

def pose_set(body=(0,0,0), body_loc=(0,0,0), arm_l=(0,0,0), arm_r=(0,0,0), hand_l=(0,0,0), hand_r=(0,0,0),
             leg_l=(0,0,0), leg_r=(0,0,0), foot_l=(0,0,0), foot_r=(0,0,0), hip=(0,0,0)):
    return [('Body',body,body_loc),('Arm_L',arm_l,None),('Arm_R',arm_r,None),('Hand_L',hand_l,None),
            ('Hand_R',hand_r,None),('Leg_L',leg_l,None),('Leg_R',leg_r,None),
            ('Foot_L',foot_l,None),('Foot_R',foot_r,None),('Hips',(0,0,0),hip)]

action('Splat_Idle', 120, [
    (1,pose_set(body=(0,0,-0.012),arm_l=(0,0.02,-0.04),arm_r=(0,-0.02,0.04))),
    (31,pose_set(body=(0,0,0.015),arm_l=(0,-0.025,-0.01),arm_r=(0,0.025,0.01))),
    (61,pose_set(body=(0,0,0.005),arm_l=(0,0.02,0.04),arm_r=(0,-0.02,-0.04))),
    (91,pose_set(body=(0,0,-0.012),arm_l=(0,0.02,-0.04),arm_r=(0,-0.02,0.04))),
    (120,pose_set(body=(0,0,-0.012),arm_l=(0,0.02,-0.04),arm_r=(0,-0.02,0.04)))])
action('Splat_Run', 30, [
    (1,pose_set(arm_l=(0,0.52,-0.18),arm_r=(0,-0.52,0.18),leg_l=(0.42,0,0),leg_r=(-0.42,0,0),foot_l=(-0.10,0,0),foot_r=(0.10,0,0),body=(0.05,0,0))),
    (8,pose_set(arm_l=(0,0,0),arm_r=(0,0,0),leg_l=(0,0,0),leg_r=(0,0,0),body=(0,0,0.02))),
    (16,pose_set(arm_l=(0,-0.52,0.18),arm_r=(0,0.52,-0.18),leg_l=(-0.42,0,0),leg_r=(0.42,0,0),foot_l=(0.10,0,0),foot_r=(-0.10,0,0),body=(-0.05,0,0))),
    (23,pose_set(arm_l=(0,0,0),arm_r=(0,0,0),leg_l=(0,0,0),leg_r=(0,0,0),body=(0,0,-0.015))),
    (30,pose_set(arm_l=(0,0,0.34),arm_r=(0,0,-0.34),leg_l=(0.42,0,0),leg_r=(-0.42,0,0),foot_l=(-0.10,0,0),foot_r=(0.10,0,0),body=(0.05,0,0)))])
action('Splat_Jump', 42, [
    (1,pose_set(arm_l=(0,0.24,-0.20),arm_r=(0,-0.24,0.20),leg_l=(-0.10,0,0),leg_r=(-0.10,0,0))),
    (8,pose_set(arm_l=(0,0.66,-0.20),arm_r=(0,-0.66,0.20),leg_l=(-0.52,0,0),leg_r=(-0.52,0,0),body=(-0.04,0,0))),
    (22,pose_set(arm_l=(0,0.60,-0.16),arm_r=(0,-0.60,0.16),leg_l=(-0.58,0,0),leg_r=(-0.58,0,0),foot_l=(0.25,0,0),foot_r=(0.25,0,0),body=(0.02,0,0))),
    (34,pose_set(arm_l=(0,0.30,-0.12),arm_r=(0,-0.30,0.12),leg_l=(-0.28,0,0),leg_r=(-0.28,0,0))),
    (42,pose_set(arm_l=(0,0.24,-0.20),arm_r=(0,-0.24,0.20),leg_l=(-0.10,0,0),leg_r=(-0.10,0,0)))])
action('Splat_Land', 24, [
    (1,pose_set(arm_l=(0,0.34,0.22),arm_r=(0,-0.34,-0.22),leg_l=(-0.08,0,0),leg_r=(-0.08,0,0))),
    (6,pose_set(arm_l=(0,0.26,0.14),arm_r=(0,-0.26,-0.14),leg_l=(-0.30,0,0),leg_r=(-0.30,0,0),hip=(0,0,-0.035),body_loc=(0,0,-0.065),body=(0.08,0,0))),
    (12,pose_set(arm_l=(0,0.16,0.08),arm_r=(0,-0.16,-0.08),leg_l=(-0.16,0,0),leg_r=(-0.16,0,0),hip=(0,0,-0.02),body_loc=(0,0,-0.03),body=(0.04,0,0))),
    (24,pose_set())])

rig.animation_data.action = bpy.data.actions.get('Splat_Idle')
scene.frame_set(1)

# Presentation setup, excluded from the GLB asset export.
bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,0.0))
floor = bpy.context.object
floor.name = 'Review floor'
floor.data.materials.append(ground_mat)
def area(name, location, power, color, size):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy = power
    data.color = color
    data.shape = 'DISK'
    data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = (Vector((0,0,1.1)) - obj.location).to_track_quat('-Z','Y').to_euler()
    return obj
area('Key softbox', (3,-4,5), 560, (0.82,0.86,1.0), 4.0)
area('Warm fill', (-4,-2,2.8), 300, (1.0,0.75,0.82), 3.0)
area('Rim', (0,3,4), 650, (0.52,0.67,1.0), 3.0)
cam_data = bpy.data.cameras.new('Review camera')
cam = bpy.data.objects.new('Review camera', cam_data)
bpy.context.collection.objects.link(cam)
cam.location = (2.75,-5.8,2.35)
cam.rotation_euler = (Vector((0,0,1.0)) - cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 2.65
scene.camera = cam
scene.view_settings.view_transform = 'AgX'
scene.render.filepath = str(PREVIEW_PATH)

# Save source before render. Export only character objects so cameras/lights/floor
# remain out of the candidate asset. GLB preserves one unified skinned body mesh and
# the animation takes for integration review.
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
export_set = [rig, core]
for obj in bpy.context.scene.objects:
    if obj.name.startswith(('Backwards cap','Cap front','Eye_','EyeCatch_','Brow_','Splat friendly')):
        export_set.append(obj)
bpy.ops.object.select_all(action='DESELECT')
for obj in export_set:
    obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.gltf(filepath=str(GLB_PATH), export_format='GLB',
    use_selection=True, export_animations=True, export_skins=True,
    export_nla_strips=False, export_apply=False, export_materials='EXPORT',
    export_yup=True)
# Unity has no glTF importer package in this project. Export the same selected
# rig as FBX for the standard Unity ModelImporter, with one animation stack per
# authored action. Keep GLB as the portable interchange/archive copy.
bpy.ops.export_scene.fbx(filepath=str(FBX_PATH), use_selection=True,
    object_types={'ARMATURE','MESH'}, apply_scale_options='FBX_SCALE_ALL',
    use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False,
    armature_nodetype='NULL', bake_anim=True, bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0, path_mode='AUTO', embed_textures=True,
    axis_forward='-Z', axis_up='Y')
scene.render.filepath = str(PREVIEW_PATH)
bpy.ops.render.render(write_still=True)
print(f'SPLAT_CANDIDATE_OK blend={BLEND_PATH} glb={GLB_PATH} fbx={FBX_PATH} preview={PREVIEW_PATH} verts={len(core.data.vertices)} faces={len(core.data.polygons)} actions={len(bpy.data.actions)}')





