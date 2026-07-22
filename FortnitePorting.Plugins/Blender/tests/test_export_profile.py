import importlib.util
from pathlib import Path
import unittest


PROFILE_PATH = Path(__file__).parents[1] / "fortnite_porting" / "export_profile.py"
SPEC = importlib.util.spec_from_file_location("export_profile", PROFILE_PATH)
export_profile = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(export_profile)


class ExportProfileTests(unittest.TestCase):
    def test_blender_45_uses_legacy_apis(self):
        profile = export_profile.resolve_export_profile((4, 5, 8))

        self.assertEqual(profile.name, "Blender 4.5")
        self.assertTrue(profile.uses_legacy_action_curves)
        self.assertTrue(profile.uses_scene_sequence_editor)

    def test_blender_50_uses_slotted_actions(self):
        profile = export_profile.resolve_export_profile((5, 0, 1))

        self.assertEqual(profile.name, "Blender 5.0-5.1")
        self.assertFalse(profile.uses_legacy_action_curves)
        self.assertTrue(profile.uses_id_property_geo_nodes_inputs)

    def test_blender_52_uses_new_geometry_nodes_inputs(self):
        profile = export_profile.resolve_export_profile((5, 2, 0))

        self.assertEqual(profile.name, "Blender 5.2+")
        self.assertFalse(profile.uses_id_property_geo_nodes_inputs)


if __name__ == "__main__":
    unittest.main()
