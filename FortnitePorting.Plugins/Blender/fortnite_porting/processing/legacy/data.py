import bpy

from ...utils import ensure_blend_data_for_file


def ensure_legacy_blend_data():
    ensure_blend_data_for_file("legacy/fortnite_porting_data.blend")

    for legacy_name, modern_name in (
        ("FPv3 Vertex Crunch", "FPv4 Vertex Crunch"),
        ("FPv3 Full Vertex Crunch", "FPv4 Full Vertex Crunch"),
    ):
        if legacy_group := bpy.data.node_groups.get(legacy_name):
            legacy_group.name = modern_name
