import bpy
import os
import sys
from math import radians
from mathutils import Matrix, Vector, Euler, Quaternion

from .logger import Log

blend_files = ["fortnite_porting_data.blend", "fortnite_porting_materials.blend"]

_loaded_versions: dict[str, tuple] = {}

def ensure_blend_data_for_file(file_name):
    current = addon_version()
    if _loaded_versions.get(file_name) == current:
        return

    addon_dir = os.path.dirname(os.path.splitext(__file__)[0])

    with bpy.data.libraries.load(os.path.join(addon_dir, "data", file_name)) as (data_from, data_to):
        for node_group in sorted(data_from.node_groups, key=lambda x: (x.startswith('.'), x)):
            if (group := bpy.data.node_groups.get(node_group)) and is_current_version_group(group):
                continue
            data_to.node_groups.append(node_group)

        for mat in data_from.materials:
            if not mat.startswith(".") and not bpy.data.materials.get(mat):
                data_to.materials.append(mat)

        for image in data_from.images:
            if not bpy.data.images.get(image):
                data_to.images.append(image)

        for obj in data_from.objects:
            if not obj.startswith(".") and not bpy.data.objects.get(obj):
                data_to.objects.append(obj)

        for font in data_from.fonts:
            if not bpy.data.fonts.get(font):
                data_to.fonts.append(font)

    for node_group in data_to.node_groups:
        if not node_group.get("addon_version"):
            node_group["addon_version"] = version_string()

    _loaded_versions[file_name] = current


# TODO: Make dynamic from mappings_registry.blend_files list?
def ensure_blend_data():
    for file_name in blend_files:
        ensure_blend_data_for_file(file_name)

def is_node_group_outdated(node_group):
    version_property = node_group.get("addon_version")
    if version_property is None:
        return True
    version_tuple = tuple(int(x) for x in version_property.split("."))
    return version_tuple < addon_version()

def is_current_version_group(node_group):
    if is_node_group_outdated(node_group):
        old_version = node_group.get("addon_version", "Outdated")
        new_name = f"{node_group.name} v{old_version}"
        Log.info(f"Renaming outdated node group '{node_group.name}' to '{new_name}'")
        node_group.name = new_name
        return False
    return True

def addon_version():
    return tuple(sys.modules["fortnite_porting"].bl_info["version"])

def version_string():
    return '.'.join(str(x) for x in addon_version())

def hash_code(num):
    return hex(abs(num))[2:]


def first(target, expr, default=None):
    if not target:
        return None
    filtered = filter(expr, target)

    return next(filtered, default)

def best(target, expr, goal, default=None):
    if not target:
        return None

    for item in target:
        if expr(item) == goal:
            return item

    for item in target:
        if expr(item) in goal:
            return item

    return default


def where(target, expr):
    if not target:
        return []
    filtered = filter(expr, target)

    return list(filtered)


def any(target, expr):
    if not target:
        return False

    filtered = list(filter(expr, target))
    return len(filtered) > 0

def all(target, expr):
    if not target:
        return False

    for item in target:
        if not expr(item):
            return False
    return True


def add_unique(target, item):
    if item in target:
        return

    target.append(item)


def add_range(target, items):
    for item in items:
        target.add(items)

def get_case_insensitive(source, string):
    for item in source:
        if item.name.casefold() == string.casefold():
            return item
    return None

