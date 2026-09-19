from __future__ import annotations

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.pose import PoseCurveInfluenceDto, PoseDto, PoseEntryDto, PoseKeyDto
from .attributes import iter_attributes


def read_pose_key(ar: FArchiveReader) -> PoseKeyDto:
    return PoseKeyDto(
        bone_name=ar.read_fstring(),
        position=ar.read_float_vector(3),
        rotation=ar.read_float_vector(4),
        scale=ar.read_float_vector(3),
    )


def read_curve_influence(ar: FArchiveReader) -> PoseCurveInfluenceDto:
    return PoseCurveInfluenceDto(
        curve_index=ar.read_int(),
        influence=ar.read_float(),
    )


def read_pose_entry(ar: FArchiveReader) -> PoseEntryDto:
    return PoseEntryDto(
        name=ar.read_fstring(),
        keys=ar.read_serialized_array(read_pose_key),
        curves=ar.read_serialized_array(read_curve_influence),
    )


def read_pose(ar: FArchiveReader) -> PoseDto:
    data = PoseDto()
    for name, payload in iter_attributes(ar):
        match name:
            case "POSES":
                data.poses = payload.read_serialized_array(read_pose_entry)
            case "CURVES":
                data.curve_names = payload.read_serialized_array(lambda section: section.read_fstring())
            case _:
                Log.warn(f"Unknown attribute: {name}")
    return data
