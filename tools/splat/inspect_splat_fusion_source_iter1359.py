from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/20261030_224550_splat-iter1337_01a1900f/rigged_character.glb"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(MODEL))
for obj in bpy.context.scene.objects:
    if obj.type != "MESH":
        continue
    coordinates = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    bounds = tuple((round(min(v[axis] for v in coordinates), 4),
                    round(max(v[axis] for v in coordinates), 4)) for axis in range(3))
    print("FUSION_SOURCE", obj.name, "verts", len(obj.data.vertices), "faces", len(obj.data.polygons),
          "dims", tuple(round(v, 4) for v in obj.dimensions),
          "bounds_xyz", bounds,
          "location", tuple(round(v, 4) for v in obj.location),
          "scale", tuple(round(v, 4) for v in obj.scale),
          "groups", len(obj.vertex_groups),
          "materials", [material.name if material else None for material in obj.data.materials])
    for material in obj.data.materials:
        if material is None or not material.use_nodes:
            continue
        for node in material.node_tree.nodes:
            if node.type == "TEX_IMAGE" and node.image:
                print("FUSION_TEXTURE", material.name, node.image.name, node.image.filepath,
                      node.image.size[:], node.image.channels)
