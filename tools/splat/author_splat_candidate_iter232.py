"""Build and export a cohesive, skinned Splat prototype without external credits.

Run with Blender 5.x in background mode. The main silhouette is a single fused
metaball surface; shoes and explorer gear are deliberate character accessories.
"""

import math
import os
import random
import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector


PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT, "Logs", "score-comparison", "splat-candidate-iter232", "Splat_Rigged_FaceArtCandidate.blend")
FBX_PATH = os.path.join(PROJECT, "Logs", "score-comparison", "splat-candidate-iter232", "Splat_Rigged_FaceArtCandidate.fbx")
PREVIEW_DIR = os.path.join(PROJECT, "Logs", "score-comparison", "splat-candidate-iter232", "previews")
SKIN_TEXTURE_PATH = os.path.join(PROJECT, "Logs", "score-comparison", "splat-candidate-iter232", "Splat_Skin_Albedo.png")
SASH_TEXTURE_PATH = os.path.join(PROJECT, "Logs", "score-comparison", "splat-candidate-iter232", "Splat_MapSash_Albedo.png")


def mat(name, color, roughness=0.48, metallic=0.0, subsurface=0.0):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    if "Subsurface Weight" in shader.inputs:
        shader.inputs["Subsurface Weight"].default_value = subsurface
    return material


def make_skin_texture(size=512):
    """Create seamless watercolor skin with soft painted face accents."""
    rng = random.Random(417)
    frequencies = (4, 8, 16, 32, 64)
    weights = (0.48, 0.25, 0.14, 0.08, 0.05)
    grids = [[ [rng.random() for _ in range(freq)] for _ in range(freq) ] for freq in frequencies]

    def smooth(t):
        return t * t * (3.0 - 2.0 * t)

    def sample(grid, u, v):
        count = len(grid)
        x = (u % 1.0) * count
        y = (v % 1.0) * count
        x0 = int(x) % count
        y0 = int(y) % count
        x1 = (x0 + 1) % count
        y1 = (y0 + 1) % count
        fx = smooth(x - int(x))
        fy = smooth(y - int(y))
        a = grid[y0][x0] * (1.0 - fx) + grid[y0][x1] * fx
        b = grid[y1][x0] * (1.0 - fx) + grid[y1][x1] * fx
        return a * (1.0 - fy) + b * fy

    def smoothstep(edge0, edge1, value):
        t = max(0.0, min(1.0, (value - edge0) / (edge1 - edge0)))
        return t * t * (3.0 - 2.0 * t)

    def paint(rgb, color, amount):
        return tuple(channel * (1.0 - amount) + color[index] * amount
                     for index, channel in enumerate(rgb))

    image = bpy.data.images.new("Splat_SoftAmethyst_Albedo", width=size, height=size, alpha=False)
    image.colorspace_settings.name = "sRGB"
    pixels = [0.0] * (size * size * 4)
    offset = 0
    for y in range(size):
        v = (y + 0.5) / size
        for x in range(size):
            u = (x + 0.5) / size
            tone = sum(sample(grid, u, v) * weight for grid, weight in zip(grids, weights)) - 0.5
            grain = (rng.random() - 0.5) * 0.018
            # Keep the watercolor broad and soft, but give it enough value range to
            # survive gameplay-scale lighting instead of reading as a flat purple ball.
            rgb = (max(0.0, min(1.0, 0.47 + tone * 0.24 + grain)),
                   max(0.0, min(1.0, 0.18 + tone * 0.10 + grain * 0.45)),
                   max(0.0, min(1.0, 0.80 + tone * 0.18 + grain * 0.7)))

            # Low-contrast watercolor halos root the blinking 3D eyes into skin.
            for side in (-1, 1):
                eye_u = 0.5 + math.atan2(side * 0.195, 0.5) / math.tau
                du = abs(u - eye_u)
                du = min(du, 1.0 - du)
                eye_q = (du / 0.057) ** 2 + ((v - 0.718) / 0.13) ** 2
                socket_wash = (1.0 - smoothstep(0.55, 1.0, eye_q)) * 0.2
                rgb = paint(rgb, (0.67, 0.42, 0.91), socket_wash)

                cheek_u = 0.5 + math.atan2(side * 0.39, 0.5) / math.tau
                cheek_du = abs(u - cheek_u)
                cheek_du = min(cheek_du, 1.0 - cheek_du)
                cheek_q = (cheek_du / 0.022) ** 2 + ((v - 0.58) / 0.021) ** 2
                cheek_wash = (1.0 - smoothstep(0.35, 1.0, cheek_q)) * 0.38
                rgb = paint(rgb, (1.0, 0.38, 0.61), cheek_wash)

            pixels[offset] = rgb[0]
            pixels[offset + 1] = rgb[1]
            pixels[offset + 2] = rgb[2]
            pixels[offset + 3] = 1.0
            offset += 4

    image.pixels.foreach_set(pixels)
    image.filepath_raw = SKIN_TEXTURE_PATH
    image.file_format = "PNG"
    image.save()
    return image


def make_sash_texture(width=512, height=128):
    """Paint a repeating teal weave with a restrained brass diamond stitch."""
    image = bpy.data.images.new("Splat_MapSash_Albedo", width=width, height=height, alpha=False)
    image.colorspace_settings.name = "sRGB"
    pixels = np.zeros((height, width, 4), dtype=np.float32)
    for y in range(height):
        v = y / max(1, height - 1)
        for x in range(width):
            u = x / width
            wave = 0.018 * math.sin(x * 0.19) + 0.012 * math.sin(y * 0.43 + x * 0.035)
            edge = min(v, 1.0 - v)
            color = np.array((0.055, 0.31, 0.34), dtype=np.float32) + wave
            if edge < 0.055:
                color = np.array((0.56, 0.37, 0.19), dtype=np.float32) + wave * 0.35
            repeat_x = (u * 12.0) % 1.0
            diamond = abs(repeat_x - 0.5) / 0.17 + abs(v - 0.5) / 0.16
            if diamond < 1.0:
                color = np.array((0.60, 0.43, 0.24), dtype=np.float32) + wave * 0.3
            pixels[y, x, :3] = np.clip(color, 0.0, 1.0)
            pixels[y, x, 3] = 1.0
    image.pixels.foreach_set(pixels.ravel())
    image.filepath_raw = SASH_TEXTURE_PATH
    image.file_format = "PNG"
    image.save()
    return image


def make_uv_sphere(name, location, scale, material, parent=None, segments=32, rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=(0, 0, 0) if parent is not None else location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    if parent is not None:
        obj.parent = parent
        obj.matrix_parent_inverse = Matrix.Identity(4)
        obj.location = location
    return obj


def make_soft_satchel(name, location, dimensions, corner_radius, material):
    """Create one softly rounded fabric-bag mesh, rather than an oval badge-like blob."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = obj.modifiers.new("Soft fabric corners", "BEVEL")
    bevel.width = corner_radius
    bevel.segments = 6
    bevel.harden_normals = True
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    obj.data.materials.append(material)
    return obj


def front_surface_y(body, x, z, clearance=0.0):
    """Return the actual fused-body front surface at a face-detail coordinate."""
    hit, location, _normal, _face = body.ray_cast(Vector((x, -5.0, z)), Vector((0.0, 1.0, 0.0)))
    if not hit:
        raise RuntimeError(f"Splat face detail missed body surface at ({x:.3f}, {z:.3f})")
    return location.y - clearance


def make_map_sash(body, material, samples=192):
    """Build one UV-mapped fabric band that hugs and wraps the body skin."""
    vertices = []
    faces = []
    uv_by_face = []
    half_width = 0.045
    for index in range(samples):
        angle = math.tau * index / samples
        outward = Vector((math.sin(angle), -math.cos(angle), 0.0))
        center_z = 0.80 - 0.11 * math.cos(angle) - 0.18 * math.sin(angle)
        for z in (center_z - half_width, center_z + half_width):
            origin = Vector((outward.x * 2.0, outward.y * 2.0, z))
            hit, location, normal, _face = body.ray_cast(origin, -outward)
            if not hit:
                raise RuntimeError(f"Splat sash did not conform to body at angle {angle:.3f}, z {z:.3f}")
            vertices.append(tuple(location + normal * 0.016))

    for index in range(samples):
        next_index = (index + 1) % samples
        faces.append((index * 2, next_index * 2, next_index * 2 + 1, index * 2 + 1))
        u0 = index / samples
        u1 = (index + 1) / samples
        uv_by_face.append(((u0, 0.0), (u1, 0.0), (u1, 1.0), (u0, 1.0)))

    mesh = bpy.data.meshes.new("Splat_WovenMapSashMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    mesh.update()
    uv = mesh.uv_layers.new(name="Splat_MapSashUV")
    uv_values = []
    for face_uv in uv_by_face:
        for coordinate in face_uv:
            uv_values.extend(coordinate)
    uv.data.foreach_set("uv", uv_values)
    for polygon in mesh.polygons:
        polygon.use_smooth = True

    sash = bpy.data.objects.new("Mesh_ContinuousMapSash", mesh)
    bpy.context.collection.objects.link(sash)
    solidify = sash.modifiers.new("Woven band thickness", "SOLIDIFY")
    solidify.thickness = 0.016
    bevel = sash.modifiers.new("Soft cloth edges", "BEVEL")
    bevel.width = 0.012
    bevel.segments = 3
    sash.modifiers.new("Cloth corner normals", "WEIGHTED_NORMAL")
    return sash


def make_empty(name, location):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.05
    obj.location = location
    return obj


def parent_to_bone(obj, armature, bone_name):
    # Keep accessory geometry in armature space and let a single bone skin it.
    # This avoids Blender's bone-parent offset ambiguity and exports cleanly to Unity.
    if obj.type != "MESH":
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.convert(target="MESH")
        obj = bpy.context.object
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    group = obj.vertex_groups.get(bone_name) or obj.vertex_groups.new(name=bone_name)
    if len(obj.data.vertices) > 0:
        group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    modifier = obj.modifiers.new("Splat accessory skin", "ARMATURE")
    modifier.object = armature
    obj.parent = armature
    obj.matrix_parent_inverse = Matrix.Identity(4)
    obj.location = (0, 0, 0)
    obj.rotation_euler = (0, 0, 0)
    obj.scale = (1, 1, 1)


def add_star(name, center, radius, depth, material, parent=None, points=5):
    verts = []
    faces = []
    sides = points * 2
    for layer, y in enumerate((-depth * 0.5, depth * 0.5)):
        for i in range(sides):
            angle = math.pi * 0.5 + i * math.pi / points
            r = radius if i % 2 == 0 else radius * 0.43
            verts.append((math.cos(angle) * r, y, math.sin(angle) * r))
    faces.append(tuple(range(sides - 1, -1, -1)))
    faces.append(tuple(range(sides, sides * 2)))
    for i in range(sides):
        nxt = (i + 1) % sides
        faces.append((i, nxt, sides + nxt, sides + i))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(material)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = center
    bevel = obj.modifiers.new("Soft stitched edges", "BEVEL")
    bevel.width = 0.015
    bevel.segments = 3
    obj.modifiers.new("Weighted corner normals", "WEIGHTED_NORMAL")
    if parent is not None:
        obj.parent = parent
        obj.matrix_parent_inverse = Matrix.Identity(4)
        obj.location = center
    return obj


def make_surface_patch(name, body, outline, material, clearance):
    """Make a small, double-sided XZ artwork patch conformed to the body skin."""
    center_x = sum(x for x, _z in outline) / len(outline)
    center_z = sum(z for _x, z in outline) / len(outline)
    vertices = [(x, front_surface_y(body, x, z, clearance), z) for x, z in outline]
    center_index = len(vertices)
    vertices.append((center_x, front_surface_y(body, center_x, center_z, clearance), center_z))
    faces = []
    for index in range(len(outline)):
        nxt = (index + 1) % len(outline)
        # Reverse winding so the visible face normal points toward the front (-Y).
        faces.append((center_index, nxt, index))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def add_open_smile(armature, body, dark_material, tongue_material, tooth_material):
    # A small closed curve reads warm and playful at gameplay scale. The former
    # black open oval and tiny teeth collapsed into a worried expression in Unity.
    face_lift = 0.14
    mouth_outline = [
        (-0.115, 0.805), (-0.075, 0.789), (-0.035, 0.78), (0.0, 0.778),
        (0.035, 0.78), (0.075, 0.789), (0.115, 0.805),
        (0.085, 0.787), (0.04, 0.772), (0.0, 0.769),
        (-0.04, 0.772), (-0.085, 0.787),
    ]
    mouth_outline = [(x, z + face_lift) for x, z in mouth_outline]
    mouth = make_surface_patch("Mesh_SplatSmile", body, mouth_outline, dark_material, 0.012)
    parent_to_bone(mouth, armature, "ART_SplatSmile")
    return mouth


def create_bone_rig(root):
    armature_data = bpy.data.armatures.new("Splat_SkinRig")
    arm = bpy.data.objects.new("Splat_SkinRig", armature_data)
    bpy.context.collection.objects.link(arm)
    arm.parent = root
    arm.show_in_front = True
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bones = {}

    def bone(name, head, tail, parent=None, deform=True):
        edit = armature_data.edit_bones.new(name)
        edit.head = head
        edit.tail = tail
        edit.use_deform = deform
        if parent:
            edit.parent = bones[parent]
            edit.use_connect = (bones[parent].tail - edit.head).length < 0.002
        bones[name] = edit
        return edit

    bone("Bone_Root", (0, 0, 0.02), (0, 0, 0.36), deform=False)
    bone("Bone_Hips", (0, 0, 0.36), (0, 0, 0.64), "Bone_Root")
    bone("Bone_Spine", (0, 0, 0.64), (0, 0, 0.96), "Bone_Hips")
    bone("Bone_Chest", (0, 0, 0.96), (0, 0, 1.24), "Bone_Spine")
    bone("Bone_Head", (0, 0, 1.10), (0, 0, 1.44), "Bone_Chest")
    bone("Bone_Antenna", (0.0, 0, 1.42), (-0.23, -0.05, 1.78), "Bone_Head")
    bone("Bone_ArmL", (-0.55, 0, 1.00), (-1.02, 0, 0.79), "Bone_Chest")
    bone("Bone_ArmR", (0.55, 0, 1.00), (1.02, 0, 0.79), "Bone_Chest")
    bone("Bone_LegL", (-0.28, 0, 0.36), (-0.33, 0, 0.12), "Bone_Hips")
    bone("Bone_LegR", (0.28, 0, 0.36), (0.33, 0, 0.12), "Bone_Hips")
    bone("ART_EyeRootL", (-0.195, -0.76, 1.03), (-0.195, -0.76, 1.08), "Bone_Head")
    bone("ART_EyeRootR", (0.195, -0.76, 1.03), (0.195, -0.76, 1.08), "Bone_Head")
    bone("ART_IrisL", (-0.195, -0.798, 1.102), (-0.195, -0.827, 1.102), "ART_EyeRootL")
    bone("ART_IrisR", (0.195, -0.798, 1.102), (0.195, -0.827, 1.102), "ART_EyeRootR")
    bone("ART_SplatSmile", (0, -0.86, 0.79), (0, -0.91, 0.79), "Bone_Head")
    bone("ART_MakerPack", (0, 0.83, 0.86), (0, 0.83, 0.95), "Bone_Chest")
    bone("ART_MakerPouch", (-0.44, -0.77, 0.66), (-0.44, -0.77, 0.75), "Bone_Chest")
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.select_set(False)
    return arm


def seg_distance(point, start, end):
    delta = end - start
    denom = max(delta.length_squared, 1e-8)
    t = max(0.0, min(1.0, (point - start).dot(delta) / denom))
    return (point - (start + delta * t)).length


def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)


def skin_body(body, arm):
    names = ["Bone_Hips", "Bone_Spine", "Bone_Chest", "Bone_Head", "Bone_Antenna", "Bone_ArmL", "Bone_ArmR", "Bone_LegL", "Bone_LegR"]
    groups = {name: body.vertex_groups.new(name=name) for name in names}
    segments = {
        "Bone_ArmL": (Vector((-0.55, 0, 1.00)), Vector((-1.02, 0, 0.79))),
        "Bone_ArmR": (Vector((0.55, 0, 1.00)), Vector((1.02, 0, 0.79))),
        "Bone_LegL": (Vector((-0.28, 0, 0.36)), Vector((-0.33, 0, 0.12))),
        "Bone_LegR": (Vector((0.28, 0, 0.36)), Vector((0.33, 0, 0.12))),
    }
    for vertex in body.data.vertices:
        p = vertex.co
        z = p.z
        side = abs(p.x)
        hips = math.exp(-((z - 0.43) / 0.39) ** 2)
        spine = math.exp(-((z - 0.78) / 0.43) ** 2)
        chest = math.exp(-((z - 1.06) / 0.42) ** 2)
        head = math.exp(-((z - 1.40) / 0.35) ** 2)
        weights = {"Bone_Hips": hips, "Bone_Spine": spine, "Bone_Chest": chest, "Bone_Head": head}
        arm_gate = smoothstep(0.43, 0.74, side)
        leg_gate = 1.0 - smoothstep(0.28, 0.49, z)
        for name in ("Bone_ArmL", "Bone_ArmR", "Bone_LegL", "Bone_LegR"):
            start, end = segments[name]
            d = seg_distance(p, start, end)
            if "Arm" in name:
                value = math.exp(-(d / 0.26) ** 2) * arm_gate
            else:
                value = math.exp(-(d / 0.23) ** 2) * leg_gate
            weights[name] = value
        total = sum(weights.values())
        if total <= 1e-8:
            groups["Bone_Chest"].add([vertex.index], 1.0, "REPLACE")
            continue
        for name, value in weights.items():
            normalized = value / total
            if normalized > 0.015:
                groups[name].add([vertex.index], normalized, "REPLACE")
    modifier = body.modifiers.new("Splat organic skin", "ARMATURE")
    modifier.object = arm
    body.parent = arm
    body.matrix_parent_inverse = arm.matrix_world.inverted()


def unwrap_body(body):
    """Project the fused skin cylindrically with its seam hidden on the rear."""
    mesh = body.data
    uv_layer = mesh.uv_layers.new(name="Splat_Body_Cylindrical")
    positions = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
    mesh.vertices.foreach_get("co", positions)
    positions = positions.reshape((-1, 3))
    loop_vertices = np.empty(len(mesh.loops), dtype=np.int32)
    mesh.loops.foreach_get("vertex_index", loop_vertices)
    loop_positions = positions[loop_vertices]
    min_z = float(np.min(positions[:, 2]))
    height = max(float(np.max(positions[:, 2]) - min_z), 1e-6)
    uvs = np.empty((len(mesh.loops), 2), dtype=np.float32)
    uvs[:, 0] = np.arctan2(loop_positions[:, 0], -loop_positions[:, 1]) / (2.0 * math.pi) + 0.5
    uvs[:, 1] = (loop_positions[:, 2] - min_z) / height

    polygon_count = len(mesh.polygons)
    loop_totals = np.empty(polygon_count, dtype=np.int32)
    mesh.polygons.foreach_get("loop_total", loop_totals)
    if int(np.sum(loop_totals)) != len(mesh.loops):
        raise RuntimeError("Polygon loop counts do not cover the Splat body loops")
    face_for_loop = np.repeat(np.arange(polygon_count, dtype=np.int32), loop_totals)
    face_min = np.full(polygon_count, np.inf, dtype=np.float32)
    face_max = np.full(polygon_count, -np.inf, dtype=np.float32)
    np.minimum.at(face_min, face_for_loop, uvs[:, 0])
    np.maximum.at(face_max, face_for_loop, uvs[:, 0])
    crosses_seam = (face_max - face_min) > 0.5
    shift_loop = crosses_seam[face_for_loop] & (uvs[:, 0] < 0.5)
    uvs[shift_loop, 0] += 1.0
    uv_layer.data.foreach_set("uv", uvs.ravel())
    return uv_layer


def almond_outline(cx, cz, rx, rz, segments=28):
    points = []
    for index in range(segments):
        angle = math.tau * index / segments
        c = math.cos(angle)
        sn = math.sin(angle)
        taper = 0.78 + 0.22 * abs(c)
        points.append((cx + rx * c, cz + rz * sn * taper))
    return points


def add_eye(arm, body, side, white, iris_mat, pupil_mat, glint_mat):
    x = side * 0.195
    eye_z = 1.12
    suffix = "L" if side < 0 else "R"
    eye_white = make_surface_patch(
        "Mesh_EyeWhite" + suffix, body, almond_outline(x, eye_z, 0.116, 0.15), white, 0.006)
    parent_to_bone(eye_white, arm, "ART_EyeRoot" + suffix)

    iris_bone = "ART_IrisL" if side < 0 else "ART_IrisR"
    iris_x = x - side * 0.005
    iris = make_surface_patch(
        "Mesh_Iris" + suffix, body, almond_outline(iris_x, eye_z - 0.012, 0.073, 0.104), iris_mat, 0.013)
    parent_to_bone(iris, arm, iris_bone)
    pupil = make_surface_patch(
        "Mesh_Pupil" + suffix, body, almond_outline(iris_x, eye_z - 0.019, 0.041, 0.076), pupil_mat, 0.020)
    parent_to_bone(pupil, arm, iris_bone)

    highlight = make_surface_patch(
        "Mesh_EyeGlint" + suffix,
        body,
        almond_outline(iris_x - 0.024, eye_z + 0.032, 0.019, 0.027, 20),
        glint_mat,
        0.026)
    parent_to_bone(highlight, arm, iris_bone)


def add_brow(arm, body, side, material):
    curve = bpy.data.curves.new("SplatBrowCurve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 12
    curve.bevel_depth = 0.018
    curve.bevel_resolution = 3
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(2)
    points = [(0.075, 1.355), (0.20, 1.39), (0.34, 1.355)]
    if side < 0:
        points = [(-x, z) for x, z in points][::-1]
    for point, (x, z) in zip(spline.bezier_points, points):
        point.co = (x, front_surface_y(body, x, z, 0.004), z)
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    brow = bpy.data.objects.new("SplatBrowCurve", curve)
    bpy.context.collection.objects.link(brow)
    curve.materials.append(material)
    bpy.ops.object.select_all(action="DESELECT")
    brow.select_set(True)
    bpy.context.view_layer.objects.active = brow
    bpy.ops.object.convert(target="MESH")
    brow = bpy.context.object
    brow.name = "ART_BrowL" if side < 0 else "ART_BrowR"
    parent_to_bone(brow, arm, "Bone_Head")


def make_render_camera(name, location, target):
    data = bpy.data.cameras.new(name)
    camera = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(camera)
    camera.location = location
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 2.75
    return camera


def create_character():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    os.makedirs(os.path.dirname(FBX_PATH), exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)

    purple = mat("Splat • soft amethyst", (0.47, 0.20, 0.82), 0.58, subsurface=0.08)
    coral = mat("Splat • coral boots", (0.96, 0.24, 0.36), 0.43, subsurface=0.025)
    teal = mat("Splat • expedition teal", (0.055, 0.34, 0.38), 0.82)
    sash_material = mat("Splat • woven map sash", (0.055, 0.31, 0.34), 0.82)
    sash_image = make_sash_texture()
    sash_shader = sash_material.node_tree.nodes.get("Principled BSDF")
    sash_image_node = sash_material.node_tree.nodes.new("ShaderNodeTexImage")
    sash_image_node.name = "Splat hand-painted map sash"
    sash_image_node.label = "Teal woven mapmaker band"
    sash_image_node.image = sash_image
    sash_material.node_tree.links.new(sash_image_node.outputs["Color"], sash_shader.inputs["Base Color"])
    map_paper = mat("Splat • rolled field chart", (0.56, 0.34, 0.14), 0.86)
    map_ink = mat("Splat • chart ink", (0.14, 0.09, 0.055), 0.88)
    brass = mat("Splat • warm starbud gold", (1.0, 0.78, 0.30), 0.5, metallic=0.02)
    friendly_brow = mat("Splat • soft lilac brow", (0.31, 0.15, 0.57), 0.64, subsurface=0.035)
    white = mat("Splat • warm eye white", (0.99, 0.97, 0.89), 0.28)
    iris = mat("Splat • fjord blue iris", (0.08, 0.49, 0.83), 0.24)
    pupil = mat("Splat • deep plum pupil", (0.035, 0.018, 0.095), 0.22)
    glint = mat("Splat • eye glint", (1.0, 1.0, 0.98), 0.18)
    cheek = mat("Splat • berry cheeks", (1.0, 0.38, 0.57), 0.57, subsurface=0.06)
    smile_mat = mat("Splat • soft smile", (0.34, 0.12, 0.49), 0.62, subsurface=0.04)
    mouth_mat = mat("Splat • mouth cavity", (0.045, 0.006, 0.075), 0.92)

    root = make_empty("ART_SplatVisual", (0, 0, 0))
    marker = make_empty("ART_SplatFace_v20", (0, 0, 0))
    marker.parent = root
    marker.location = (0, 0, 0)

    # One connected organic body and flipper arms; the head and body have no seam.
    mb_data = bpy.data.metaballs.new("Splat_UnifiedSilhouette")
    mb_data.resolution = 0.018
    mb_data.render_resolution = 0.012
    mb_data.threshold = 0.6
    mb = bpy.data.objects.new("Splat_UnifiedSilhouette", mb_data)
    bpy.context.collection.objects.link(mb)

    def blob(location, size, tilt=0.0):
        element = mb_data.elements.new()
        element.type = "ELLIPSOID"
        element.co = location
        element.radius = 1.0
        # Blender's ellipsoid size fields are diameters; the artist values above are radii.
        element.size_x, element.size_y, element.size_z = (axis * 2.0 for axis in size)
        element.rotation = Quaternion((0.0, 1.0, 0.0), math.radians(tilt))
        element.stiffness = 2.0

    # A sculpted pear silhouette: broad grounded lower torso, tapered crown, and
    # short flippers that remain part of the same continuous skin surface.
    blob((0.0, 0.0, 0.76), (0.70, 0.66, 0.70))
    blob((0.0, 0.0, 0.46), (0.68, 0.63, 0.49))
    blob((0.0, 0.0, 1.08), (0.48, 0.45, 0.34))
    # Integrate short paddle arms into the lower silhouette. Their centers sit
    # inside the torso so the skin flows through the shoulders instead of making nubs.
    blob((-0.54, 0.0, 0.60), (0.28, 0.22, 0.13), tilt=-30.0)
    blob((0.54, 0.0, 0.60), (0.28, 0.22, 0.13), tilt=30.0)
    bpy.context.view_layer.objects.active = mb
    mb.select_set(True)
    bpy.ops.object.convert(target="MESH")
    body = bpy.context.object
    body.name = "ART_SplatBody"
    body.data.name = "Splat_UnifiedOrganicSkin"
    body.data.materials.clear()
    body.data.materials.append(purple)
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    unwrap_body(body)
    skin_texture = make_skin_texture()
    shader = purple.node_tree.nodes.get("Principled BSDF")
    skin_image_node = purple.node_tree.nodes.new("ShaderNodeTexImage")
    skin_image_node.name = "Splat hand-painted skin"
    skin_image_node.label = "Soft amethyst watercolor"
    skin_image_node.image = skin_texture
    skin_image_node.location = (-320, 80)
    purple.node_tree.links.new(skin_image_node.outputs["Color"], shader.inputs["Base Color"])
    body_uv = body.data.uv_layers.get("Splat_Body_Cylindrical")
    if body_uv is not None:
        uv_node = purple.node_tree.nodes.new("ShaderNodeUVMap")
        uv_node.name = "Splat body UVs"
        uv_node.uv_map = body_uv.name
        uv_node.location = (-540, 80)
        purple.node_tree.links.new(uv_node.outputs["UV"], skin_image_node.inputs["Vector"])
    purple.node_tree.nodes.active = skin_image_node
    for poly in body.data.polygons:
        center = sum((body.data.vertices[index].co for index in poly.vertices), Vector()) / len(poly.vertices)
        poly.material_index = 0
        poly.use_smooth = True
    body.parent = root

    arm = create_bone_rig(root)
    skin_body(body, arm)
    # Make the skin modifier evaluate after geometry is parented under the visual root.
    body.parent = arm
    body.matrix_parent_inverse = arm.matrix_world.inverted()

    # Candidate keeps the map roll on the rear pack bone but omits the full-body belt.

    # Low coral boots sit just beyond the rear hem so the follow-camera silhouette
    # includes readable feet instead of ending as a featureless purple sphere.
    for side, suffix, bone_name in [(-1, "L", "Bone_LegL"), (1, "R", "Bone_LegR")]:
        boot = make_uv_sphere("ART_Boot" + suffix, (side * 0.31, 0.49, 0.12), (0.23, 0.27, 0.13), coral)
        parent_to_bone(boot, arm, bone_name)

    # Let the large glossy eyes carry the expression without sharp, dark brows.
    add_eye(arm, body, -1, white, iris, pupil, glint)
    add_eye(arm, body, 1, white, iris, pupil, glint)
    # Short, softly colored arches lift the expression without the sharp angled
    # brows that made earlier Unity captures look worried.
    # Candidate omits raised brow strips to keep the expression soft and integrated.

    add_open_smile(arm, body, smile_mat, cheek, white)

    # Carry one rolled map at the back-right hip, held by two small woven ties.
    map_angle = math.radians(148.0)
    map_outward = Vector((math.sin(map_angle), -math.cos(map_angle), 0.0))
    map_height = 0.68
    map_origin = Vector((map_outward.x * 2.0, map_outward.y * 2.0, map_height))
    map_hit, map_surface, map_normal, _map_face = body.ray_cast(map_origin, -map_outward)
    if not map_hit:
        raise RuntimeError("Splat map roll did not find the body surface")
    map_position = tuple(map_surface + map_normal * 0.07)
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.078, depth=0.40,
                                        location=map_position)
    map_roll = bpy.context.object
    map_roll.name = "ART_RolledFieldChart"
    map_roll.data.materials.append(map_paper)
    for polygon in map_roll.data.polygons:
        polygon.use_smooth = len(polygon.vertices) == 4
    parent_to_bone(map_roll, arm, "ART_MakerPack")

    # Two cloth ties keep the map roll attached to the sash.
    for index, z in enumerate((map_position[2] - 0.13, map_position[2] + 0.13)):
        bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
                                        location=(map_position[0], map_position[1], z),
                                        major_radius=0.078, minor_radius=0.008)
        tie = bpy.context.object
        tie.name = "ART_MapRollTie" + str(index)
        tie.data.materials.append(teal)
        for polygon in tie.data.polygons:
            polygon.use_smooth = True
        parent_to_bone(tie, arm, "ART_MakerPack")

    # A tiny gold starbud is the recurring hand-drawn navigation motif.
    antenna_curve = bpy.data.curves.new("SplatAntennaStemCurve", "CURVE")
    antenna_curve.dimensions = "3D"
    antenna_curve.resolution_u = 16
    antenna_curve.bevel_depth = 0.034
    antenna_curve.bevel_resolution = 4
    antenna_spline = antenna_curve.splines.new("BEZIER")
    antenna_spline.bezier_points.add(2)
    for point, co in zip(antenna_spline.bezier_points, [(0.0, 0.0, 1.40), (-0.08, -0.025, 1.60), (-0.23, -0.05, 1.78)]):
        point.co = co
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    antenna_obj = bpy.data.objects.new("SplatAntennaStem", antenna_curve)
    bpy.context.collection.objects.link(antenna_obj)
    antenna_curve.materials.append(purple)
    bpy.ops.object.select_all(action="DESELECT")
    antenna_obj.select_set(True)
    bpy.context.view_layer.objects.active = antenna_obj
    bpy.ops.object.convert(target="MESH")
    antenna_obj = bpy.context.object
    antenna_obj.name = "ART_AntennaStem"
    parent_to_bone(antenna_obj, arm, "Bone_Antenna")
    antenna_tip = add_star("ART_AntennaStarbud", (-0.23, -0.05, 1.78), 0.09, 0.06, brass, points=4)
    parent_to_bone(antenna_tip, arm, "Bone_Antenna")

    # The animator looks for these stable names; they are also explicit skin joints.
    bpy.context.view_layer.objects.active = arm

    # Studio render setup for production review; it is excluded from the FBX selection.
    world = bpy.data.worlds.new("Splat studio")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.055, 0.075, 0.13, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.55
    bpy.context.scene.world = world
    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 32
    bpy.context.scene.render.resolution_x = 900
    bpy.context.scene.render.resolution_y = 900
    bpy.context.scene.render.resolution_percentage = 100
    bpy.context.scene.render.image_settings.file_format = "PNG"
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.view_settings.view_transform = "AgX"

    def area_light(name, location, energy, size, color):
        data = bpy.data.lights.new(name, "AREA")
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        data.color = color
        obj = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(obj)
        obj.location = location
        obj.rotation_euler = (Vector((0, 0, 0.95)) - obj.location).to_track_quat("-Z", "Y").to_euler()
        return obj

    area_light("Softbox key", (3.0, -4.0, 4.2), 470, 4.0, (0.76, 0.85, 1.0))
    area_light("Warm fill", (-3.0, -2.0, 2.2), 270, 3.2, (1.0, 0.67, 0.49))
    area_light("Fjord rim", (0.0, 2.0, 3.1), 520, 3.0, (0.32, 0.70, 1.0))

    for view, loc, target in [
        ("front-three-quarter", (2.5, -5.8, 1.45), (0, 0, 1.0)),
        ("back-pack", (0, 5.8, 1.25), (0, 0, 1.0)),
    ]:
        camera = make_render_camera("PreviewCamera_" + view, loc, target)
        bpy.context.scene.camera = camera
        bpy.context.scene.render.filepath = os.path.join(PREVIEW_DIR, view + ".png")
        bpy.ops.render.render(write_still=True)
        bpy.data.objects.remove(camera, do_unlink=True)

    # Keep each generated blend source clean; the previous authored file is
    # already preserved outside the project when a risky trial needs rollback.
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    # Export every object under the character root; studio objects stay in the blend.
    bpy.ops.object.select_all(action="DESELECT")
    selected = [root] + list(root.children_recursive)
    for obj in set(selected):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        object_types={"ARMATURE", "MESH", "EMPTY"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_anim=False,
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        path_mode="AUTO",
    )
    print("SPLAT_ASSET", BLEND_PATH, FBX_PATH, sep="\n")


if __name__ == "__main__":
    create_character()
