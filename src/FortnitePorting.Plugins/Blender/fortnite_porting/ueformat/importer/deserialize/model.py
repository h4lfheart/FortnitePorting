from __future__ import annotations

import numpy as np

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.model import CollisionDto, ModelDto
from .attributes import iter_attributes
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
    for name, payload in iter_attributes(ar):
        match name:
            case "LODS":
                data.lods = payload.read_serialized_array(read_lod)
            case "SKELETON":
                data.skeleton = read_skeleton(payload)
            case "COLLISION":
                data.collisions = payload.read_serialized_array(read_collision)
            case _:
                Log.warn(f"Unknown attribute: {name}")
    return data
