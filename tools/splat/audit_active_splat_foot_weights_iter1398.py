import bpy

MODEL = r"E:\Users\thoma\git\space-and-crafts-3\Assets\SpaceAndCrafts\Resources\Characters\Splat_Rigged_Candidate_Iter1137.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=MODEL, automatic_bone_orientation=False)
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
print("ITER1398_ACTIVE_SPLAT_WEIGHT_AUDIT_START")
print(f"model={MODEL} meshes={len(meshes)} armatures={len(armatures)}")
for armature in armatures:
    print(f"armature={armature.name!r} bones={[bone.name for bone in armature.data.bones]}")
for obj in meshes:
    mesh = obj.data
    groups = {group.index: group.name for group in obj.vertex_groups}
    selected = {index: [] for index, name in groups.items()
                if any(tag in name.lower() for tag in ("foot", "ankle", "leg"))}
    for vertex in mesh.vertices:
        for membership in vertex.groups:
            if membership.group in selected:
                selected[membership.group].append(membership.weight)
    print(f"mesh={obj.name!r} vertices={len(mesh.vertices)} vertex_groups={len(groups)}")
    for index, weights in selected.items():
        name = groups[index]
        if not weights:
            print(f"group={name!r} assigned_vertices=0")
            continue
        weights.sort()
        print(
            f"group={name!r} assigned_vertices={len(weights)} "
            f"gt_010={sum(value >= .10 for value in weights)} "
            f"gt_025={sum(value >= .25 for value in weights)} "
            f"gt_055={sum(value >= .55 for value in weights)} "
            f"gt_075={sum(value >= .75 for value in weights)} "
            f"min={weights[0]:.4f} median={weights[len(weights)//2]:.4f} max={weights[-1]:.4f}"
        )
print("ITER1398_ACTIVE_SPLAT_WEIGHT_AUDIT_END")
