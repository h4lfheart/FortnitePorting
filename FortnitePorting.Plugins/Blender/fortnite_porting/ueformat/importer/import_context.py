from __future__ import annotations

import gzip
from dataclasses import dataclass
from pathlib import Path

from bpy.types import Action, Object

from ..logging import Log
from ..options import UEAnimOptions, UEFormatOptions, UEModelOptions, UEPoseOptions
from .archive.reader import FArchiveReader
from .constants import ANIM_IDENTIFIER, MAGIC, MODEL_IDENTIFIER, POSE_IDENTIFIER
from .create.anim import create_anim
from .create.model import create_model
from .create.pose import create_pose
from .create.space import to_blender_space
from .dto.anim import AnimDto
from .dto.model import ModelDto
from .dto.pose import PoseDto
from .version import EUEFormatVersion


@dataclass(slots=True)
class UEFormatHeader:
    identifier: str
    file_version: EUEFormatVersion
    object_name: str
    object_path: str


def parse_header(ar: FArchiveReader) -> tuple[UEFormatHeader, FArchiveReader]:
    magic = ar.read_string(len(MAGIC))
    if magic != MAGIC:
        raise ValueError("Invalid magic")

    identifier = ar.read_fstring()
    file_version = EUEFormatVersion(int.from_bytes(ar.read_byte(), byteorder="big"))
    if file_version > EUEFormatVersion.LatestVersion:
        msg = f"File Version {file_version} is not supported for this version of the importer."
        Log.error(msg)
        raise ValueError(msg)

    object_name = ar.read_fstring()
    object_path = ""
    if file_version >= EUEFormatVersion.AttributeFormatRestructure:
        object_path = ar.read_fstring()

    Log.info(f"Importing {object_name}")

    body = ar
    is_compressed = ar.read_bool()
    if is_compressed:
        compression_type = ar.read_fstring()
        uncompressed_size = ar.read_int()
        _compressed_size = ar.read_int()

        if compression_type == "GZIP":
            body = FArchiveReader(gzip.decompress(ar.read_to_end()))
        elif compression_type == "ZSTD":
            from .. import zstd_decompressor

            body = FArchiveReader(
                zstd_decompressor.decompress(ar.read_to_end(), uncompressed_size),
            )
        else:
            msg = f"Unknown Compression Type: {compression_type}"
            Log.error(msg)
            raise ValueError(msg)

    body.file_version = file_version
    return UEFormatHeader(identifier, file_version, object_name, object_path), body


def parse_body(header: UEFormatHeader, ar: FArchiveReader) -> ModelDto | AnimDto | PoseDto:
    if header.file_version >= EUEFormatVersion.AttributeFormatRestructure:
        from . import deserialize as parser
    else:
        from . import legacy as parser

    if header.identifier == MODEL_IDENTIFIER:
        return parser.read_model(ar)
    if header.identifier == ANIM_IDENTIFIER:
        return parser.read_anim(ar)
    if header.identifier == POSE_IDENTIFIER:
        return parser.read_pose(ar)

    msg = f"Unknown identifier: {header.identifier}"
    Log.error(msg)
    raise ValueError(msg)


class UEFormatImport:
    def __init__(self, options: UEFormatOptions) -> None:
        self.options = options

    def import_file(self, path: str | Path) -> Object | Action | None:
        created, _ = self.import_file_with_dto(path)
        return created

    def import_file_with_dto(
        self, path: str | Path,
    ) -> tuple[Object | Action | None, ModelDto | AnimDto | PoseDto]:
        path = path if isinstance(path, Path) else Path(path)
        Log.time_start(f"Import {path}")
        with path.open("rb") as file:
            result = self.import_data_with_dto(file.read())
        Log.time_end(f"Import {path}")
        return result

    def import_data(self, data: bytes) -> Object | Action | None:
        created, _ = self.import_data_with_dto(data)
        return created

    def import_data_with_dto(
        self, data: bytes,
    ) -> tuple[Object | Action | None, ModelDto | AnimDto | PoseDto]:
        with FArchiveReader(data) as ar:
            return self.import_data_by_reader_with_dto(ar)

    def import_data_by_reader(self, ar: FArchiveReader) -> Object | Action | None:
        created, _ = self.import_data_by_reader_with_dto(ar)
        return created

    def import_data_by_reader_with_dto(
        self, ar: FArchiveReader,
    ) -> tuple[Object | Action | None, ModelDto | AnimDto | PoseDto]:
        header, body = parse_header(ar)
        dto = parse_body(header, body)
        to_blender_space(dto, header.file_version, self.options.scale_factor)

        if isinstance(dto, ModelDto):
            assert isinstance(self.options, UEModelOptions)
            return create_model(dto, self.options, header.object_name), dto
        if isinstance(dto, AnimDto):
            assert isinstance(self.options, UEAnimOptions)
            return create_anim(dto, self.options, header.object_name), dto
        if isinstance(dto, PoseDto):
            assert isinstance(self.options, UEPoseOptions)
            create_pose(dto, self.options, header.object_name)
            return None, dto

        raise ValueError(f"Unsupported DTO: {type(dto)}")
