from __future__ import annotations

from ...logging import Log
from ..archive.reader import FArchiveReader
from ..dto.anim import (
    AnimDto,
    AnimMetadataDto,
    CurveDto,
    EAdditiveAnimationType,
    EAdditiveBasePoseType,
    FloatKeyDto,
    QuatKeyDto,
    TrackDto,
    VectorKeyDto,
)
from ..version import EUEFormatVersion


def read_vector_key(ar: FArchiveReader) -> VectorKeyDto:
    return VectorKeyDto(frame=ar.read_int(), value=ar.read_float_vector(3))


def read_quat_key(ar: FArchiveReader) -> QuatKeyDto:
    return QuatKeyDto(frame=ar.read_int(), value=ar.read_float_vector(4))


def read_float_key(ar: FArchiveReader) -> FloatKeyDto:
    return FloatKeyDto(frame=ar.read_int(), value=ar.read_float())


def read_track(ar: FArchiveReader) -> TrackDto:
    return TrackDto(
        name=ar.read_fstring(),
        position_keys=ar.read_serialized_array(read_vector_key),
        rotation_keys=ar.read_serialized_array(read_quat_key),
        scale_keys=ar.read_serialized_array(read_vector_key),
    )


def read_curve(ar: FArchiveReader) -> CurveDto:
    return CurveDto(
        name=ar.read_fstring(),
        keys=ar.read_serialized_array(read_float_key),
    )


def read_metadata(ar: FArchiveReader) -> AnimMetadataDto:
    return AnimMetadataDto(
        num_frames=ar.read_int(),
        frames_per_second=ar.read_float(),
        ref_pose_path=ar.read_fstring(),
        additive_anim_type=EAdditiveAnimationType(int.from_bytes(ar.read_byte(), byteorder="little")),
        ref_pose_type=EAdditiveBasePoseType(int.from_bytes(ar.read_byte(), byteorder="little")),
        ref_frame_index=ar.read_int(),
    )


def read_anim(ar: FArchiveReader) -> AnimDto:
    data = AnimDto()

    if ar.file_version < EUEFormatVersion.SerializeAssetMetadata:
        data.metadata = AnimMetadataDto(num_frames=ar.read_int(), frames_per_second=ar.read_float())

    while not ar.eof():
        section_name = ar.read_fstring()
        array_size = ar.read_int()
        byte_size = ar.read_int()

        match section_name:
            case "METADATA":
                data.metadata = read_metadata(ar)
            case "TRACKS":
                data.tracks = ar.read_array(array_size, read_track)
            case "CURVES":
                data.curves = ar.read_array(array_size, read_curve)
            case _:
                Log.warn(f"Unknown Animation Data: {section_name}")
                ar.skip(byte_size)

    return data
