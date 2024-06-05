import bpy
from math import radians
from mathutils import Matrix, Vector, Euler, Quaternion


def first(target, expr, default=None):
    if not target:
        return None
    filtered = filter(expr, target)

    return next(filtered, default)


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


def add_unique(target, item):
    if item in target:
        return

    target.append(item)

def add_range(target, items):
    for item in items:
        target.add(items)
        
