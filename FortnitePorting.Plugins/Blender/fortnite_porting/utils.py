import bpy
import json
import os
import sys
from math import radians
from mathutils import Matrix, Vector, Euler, Quaternion

from .logger import Log

blend_files = ["fortnite_porting_data.blend", "fortnite_porting_materials.blend"]

def _read_meta_version() -> tuple:
    meta_path = os.path.join(os.path.dirname(__file__), "fortnite_porting_meta.json")
    if not os.path.exists(meta_path):
        return tuple(sys.modules["fortnite_porting"].bl_info["version"])
    try:
        with open(meta_path, "r") as f:
            meta = json.load(f)
        version_str = meta.get("Version", "").lstrip("v")
        parts = version_str.split("-")[0].split(".")
        return tuple(int(p) for p in parts if p.isdigit())
    except Exception:
        return tuple(sys.modules["fortnite_porting"].bl_info["version"])

loaded_versions: dict[str, tuple] = {}

def ensure_blend_data_for_file(file_name):
    current = addon_version()
    if loaded_versions.get(file_name) == current:
        return

    addon_dir = os.path.dirname(os.path.splitext(__file__)[0])
    blend_node_group_names = []

    with bpy.data.libraries.load(os.path.join(addon_dir, "data", file_name)) as (data_from, data_to):
        blend_node_group_names = list(data_from.node_groups)
        for node_group in sorted(blend_node_group_names, key=lambda x: (x.startswith('.'), x)):
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

    for name in blend_node_group_names:
        group = bpy.data.node_groups.get(name)
        if group is not None and not group.get("addon_version"):
            group["addon_version"] = version_string()

    loaded_versions[file_name] = current


# TODO: Make dynamic from mappings_registry.blend_files list?
def ensure_blend_data():
    for file_name in blend_files:
        ensure_blend_data_for_file(file_name)

def is_node_group_outdated(node_group):
    version_property = node_group.get("addon_version")
    if version_property is None:
        return False
    version_tuple = tuple(int(x) for x in version_property.split("."))
    return version_tuple < addon_version()

def is_current_version_group(node_group):
    if node_group.get("addon_version") is None:
        node_group["addon_version"] = version_string()
        return True

    if not is_node_group_outdated(node_group):
        return True

    old_version = node_group.get("addon_version")
    original_name = node_group.name
    new_name = f"{original_name} v{old_version}"

    if getattr(node_group, "is_embedded_data", False):
        Log.warn(f"Cannot rename embedded outdated node group '{original_name}'")
        return False

    if node_group.library:
        try:
            node_group.make_local()
        except Exception as ex:
            Log.warn(f"Could not localize linked node group '{original_name}': {ex}")
            return False

    try:
        Log.info(f"Renaming outdated node group '{original_name}' to '{new_name}'")
        node_group.name = new_name
    except Exception as ex:
        Log.warn(f"Could not rename outdated node group '{original_name}': {ex}")
        return False

    return False

def addon_version():
    return _read_meta_version()

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

