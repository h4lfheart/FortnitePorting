from __future__ import annotations

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.pose import PoseCurveInfluenceDto, PoseDto, PoseEntryDto, PoseKeyDto


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

    while not ar.eof():
        section_name = ar.read_fstring()
        array_size = ar.read_int()
        byte_size = ar.read_int()

        match section_name:
            case "POSES":
                data.poses = ar.read_array(array_size, read_pose_entry)
            case "CURVES":
                data.curve_names = ar.read_array(array_size, lambda section: section.read_fstring())
            case _:
                Log.warn(f"Unknown Pose Data: {section_name}")
                ar.skip(byte_size)

    return data
