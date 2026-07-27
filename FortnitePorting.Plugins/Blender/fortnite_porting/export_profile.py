from dataclasses import dataclass
from functools import cache


@dataclass(frozen=True)
class BlenderExportProfile:
    name: str
    minimum_version: tuple[int, int, int]
    uses_legacy_action_curves: bool
    uses_scene_sequence_editor: bool
    uses_legacy_sequence_api: bool
    uses_id_property_geo_nodes_inputs: bool
    uses_legacy_materials: bool


BLENDER_42_PROFILE = BlenderExportProfile(
    name="Blender 4.2-4.5",
    minimum_version=(4, 2, 0),
    uses_legacy_action_curves=True,
    uses_scene_sequence_editor=True,
    uses_legacy_sequence_api=True,
    uses_id_property_geo_nodes_inputs=True,
    uses_legacy_materials=True,
)

BLENDER_50_PROFILE = BlenderExportProfile(
    name="Blender 5.0-5.1",
    minimum_version=(5, 0, 0),
    uses_legacy_action_curves=False,
    uses_scene_sequence_editor=False,
    uses_legacy_sequence_api=False,
    uses_id_property_geo_nodes_inputs=True,
    uses_legacy_materials=False,
)

BLENDER_52_PROFILE = BlenderExportProfile(
    name="Blender 5.2+",
    minimum_version=(5, 2, 0),
    uses_legacy_action_curves=False,
    uses_scene_sequence_editor=False,
    uses_legacy_sequence_api=False,
    uses_id_property_geo_nodes_inputs=False,
    uses_legacy_materials=False,
)


@cache
def resolve_export_profile(version: tuple[int, ...]) -> BlenderExportProfile:
    """Return the API/export profile for the Blender instance receiving an export."""
    normalized_version = tuple(version[:3]) + (0,) * max(0, 3 - len(version))

    if normalized_version >= BLENDER_52_PROFILE.minimum_version:
        return BLENDER_52_PROFILE
    if normalized_version >= BLENDER_50_PROFILE.minimum_version:
        return BLENDER_50_PROFILE
    return BLENDER_42_PROFILE
