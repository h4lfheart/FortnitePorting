from collections.abc import Collection
from typing import cast

import bpy
from bpy.types import PoseBone, bpy_prop_collection
from mathutils import Vector, Quaternion
from math import *


def bytes_to_str(in_bytes: bytes) -> str:
    return in_bytes.rstrip(b"\x00").decode()


def make_axis_vector(vec_in: Vector) -> Vector:
    vec_out = Vector()
    x, y, z = vec_in

    if abs(x) > abs(y):
        if abs(x) > abs(z):
            vec_out.x = 1 if x >= 0 else -1
        else:
            vec_out.z = 1 if z >= 0 else -1
    elif abs(y) > abs(z):
        vec_out.y = 1 if y >= 0 else -1
    else:
        vec_out.z = 1 if z >= 0 else -1

    return vec_out


def get_case_insensitive(source: bpy_prop_collection, string: str) -> PoseBone | None:
    string = string.lower()
    source_ = cast(Collection[PoseBone], source)

    for item in source_:
        if item.name.lower() == string:
            return item

    return None


def get_active_armature():
    obj = bpy.context.object

    if obj is None:
        return None

    if obj.type == "ARMATURE":
        return obj
    elif obj.type == "MESH":
        for modifier in obj.modifiers:  # type: ignore
            if modifier.type == "ARMATURE":
                return modifier.object

# pitch, yaw, roll
def make_quat(rot):
    return Quaternion((rot[3], rot[0], rot[1], rot[2]))

def make_vector(vec):
    return Vector((vec[0], vec[1], vec[2]))