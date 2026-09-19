from __future__ import annotations

import numpy as np

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.model import CollisionDto, ModelDto
from .lod import read_lod
from .skeleton import read_skeleton


def read_collision(ar: FArchiveReader) -> CollisionDto:
    name = ar.read_fstring()
    vertices_count = ar.read_int()
    vertices = np.array(ar.read_float_vector(vertices_count * 3)).reshape(vertices_count, 3)
    indices_count = ar.read_int()
    indices = np.array(ar.read_int_vector(indices_count), dtype=np.int32).reshape(indices_count // 3, 3)
    return CollisionDto(name=name, vertices=vertices, indices=indices)


def read_model(ar: FArchiveReader) -> ModelDto:
    data = ModelDto()

    while not ar.eof():
        section_name = ar.read_fstring()
        array_size = ar.read_int()
        byte_size = ar.read_int()

        match section_name:
            case "LODS":
                data.lods = ar.read_array(array_size, read_lod)
            case "SKELETON":
                data.skeleton = read_skeleton(ar.chunk(byte_size))
            case "COLLISION":
                data.collisions = ar.read_array(array_size, read_collision)
            case _:
                Log.warn(f"Unknown Model Data: {section_name}")
                ar.skip(byte_size)

    return data
