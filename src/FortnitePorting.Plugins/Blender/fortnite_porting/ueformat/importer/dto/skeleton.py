from __future__ import annotations

from dataclasses import dataclass, field

import numpy as np
import numpy.typing as npt


@dataclass(slots=True)
class BoneDto:
    name: str
    parent_index: int
    position: npt.NDArray[np.floating]
    rotation: npt.NDArray[np.floating]
    scale: npt.NDArray[np.floating] = field(
        default_factory=lambda: np.array((1.0, 1.0, 1.0)),
    )


@dataclass(slots=True)
class SocketDto:
    name: str
    parent_name: str
    position: npt.NDArray[np.floating]
    rotation: npt.NDArray[np.floating]
    scale: npt.NDArray[np.floating]


@dataclass(slots=True)
class VirtualBoneDto:
    source_name: str
    target_name: str
    virtual_name: str


@dataclass(slots=True)
class SkeletonDto:
    skeleton_path: str = ""
    bones: list[BoneDto] = field(default_factory=list)
    sockets: list[SocketDto] = field(default_factory=list)
    virtual_bones: list[VirtualBoneDto] = field(default_factory=list)
