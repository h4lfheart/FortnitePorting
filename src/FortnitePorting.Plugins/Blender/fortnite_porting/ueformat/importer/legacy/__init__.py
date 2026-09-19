"""Frozen pre-AttributeFormatRestructure parsers.

Do not add features here. Call from import_context.py only; touch only for proven
regressions in old-file import.
"""

from __future__ import annotations

from ..archive.reader import FArchiveReader
from ..dto.model import ModelDto
from ..version import EUEFormatVersion
from .anim import read_anim
from .pose import read_pose


def read_model(ar: FArchiveReader) -> ModelDto:
    if ar.file_version >= EUEFormatVersion.LevelOfDetailFormatRestructure:
        from .model import read_model as read_chunked_model

        return read_chunked_model(ar)

    from .model_pre_lod import read_model as read_flat_model

    return read_flat_model(ar)
