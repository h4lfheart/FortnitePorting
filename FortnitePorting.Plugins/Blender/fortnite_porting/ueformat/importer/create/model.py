from __future__ import annotations

from typing import cast

import bpy
from bpy.types import ByteColorAttribute, Object
from mathutils import Vector

from ...options import UEModelOptions
from ..dto.model import ModelDto
from .skeleton import create_skeleton


def create_model(dto: ModelDto, options: UEModelOptions, name: str) -> Object | None:
    return_object = None
    target_lod = min(options.target_lod, len(dto.lods) - 1) if dto.lods else 0
    created_lods: list[Object] = []

    for index, lod in enumerate(dto.lods):
        if index != target_lod:
            continue

        lod_name = f"{name}_{lod.name}"
        mesh_data = bpy.data.meshes.new(lod_name)
        mesh_data.from_pydata(lod.vertices, [], lod.indices)

        mesh_object = bpy.data.objects.new(lod_name, mesh_data)
        return_object = mesh_object
        if options.link:
            bpy.context.collection.objects.link(mesh_object)

        if len(lod.normals) > 0:
            mesh_data.polygons.foreach_set("use_smooth", [True] * len(mesh_data.polygons))
            mesh_data.normals_split_custom_set_from_vertices(lod.normals)
            if bpy.app.version < (4, 1, 0):
                mesh_data.use_auto_smooth = True

        if lod.weights and dto.skeleton and dto.skeleton.bones:
            for weight in lod.weights:
                bone_name = dto.skeleton.bones[weight.bone_index].name
                vertex_group = mesh_object.vertex_groups.get(bone_name)
                if not vertex_group:
                    vertex_group = mesh_object.vertex_groups.new(name=bone_name)
                vertex_group.add([weight.vertex_index], weight.weight, "ADD")

        if options.import_morph_targets and lod.morphs:
            if not mesh_object.data.shape_keys:
                mesh_object.shape_key_add(name="Basis", from_mix=False)
            for morph in lod.morphs:
                key = mesh_object.shape_key_add(from_mix=False)
                key.name = morph.name
                key.interpolation = "KEY_LINEAR"
                for delta in morph.deltas:
                    key.data[delta.vertex_index].co += Vector(delta.position)
                key.value = 0

        def squish(array):
            return array.reshape(array.size)

        vertices = [vertex for polygon in mesh_data.polygons for vertex in polygon.vertices]
        for color_info in lod.colors:
            remapped = color_info.data[vertices]
            vertex_color = cast(
                ByteColorAttribute,
                mesh_data.color_attributes.new(domain="CORNER", type="BYTE_COLOR", name=color_info.name),
            )
            vertex_color.data.foreach_set("color", squish(remapped))

        for i, uvs in enumerate(lod.uvs):
            remapped = uvs[vertices]
            uv_layer = mesh_data.uv_layers.new(name="UV" + str(i))
            uv_layer.data.foreach_set("uv", squish(remapped))

        if lod.materials:
            for i, material in enumerate(lod.materials):
                mat = bpy.data.materials.get(material.material_name)
                if mat is None:
                    mat = bpy.data.materials.new(name=material.material_name)
                mesh_data.materials.append(mat)
                start_face_index = material.first_index // 3
                end_face_index = start_face_index + material.num_faces
                for face_index in range(start_face_index, end_face_index):
                    mesh_data.polygons[face_index].material_index = i

        created_lods.append(mesh_object)

    skeleton_object = create_skeleton(dto, options, name, created_lods)
    if skeleton_object is not None:
        return_object = skeleton_object

    if options.import_collision and dto.collisions:
        for index, collision in enumerate(dto.collisions):
            collision_name = index if collision.name == "None" else collision.name
            collision_object_name = f"UCX_{name}_{collision_name}"
            collision_mesh_data = bpy.data.meshes.new(collision_object_name)
            collision_mesh_data.from_pydata(collision.vertices, [], collision.indices)
            collision_mesh_object = bpy.data.objects.new(collision_object_name, collision_mesh_data)
            collision_mesh_object.display_type = "WIRE"
            if created_lods:
                collision_mesh_object.parent = created_lods[0]
            if options.link:
                bpy.context.collection.objects.link(collision_mesh_object)

    return return_object
