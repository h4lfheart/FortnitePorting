from __future__ import annotations

import numpy as np

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.model import LodDto, ModelDto, VertexColorDto
from ..dto.skeleton import SkeletonDto
from ..version import EUEFormatVersion
from .chunks import iter_chunks
from .lod import read_material, read_morph, read_vertex_color, read_weight
from .model import read_collision
from .skeleton import read_bone, read_socket


def read_model(ar: FArchiveReader) -> ModelDto:
    data = ModelDto()
    data.skeleton = SkeletonDto()
    lod = LodDto(name="LOD0")

    for header_name, array_size, section_ar in iter_chunks(ar):
        if header_name == "VERTICES":
            lod.vertices = np.array(section_ar.read_float_vector(array_size * 3)).reshape(array_size, 3)
        elif header_name == "INDICES":
            lod.indices = np.array(
                section_ar.read_int_vector(array_size),
                dtype=np.int32,
            ).reshape(array_size // 3, 3)
        elif header_name == "NORMALS":
            if section_ar.file_version >= EUEFormatVersion.SerializeBinormalSign:
                flattened = np.array(section_ar.read_float_vector(array_size * 4))
                lod.normals = flattened.reshape(-1, 4)[:, 1:]
            else:
                lod.normals = np.array(section_ar.read_float_vector(array_size * 3)).reshape(array_size, 3)
        elif header_name == "TANGENTS":
            section_ar.skip(array_size * 3 * 3)
        elif header_name == "VERTEXCOLORS":
            if section_ar.file_version >= EUEFormatVersion.AddMultipleVertexColors:
                lod.colors = [read_vertex_color(section_ar) for _ in range(array_size)]
            else:
                lod.colors = [
                    VertexColorDto(
                        "COL0",
                        (np.array(section_ar.read_byte_vector(array_size * 4)).reshape(array_size, 4) / 255).astype(
                            np.float32,
                        ),
                    ),
                ]
        elif header_name == "TEXCOORDS":
            lod.uvs = []
            for _ in range(array_size):
                count = section_ar.read_int()
                lod.uvs.append(np.array(section_ar.read_float_vector(count * 2)).reshape(count, 2))
        elif header_name == "MATERIALS":
            lod.materials = section_ar.read_array(array_size, read_material)
        elif header_name == "WEIGHTS":
            lod.weights = section_ar.read_array(array_size, read_weight)
        elif header_name == "MORPHTARGETS":
            lod.morphs = section_ar.read_array(array_size, read_morph)
        elif header_name == "BONES":
            data.skeleton.bones = section_ar.read_array(array_size, read_bone)
        elif header_name == "SOCKETS":
            data.skeleton.sockets = section_ar.read_array(array_size, read_socket)
        elif header_name == "COLLISION":
            data.collisions = section_ar.read_array(array_size, read_collision)
        else:
            Log.warn(f"Unknown Data: {header_name}")

    data.lods.append(lod)
    return data
