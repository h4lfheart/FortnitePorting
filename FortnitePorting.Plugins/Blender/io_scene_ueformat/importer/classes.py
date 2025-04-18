from __future__ import annotations

from dataclasses import dataclass, field
from enum import IntEnum, auto
from typing import TYPE_CHECKING, Any

import numpy as np
import numpy.typing as npt
from mathutils import Quaternion, Vector

from ..logging import Log

if TYPE_CHECKING:
    from io_scene_ueformat.importer.reader import FArchiveReader


MAGIC = "UEFORMAT"
MODEL_IDENTIFIER = "UEMODEL"
ANIM_IDENTIFIER = "UEANIM"


class EUEFormatVersion(IntEnum):
    BeforeCustomVersionWasAdded = 0
    SerializeBinormalSign = 1
    AddMultipleVertexColors = 2
    AddConvexCollisionGeom = 3
    LevelOfDetailFormatRestructure = 4
    SerializeVirtualBones = 5
    SerializeMaterialPath = 6
    SerializeAssetMetadata = 7
    PreserveOriginalTransforms = 8

    VersionPlusOne = auto()
    LatestVersion = VersionPlusOne - 1


@dataclass(slots=True)
class UEModel:
    lods: list[UEModelLOD] = field(default_factory=list)
    collisions: list[ConvexCollision] = field(default_factory=list)
    skeleton: UEModelSkeleton | None = None
    # physics = None  # noqa: ERA001

    @classmethod
    def from_archive(
        cls,
        ar: FArchiveReader,
        scale: float,
    ) -> UEModel:
        data = cls()

        while not ar.eof():
            section_name = ar.read_fstring()
            array_size = ar.read_int()
            byte_size = ar.read_int()

            match section_name:
                case "LODS":
                    data.lods = [UEModelLOD.from_archive(ar, scale) for _ in range(array_size)]
                case "SKELETON":
                    data.skeleton = UEModelSkeleton.from_archive(ar.chunk(byte_size), scale)
                case "COLLISION":
                    data.collisions = ar.read_array(
                        array_size,
                        lambda ar: ConvexCollision.from_archive(ar, scale),
                    )
                case _:
                    Log.warn(f"Unknown Section Data: {section_name}")
                    ar.skip(byte_size)
        return data


@dataclass(slots=True)
class UEModelLOD:
    name: str
    vertices: npt.NDArray[np.floating] = field(default_factory=lambda: np.zeros(0))
    indices: npt.NDArray[np.int32] = field(default_factory=lambda: np.zeros(0, dtype=np.int32))
    normals: npt.NDArray[np.floating] = field(default_factory=lambda: np.zeros(0))
    tangents: list = field(default_factory=list)
    colors: list[VertexColor] = field(default_factory=list)
    uvs: list[npt.NDArray[Any]] = field(default_factory=list)
    materials: list[Material] = field(default_factory=list)
    morphs: list[MorphTarget] = field(default_factory=list)
    weights: list[Weight] = field(default_factory=list)

    @classmethod
    def from_archive(
        cls,
        ar: FArchiveReader,
        scale: float,
    ) -> UEModelLOD:
        data = cls(name=ar.read_fstring())

        lod_size = ar.read_int()
        ar = ar.chunk(lod_size)

        while not ar.eof():
            header_name = ar.read_fstring()
            array_size = ar.read_int()
            byte_size = ar.read_int()

            pos = ar.data.tell()

            if header_name == "VERTICES":
                flattened = ar.read_float_vector(array_size * 3)
                data.vertices = (np.array(flattened) * scale).reshape(array_size, 3)
                
                if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms:
                    data.vertices *= (1, -1, 1)
                    
            elif header_name == "INDICES":
                data.indices = np.array(
                    ar.read_int_vector(array_size),
                    dtype=np.int32,
                ).reshape(array_size // 3, 3)
            elif header_name == "NORMALS":
                # W XYZ  # TODO: change to XYZ W 
                flattened = np.array(ar.read_float_vector(array_size * 4))
                data.normals = flattened.reshape(-1, 4)[:, 1:]

                if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms:
                    data.normals *= (1, -1, 1)
                    
            elif header_name == "TANGENTS":
                ar.skip(array_size * 3 * 3)
            elif header_name == "VERTEXCOLORS":
                data.colors = [VertexColor.from_archive(ar) for _ in range(array_size)]
            elif header_name == "TEXCOORDS":
                data.uvs = []
                for _ in range(array_size):
                    count = ar.read_int()
                    uvs = np.array(ar.read_float_vector(count * 2)).reshape(count, 2)

                    if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms:
                        uvs *= (1, -1)
                        uvs += (0, 1)
                
                    data.uvs.append(uvs)
            elif header_name == "MATERIALS":
                data.materials = ar.read_array(
                    array_size,
                    lambda ar: Material.from_archive(ar),
                )
            elif header_name == "WEIGHTS":
                data.weights = ar.read_array(
                    array_size,
                    lambda ar: Weight.from_archive(ar),
                )
            elif header_name == "MORPHTARGETS":
                data.morphs = ar.read_array(
                    array_size,
                    lambda ar: MorphTarget.from_archive(ar, scale),
                )
            else:
                Log.warn(f"Unknown Mesh Data: {header_name}")
                ar.skip(byte_size)

            ar.data.seek(pos + byte_size, 0)
        return data


@dataclass(slots=True)
class UEModelSkeleton:
    metadata: UEModelSkeletonMetadata = None
    bones: list[Bone] = field(default_factory=list)
    sockets: list[Socket] = field(default_factory=list)
    virtual_bones: list[VirtualBone] = field(default_factory=list)

    @classmethod
    def from_archive(cls, ar: FArchiveReader, scale: float = 1.0) -> UEModelSkeleton:
        data = cls()
        
        while not ar.eof():
            section_name = ar.read_fstring()
            array_size = ar.read_int()
            byte_size = ar.read_int()

            pos = ar.data.tell()
            match section_name:
                case "METADATA":
                    data.metadata = UEModelSkeletonMetadata.from_archive(ar)
                case "BONES":
                    data.bones = ar.read_array(
                        array_size,
                        lambda ar: Bone.from_archive(ar, scale),
                    )
                case "SOCKETS":
                    data.sockets = ar.read_array(
                        array_size,
                        lambda ar: Socket.from_archive(ar, scale),
                    )
                case "VIRTUALBONES":
                    data.virtual_bones = ar.read_array(
                        array_size,
                        lambda ar: VirtualBone.from_archive(ar),
                    )
                case _:
                    Log.warn(f"Unknown Skeleton Data: {section_name}")
                    ar.skip(byte_size)
            ar.data.seek(pos + byte_size, 0)

        return data


@dataclass(slots=True)
class UEModelSkeletonMetadata:
    skeleton_path: str
    
    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> UEModelSkeletonMetadata:
        return cls(skeleton_path=ar.read_fstring())

@dataclass(slots=True)
class ConvexCollision:
    name: str
    vertices: npt.NDArray[np.floating[Any]]
    indices: npt.NDArray[np.int32]

    @classmethod
    def from_archive(cls, ar: FArchiveReader, scale: float) -> ConvexCollision:
        name = ar.read_fstring()

        vertices_count = ar.read_int()
        vertices_flattened = ar.read_float_vector(vertices_count * 3)
        vertices = (np.array(vertices_flattened) * scale).reshape(
            vertices_count,
            3,
        )

        if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms:
            vertices *= (1, -1, 1)

        indices_count = ar.read_int()
        indices = np.array(
            ar.read_int_vector(indices_count),
            dtype=np.int32,
        ).reshape(indices_count // 3, 3)

        return cls(name=name, vertices=vertices, indices=indices)


@dataclass(slots=True)
class VertexColor:
    name: str
    data: npt.NDArray[np.float32]

    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> VertexColor:
        name = ar.read_fstring()
        count = ar.read_int()
        data = (np.array(ar.read_byte_vector(count * 4)).reshape(count, 4) / 255).astype(np.float32)

        return cls(name, data)


@dataclass(slots=True)
class Material:
    material_name: str
    material_path: str
    first_index: int
    num_faces: int

    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> Material:
        return cls(
            material_name=ar.read_fstring(),
            material_path=ar.read_fstring() if ar.file_version >= EUEFormatVersion.SerializeMaterialPath else "",
            first_index=ar.read_int(),
            num_faces=ar.read_int(),
        )


@dataclass(slots=True)
class Bone:
    name: str
    parent_index: int
    position: list[float]
    rotation: tuple[float, float, float, float]

    @classmethod
    def from_archive(cls, ar: FArchiveReader, scale: float) -> Bone:
        return cls(
            name=ar.read_fstring(),
            parent_index=ar.read_int(),
            position=ar.read_float_vector(3) 
                     * scale
                     * ((1, -1, 1) if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms else (1, 1, 1)),
            rotation=ar.read_float_vector(4)
                     * ((1, -1, 1, -1) if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms else (1, 1, 1, 1)),
        )


@dataclass(slots=True)
class Weight:
    bone_index: int
    vertex_index: int
    weight: float

    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> Weight:
        return cls(
            bone_index=ar.read_short(),
            vertex_index=ar.read_int(),
            weight=ar.read_float(),
        )


@dataclass(slots=True)
class MorphTarget:
    name: str
    deltas: list[MorphTargetData]

    @classmethod
    def from_archive(cls, ar: FArchiveReader, scale: float) -> MorphTarget:
        return cls(
            name=ar.read_fstring(),
            deltas=ar.read_bulk_array(
                lambda ar: MorphTargetData.from_archive(ar, scale),
            ),
        )


@dataclass(slots=True)
class MorphTargetData:
    position: list[float]
    normals: tuple[float, float, float]
    vertex_index: int

    @classmethod
    def from_archive(cls, ar: FArchiveReader, scale: float) -> MorphTargetData:
        return cls(
            position=ar.read_float_vector(3)
                      * scale
                      * ((1, -1, 1) if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms else (1, 1, 1)),
            normals=ar.read_float_vector(3)
                    * ((1, -1, 1) if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms else (1, 1, 1)),
            vertex_index=ar.read_int(),
        )


@dataclass(slots=True)
class Socket:
    name: str
    parent_name: str
    position: list[float]
    rotation: tuple[float, float, float, float]
    scale: tuple[float, float, float]

    @classmethod
    def from_archive(cls, ar: FArchiveReader, scale: float) -> Socket:
        return cls(
            name=ar.read_fstring(),
            parent_name=ar.read_fstring(),
            position=ar.read_float_vector(3)
                     * scale
                     * ((1, -1, 1) if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms else (1, 1, 1)),
            rotation=ar.read_float_vector(4)
                     * ((1, -1, 1, -1) if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms else (1, 1, 1, 1)),
            scale=ar.read_float_vector(3)
                  * scale
                  * ((1, -1, 1) if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms else (1, 1, 1)),
        )


@dataclass(slots=True)
class VirtualBone:
    source_name: str
    target_name: str
    virtual_name: str

    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> VirtualBone:
        return cls(
            source_name=ar.read_fstring(),
            target_name=ar.read_fstring(),
            virtual_name=ar.read_fstring(),
        )


@dataclass(slots=True)
class UEAnim:
    metadata: UEAnimMetadata = None
    tracks: list[Track] = field(default_factory=list)
    curves: list[Curve] = field(default_factory=list)

    @classmethod
    def from_archive(cls, ar: FArchiveReader, scale: float) -> UEAnim:
        data = cls()
        
        if ar.file_version < EUEFormatVersion.SerializeAssetMetadata:
            data.metadata = UEAnimMetadata(num_frames=ar.read_int(), frames_per_second=ar.read_float())

        while not ar.eof():
            section_name = ar.read_fstring()
            array_size = ar.read_int()
            byte_size = ar.read_int()

            match section_name:
                case "METADATA":
                    data.metadata = UEAnimMetadata.from_archive(ar)
                case "TRACKS":
                    data.tracks = ar.read_array(
                        array_size,
                        lambda ar: Track.from_archive(ar, scale),
                    )
                case "CURVES":
                    data.curves = ar.read_array(
                        array_size,
                        lambda ar: Curve.from_archive(ar),
                    )
                case _:
                    ar.skip(byte_size)

        return data

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
class UEAnimMetadata:
    num_frames: int
    frames_per_second: float
    
    ref_pose_path: str = ""
    additive_anim_type: EAdditiveAnimationType = EAdditiveAnimationType.AAT_None
    ref_pose_type: EAdditiveBasePoseType = EAdditiveBasePoseType.ABPT_None
    ref_frame_index: int = 0
    
    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> UEAnimMetadata:
        return cls(
            num_frames=ar.read_int(),
            frames_per_second=ar.read_float(),
            ref_pose_path=ar.read_fstring(),
            additive_anim_type=EAdditiveAnimationType(int.from_bytes(ar.read_byte())),
            ref_pose_type=EAdditiveBasePoseType(int.from_bytes(ar.read_byte())),
            ref_frame_index=ar.read_int()
        )
    
@dataclass(slots=True)
class Curve:
    name: str
    keys: list[FloatKey]

    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> Curve:
        return cls(
            name=ar.read_fstring(),
            keys=ar.read_bulk_array(lambda ar: FloatKey.from_archive(ar)),
        )


@dataclass(slots=True)
class Track:
    name: str
    position_keys: list[VectorKey]
    rotation_keys: list[QuatKey]
    scale_keys: list[VectorKey]

    @classmethod
    def from_archive(cls, ar: FArchiveReader, scale: float) -> Track:
        return cls(
            name=ar.read_fstring(),
            position_keys=ar.read_bulk_array(
                lambda ar: VectorKey.from_archive(ar, scale),
            ),
            rotation_keys=ar.read_bulk_array(lambda ar: QuatKey.from_archive(ar)),
            scale_keys=ar.read_bulk_array(lambda ar: VectorKey.from_archive(ar, allows_mirror=False)),
        )


@dataclass(slots=True)
class AnimKey:
    frame: int

    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> AnimKey:
        return cls(frame=ar.read_int())


@dataclass(slots=True)
class VectorKey(AnimKey):
    value: list[float]

    @classmethod
    def from_archive(cls, ar: FArchiveReader, multiplier: float = 1.0, allows_mirror: bool = True) -> VectorKey:
        return cls(
            frame=ar.read_int(),
            value=ar.read_float_vector(3) 
                  * multiplier
                  * ((1, -1, 1) if allows_mirror and ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms else (1, 1, 1)),
        )

    def get_vector(self) -> Vector:
        return Vector(self.value)


@dataclass(slots=True)
class QuatKey(AnimKey):
    value: tuple[float, float, float, float]

    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> QuatKey:
        return cls(
            frame=ar.read_int(),
            value=ar.read_float_vector(4)
                  * ((1, -1, 1, -1) if ar.file_version >= EUEFormatVersion.PreserveOriginalTransforms else (1, 1, 1, 1)),
        )

    def get_quat(self) -> Quaternion:
        return Quaternion((self.value[3], self.value[0], self.value[1], self.value[2]))


@dataclass(slots=True)
class FloatKey(AnimKey):
    value: float

    @classmethod
    def from_archive(cls, ar: FArchiveReader) -> FloatKey:
        return cls(
            frame=ar.read_int(),
            value=ar.read_float(),
        )

"""
class UEModelPhysics:
    bodies = []

    def __init__(self, ar: FArchiveReader, scale):

        while not ar.eof():
            header_name = ar.read_fstring()
            array_size = ar.read_int()
            byte_size = ar.read_int()

            pos = ar.data.tell()
            if header_name == "BODIES":
                self.bodies = ar.read_array(array_size, lambda ar: BodySetup(ar, scale))
            else:
                Log.warn(f"Unknown Skeleton Data: {header_name}")
                ar.skip(byte_size)
            ar.data.seek(pos + byte_size, 0)

class EPhysicsType(IntEnum):
    PhysType_Default = 0
    PhysType_Kinematic = 1
    PhysType_Simulated = 2


class BodySetup:
    bone_name = ""
    physics_type = EPhysicsType.PhysType_Default

    sphere_elems = []
    box_elems = []
    capsule_elems = []
    tapered_capsule_elems = []
    convex_elems = []

    def __init__(self, ar: FArchiveReader, scale):
        self.bone_name = ar.read_fstring()
        self.physics_type = EPhysicsType(int.from_bytes(ar.read_byte(), byteorder="big"))

        self.sphere_elems = ar.read_bulk_array(lambda ar: SphereCollision(ar, scale))
        self.box_elems = ar.read_bulk_array(lambda ar: BoxCollision(ar, scale))
        self.capsule_elems = ar.read_bulk_array(lambda ar: CapsuleCollision(ar, scale))
        self.tapered_capsule_elems = ar.read_bulk_array(lambda ar: TaperedCapsuleCollision(ar, scale))
        self.convex_elems = ar.read_bulk_array(lambda ar: ConvexCollision(ar, scale))

class SphereCollision:
    name = ""
    center = []
    radius = 0

    def __init__(self, ar: FArchiveReader, scale):
        self.name = ar.read_fstring()
        self.center = [pos * scale for pos in ar.read_float_vector(3)]
        self.radius = ar.read_float()

class BoxCollision:
    name = ""
    center = []
    rotation = []
    x = 0
    y = 0
    z = 0

    def __init__(self, ar: FArchiveReader, scale):
        self.name = ar.read_fstring()
        self.center = [pos * scale for pos in ar.read_float_vector(3)]
        self.rotation = ar.read_float_vector(3)
        self.x = ar.read_float()
        self.y = ar.read_float()
        self.z = ar.read_float()

class CapsuleCollision:
    name = ""
    center = []
    rotation = []
    radius = 0
    length = 0

    def __init__(self, ar: FArchiveReader, scale):
        self.name = ar.read_fstring()
        self.center = [pos * scale for pos in ar.read_float_vector(3)]
        self.rotation = ar.read_float_vector(3)
        self.radius = ar.read_float()
        self.length = ar.read_float()

class TaperedCapsuleCollision:
    name = ""
    center = []
    rotation = []
    radius0 = 0
    radius1 = 0
    length = 0

    def __init__(self, ar: FArchiveReader, scale):
        self.name = ar.read_fstring()
        self.center = [pos * scale for pos in ar.read_float_vector(3)]
        self.rotation = ar.read_float_vector(3)
        self.radius0 = ar.read_float()
        self.radius1 = ar.read_float()
        self.length = ar.read_float()
"""
