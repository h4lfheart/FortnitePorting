from __future__ import annotations

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.skeleton import BoneDto, SkeletonDto, SocketDto, VirtualBoneDto
from .attributes import iter_attributes


def read_bone(ar: FArchiveReader) -> BoneDto:
    return BoneDto(
        name=ar.read_fstring(),
        parent_index=ar.read_int(),
        position=ar.read_float_vector(3),
        rotation=ar.read_float_vector(4),
        scale=ar.read_float_vector(3),
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
    for name, payload in iter_attributes(ar):
        match name:
            case "METADATA":
                data.skeleton_path = payload.read_fstring()
            case "BONES":
                data.bones = payload.read_serialized_array(read_bone)
            case "SOCKETS":
                data.sockets = payload.read_serialized_array(read_socket)
            case "VIRTUALBONES":
                data.virtual_bones = payload.read_serialized_array(read_virtual_bone)
            case _:
                Log.warn(f"Unknown attribute: {name}")
    return data
