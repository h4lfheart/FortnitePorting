from __future__ import annotations

import numpy as np

from ..dto.anim import AnimDto
from ..dto.model import ModelDto
from ..dto.pose import PoseDto
from ..version import EUEFormatVersion

_POS_MIRROR = np.array((1.0, -1.0, 1.0))
_UV_MIRROR = np.array((1.0, -1.0))
_UV_OFFSET = np.array((0.0, 1.0))
_QUAT_MIRROR = np.array((1.0, -1.0, 1.0, -1.0))


def should_mirror(version: EUEFormatVersion) -> bool:
    return version >= EUEFormatVersion.PreserveOriginalTransforms


def _apply_pos(value, scale: float, mirror: bool):
    out = np.asarray(value, dtype=np.float64) * scale
    if mirror:
        out = out * _POS_MIRROR
    return out


def _apply_quat(value, mirror: bool):
    out = np.asarray(value, dtype=np.float64)
    if mirror:
        out = out * _QUAT_MIRROR
    return out


def to_blender_space(dto: ModelDto | AnimDto | PoseDto, version: EUEFormatVersion, scale_factor: float) -> None:
    mirror = should_mirror(version)
    if isinstance(dto, ModelDto):
        _model(dto, scale_factor, mirror)
    elif isinstance(dto, AnimDto):
        _anim(dto, scale_factor, mirror)
    elif isinstance(dto, PoseDto):
        _pose(dto, scale_factor, mirror)


def _model(dto: ModelDto, scale: float, mirror: bool) -> None:
    for lod in dto.lods:
        if lod.vertices.size:
            lod.vertices = _apply_pos(lod.vertices, scale, mirror)
        if lod.normals.size:
            lod.normals = _apply_pos(lod.normals, 1.0, mirror)
        lod.uvs = [(uvs * _UV_MIRROR + _UV_OFFSET) if mirror else uvs for uvs in lod.uvs]
        for morph in lod.morphs:
            for delta in morph.deltas:
                delta.position = _apply_pos(delta.position, scale, mirror)
                delta.normals = _apply_pos(delta.normals, 1.0, mirror)

    if dto.skeleton:
        for bone in dto.skeleton.bones:
            bone.position = _apply_pos(bone.position, scale, mirror)
            bone.rotation = _apply_quat(bone.rotation, mirror)
        for socket in dto.skeleton.sockets:
            socket.position = _apply_pos(socket.position, scale, mirror)
            socket.rotation = _apply_quat(socket.rotation, mirror)
            socket.scale = _apply_pos(socket.scale, scale, mirror)

    for collision in dto.collisions:
        if collision.vertices.size:
            collision.vertices = _apply_pos(collision.vertices, scale, mirror)


def _anim(dto: AnimDto, scale: float, mirror: bool) -> None:
    for track in dto.tracks:
        for key in track.position_keys:
            key.value = _apply_pos(key.value, scale, mirror)
        for key in track.rotation_keys:
            key.value = _apply_quat(key.value, mirror)


def _pose(dto: PoseDto, scale: float, mirror: bool) -> None:
    for pose in dto.poses:
        for key in pose.keys:
            key.position = _apply_pos(key.position, scale, mirror)
            key.rotation = _apply_quat(key.rotation, mirror)
