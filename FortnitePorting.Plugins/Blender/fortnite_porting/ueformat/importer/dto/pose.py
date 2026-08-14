from __future__ import annotations

from dataclasses import dataclass, field

import numpy as np
import numpy.typing as npt


@dataclass(slots=True)
class PoseCurveInfluenceDto:
    curve_index: int
    influence: float


@dataclass(slots=True)
class PoseKeyDto:
    bone_name: str
    position: npt.NDArray[np.floating]
    rotation: npt.NDArray[np.floating]
    scale: npt.NDArray[np.floating]


@dataclass(slots=True)
class PoseEntryDto:
    name: str
    keys: list[PoseKeyDto] = field(default_factory=list)
    curves: list[PoseCurveInfluenceDto] = field(default_factory=list)


@dataclass(slots=True)
class PoseDto:
    poses: list[PoseEntryDto] = field(default_factory=list)
    curve_names: list[str] = field(default_factory=list)
