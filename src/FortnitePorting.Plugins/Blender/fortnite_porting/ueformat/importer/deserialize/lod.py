from __future__ import annotations

import numpy as np

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.model import LodDto, MaterialDto, MorphDeltaDto, MorphDto, VertexColorDto, WeightDto
from .attributes import iter_attributes


def read_material(ar: FArchiveReader) -> MaterialDto:
    return MaterialDto(
        material_name=ar.read_fstring(),
        material_path=ar.read_fstring(),
        first_index=ar.read_int(),
        num_faces=ar.read_int(),
    )


def read_weight(ar: FArchiveReader) -> WeightDto:
    return WeightDto(
        bone_index=ar.read_ushort(),
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


def _read_vector_array(ar: FArchiveReader, width: int) -> np.ndarray:
    count = ar.read_int()
    return np.array(ar.read_float_vector(count * width)).reshape(count, width)


def read_lod(ar: FArchiveReader) -> LodDto:
    data = LodDto(name=ar.read_fstring())
    for name, payload in iter_attributes(ar):
        match name:
            case "VERTICES":
                data.vertices = _read_vector_array(payload, 3)
            case "NORMALS":
                count = payload.read_int()
                flattened = np.array(payload.read_float_vector(count * 4))
                data.normals = flattened.reshape(-1, 4)[:, 1:]
            case "TANGENTS":
                pass
            case "TEXCOORDS":
                data.uvs = []
                for _ in range(payload.read_int()):
                    payload.read_fstring()
                    uv_count = payload.read_int()
                    data.uvs.append(np.array(payload.read_float_vector(uv_count * 2)).reshape(uv_count, 2))
            case "INDICES":
                count = payload.read_int()
                data.indices = np.array(payload.read_int_vector(count), dtype=np.int32).reshape(count // 3, 3)
            case "VERTEXCOLORS":
                data.colors = payload.read_serialized_array(read_vertex_color)
            case "MATERIALS":
                data.materials = payload.read_serialized_array(read_material)
            case "WEIGHTS":
                data.weights = payload.read_serialized_array(read_weight)
            case "MORPHTARGETS":
                data.morphs = payload.read_serialized_array(read_morph)
            case _:
                Log.warn(f"Unknown attribute: {name}")
    return data
