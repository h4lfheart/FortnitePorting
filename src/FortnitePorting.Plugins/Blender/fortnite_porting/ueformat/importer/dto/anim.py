from __future__ import annotations

from dataclasses import dataclass, field
from enum import IntEnum

import numpy as np
import numpy.typing as npt


class EAdditiveAnimationType(IntEnum):
    AAT_None = 0
    AAT_LocalSpaceBase = 1
    AAT_RotationOffsetMeshSpace = 2
    AAT_MAX = 3


class EAdditiveBasePoseType(IntEnum):
    ABPT_None = 0
    ABPT_RefPose = 1
    ABPT_AnimScaled = 2
    ABPT_AnimFrame = 3
    ABPT_LocalAnimFrame = 4
    ABPT_MAX = 5


@dataclass(slots=True)
class AnimMetadataDto:
    num_frames: int
    frames_per_second: float
    ref_pose_path: str = ""
    additive_anim_type: EAdditiveAnimationType = EAdditiveAnimationType.AAT_None
    ref_pose_type: EAdditiveBasePoseType = EAdditiveBasePoseType.ABPT_None
    ref_frame_index: int = 0


@dataclass(slots=True)
class VectorKeyDto:
    frame: int
    value: npt.NDArray[np.floating]


@dataclass(slots=True)
class QuatKeyDto:
    frame: int
    value: npt.NDArray[np.floating]


@dataclass(slots=True)
class FloatKeyDto:
    frame: int
    value: float


@dataclass(slots=True)
class TrackDto:
    name: str
    position_keys: list[VectorKeyDto]
    rotation_keys: list[QuatKeyDto]
    scale_keys: list[VectorKeyDto]


@dataclass(slots=True)
class CurveDto:
    name: str
    keys: list[FloatKeyDto]


@dataclass(slots=True)
class AnimDto:
    metadata: AnimMetadataDto | None = None
    tracks: list[TrackDto] = field(default_factory=list)
    curves: list[CurveDto] = field(default_factory=list)
