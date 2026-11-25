import bpy
import os
import sys


def arg_after(flag):
    args = sys.argv
    return args[args.index(flag) + 1]


source = os.path.abspath(arg_after("--source"))
destination = os.path.abspath(arg_after("--destination"))
os.makedirs(os.path.dirname(destination), exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=source)
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if not meshes:
    raise RuntimeError("Orbit Postbox GLB imported with no mesh objects")

for obj in meshes:
    parent_name = obj.parent.name if obj.parent else "<none>"
    print("ORBIT_POSTBOX_OBJECT=" + obj.name + " PARENT=" + parent_name
          + " LOCAL_ROT=" + ",".join(f"{v:.6f}" for v in obj.rotation_euler)
          + " WORLD_ROT=" + ",".join(f"{v:.6f}" for v in obj.matrix_world.to_euler()))

for obj in bpy.context.scene.objects:
    obj.select_set(False)
for obj in meshes:
    obj.select_set(True)
    obj.data.validate(verbose=False, clean_customdata=False)
    obj.data.update()
bpy.context.view_layer.objects.active = meshes[0]
# Bake the imported GLTF node hierarchy into the mesh before exporting. This
# keeps the design's upright axis and front-panel normal when FBX drops the
# selected mesh's non-exported parent nodes.
for obj in meshes:
    world = obj.matrix_world.copy()
    obj.parent = None
    obj.matrix_world = world
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
bpy.context.view_layer.update()
bpy.context.view_layer.objects.active = meshes[0]

bounds = [[], [], []]
for obj in meshes:
    for corner in obj.bound_box:
        world = obj.matrix_world @ __import__("mathutils").Vector(corner)
        for axis in range(3):
            bounds[axis].append(world[axis])
size = [max(values) - min(values) for values in bounds]
print("ORBIT_POSTBOX_BAKED_BOUNDS_XYZ=" + ",".join(f"{value:.6f}" for value in size))
print("ORBIT_POSTBOX_MESH_OBJECTS=" + str(len(meshes)))
print("ORBIT_POSTBOX_TRIANGLES=" + str(sum(len(poly.vertices) - 2 for obj in meshes for poly in obj.data.polygons)))

bpy.ops.export_scene.fbx(
    filepath=destination,
    use_selection=True,
    object_types={"MESH"},
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_UNITS",
    # Bake the Z-up GLTF basis into vertices so Unity's Y-up FBX importer does
    # not reinterpret the mailbox's upright body/front panel as a different axis.
    bake_space_transform=True,
    axis_forward="-Z",
    axis_up="Y",
    path_mode="COPY",
    embed_textures=False,
    add_leaf_bones=False,
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
)
print("ORBIT_POSTBOX_FBX=" + destination)
