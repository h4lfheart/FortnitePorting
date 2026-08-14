from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

import numpy as np
import numpy.typing as npt

from .skeleton import SkeletonDto


@dataclass(slots=True)
class VertexColorDto:
    name: str
    data: npt.NDArray[np.float32]


@dataclass(slots=True)
class MaterialDto:
    material_name: str
    material_path: str
    first_index: int
    num_faces: int


@dataclass(slots=True)
class WeightDto:
    bone_index: int
    vertex_index: int
    weight: float


@dataclass(slots=True)
class MorphDeltaDto:
    position: npt.NDArray[np.floating]
    normals: npt.NDArray[np.floating]
    vertex_index: int


@dataclass(slots=True)
class MorphDto:
    name: str
    deltas: list[MorphDeltaDto]


@dataclass(slots=True)
class LodDto:
    name: str
    vertices: npt.NDArray[np.floating] = field(default_factory=lambda: np.zeros(0))
    indices: npt.NDArray[np.int32] = field(default_factory=lambda: np.zeros(0, dtype=np.int32))
    normals: npt.NDArray[np.floating] = field(default_factory=lambda: np.zeros(0))
    colors: list[VertexColorDto] = field(default_factory=list)
    uvs: list[npt.NDArray[Any]] = field(default_factory=list)
    materials: list[MaterialDto] = field(default_factory=list)
    morphs: list[MorphDto] = field(default_factory=list)
    weights: list[WeightDto] = field(default_factory=list)


@dataclass(slots=True)
class CollisionDto:
    name: str
    vertices: npt.NDArray[np.floating[Any]]
    indices: npt.NDArray[np.int32]


@dataclass(slots=True)
class ModelDto:
    lods: list[LodDto] = field(default_factory=list)
    collisions: list[CollisionDto] = field(default_factory=list)
    skeleton: SkeletonDto | None = None
