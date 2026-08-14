from __future__ import annotations

import numpy as np

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.model import LodDto, MaterialDto, MorphDeltaDto, MorphDto, VertexColorDto, WeightDto
from ..version import EUEFormatVersion
from .chunks import iter_chunks


def read_material(ar: FArchiveReader) -> MaterialDto:
    return MaterialDto(
        material_name=ar.read_fstring(),
        material_path=ar.read_fstring() if ar.file_version >= EUEFormatVersion.SerializeMaterialPath else "",
        first_index=ar.read_int(),
        num_faces=ar.read_int(),
    )


def read_weight(ar: FArchiveReader) -> WeightDto:
    return WeightDto(
        bone_index=ar.read_short(),
        vertex_index=ar.read_int(),
        weight=ar.read_float(),
    )


def read_morph_delta(ar: FArchiveReader) -> MorphDeltaDto:
    return MorphDeltaDto(
        position=ar.read_float_vector(3),
        normals=ar.read_float_vector(3),
        vertex_index=ar.read_int(),
    )


def read_morph(ar: FArchiveReader) -> MorphDto:
    return MorphDto(
        name=ar.read_fstring(),
        deltas=ar.read_serialized_array(read_morph_delta),
    )


def read_vertex_color(ar: FArchiveReader) -> VertexColorDto:
    name = ar.read_fstring()
    count = ar.read_int()
    data = (np.array(ar.read_byte_vector(count * 4)).reshape(count, 4) / 255).astype(np.float32)
    return VertexColorDto(name, data)


def read_lod(ar: FArchiveReader) -> LodDto:
    data = LodDto(name=ar.read_fstring())
    lod_size = ar.read_int()
    ar = ar.chunk(lod_size)

    for header_name, array_size, section_ar in iter_chunks(ar):
        if header_name == "VERTICES":
            data.vertices = np.array(section_ar.read_float_vector(array_size * 3)).reshape(array_size, 3)
        elif header_name == "INDICES":
            data.indices = np.array(
                section_ar.read_int_vector(array_size),
                dtype=np.int32,
            ).reshape(array_size // 3, 3)
        elif header_name == "NORMALS":
            flattened = np.array(section_ar.read_float_vector(array_size * 4))
            data.normals = flattened.reshape(-1, 4)[:, 1:]
        elif header_name == "TANGENTS":
            section_ar.skip(array_size * 3 * 3)
        elif header_name == "VERTEXCOLORS":
            data.colors = [read_vertex_color(section_ar) for _ in range(array_size)]
        elif header_name == "TEXCOORDS":
            data.uvs = []
            for _ in range(array_size):
                count = section_ar.read_int()
                data.uvs.append(np.array(section_ar.read_float_vector(count * 2)).reshape(count, 2))
        elif header_name == "MATERIALS":
            data.materials = section_ar.read_array(array_size, read_material)
        elif header_name == "WEIGHTS":
            data.weights = section_ar.read_array(array_size, read_weight)
        elif header_name == "MORPHTARGETS":
            data.morphs = section_ar.read_array(array_size, read_morph)
        else:
            Log.warn(f"Unknown LOD Data: {header_name}")

    return data
