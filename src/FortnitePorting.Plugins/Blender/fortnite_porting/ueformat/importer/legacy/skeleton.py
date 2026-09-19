from __future__ import annotations

import numpy as np

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.skeleton import BoneDto, SkeletonDto, SocketDto, VirtualBoneDto
from .chunks import iter_chunks

_UNIT_SCALE = np.array((1.0, 1.0, 1.0))


def read_bone(ar: FArchiveReader) -> BoneDto:
    return BoneDto(
        name=ar.read_fstring(),
        parent_index=ar.read_int(),
        position=ar.read_float_vector(3),
        rotation=ar.read_float_vector(4),
        scale=_UNIT_SCALE.copy(),
    )


def read_socket(ar: FArchiveReader) -> SocketDto:
    return SocketDto(
        name=ar.read_fstring(),
        parent_name=ar.read_fstring(),
        position=ar.read_float_vector(3),
        rotation=ar.read_float_vector(4),
        scale=ar.read_float_vector(3),
    )


def read_virtual_bone(ar: FArchiveReader) -> VirtualBoneDto:
    return VirtualBoneDto(
        source_name=ar.read_fstring(),
        target_name=ar.read_fstring(),
        virtual_name=ar.read_fstring(),
    )


def read_skeleton(ar: FArchiveReader) -> SkeletonDto:
    data = SkeletonDto()
    for section_name, array_size, section_ar in iter_chunks(ar):
        match section_name:
            case "METADATA":
                data.skeleton_path = section_ar.read_fstring()
            case "BONES":
                data.bones = section_ar.read_array(array_size, read_bone)
            case "SOCKETS":
                data.sockets = section_ar.read_array(array_size, read_socket)
            case "VIRTUALBONES":
                data.virtual_bones = section_ar.read_array(array_size, read_virtual_bone)
            case _:
                Log.warn(f"Unknown Skeleton Data: {section_name}")
    return data
