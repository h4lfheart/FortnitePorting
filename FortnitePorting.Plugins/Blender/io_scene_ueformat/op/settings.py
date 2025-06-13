from bpy.props import BoolProperty, FloatProperty, IntProperty
from bpy.types import PropertyGroup
from typing import Any


class UFSettings(PropertyGroup):
    scale_factor: FloatProperty(name="Scale", default=0.01, min=0.01)
    
    bone_length: FloatProperty(name="Bone Length", default=4.0, min=0.1)
    reorient_bones: BoolProperty(name="Reorient Bones", default=False)
    target_lod: IntProperty(name="Level of Detail", default=0, min=0)
    import_collision: BoolProperty(name="Import Collision", default=False)
    import_morph_targets: BoolProperty(name="Import Morph Targets", default=True)
    import_sockets: BoolProperty(name="Import Sockets", default=True)
    import_virtual_bones: BoolProperty(name="Import Virtual Bones", default=False)

    rotation_only: BoolProperty(name="Rotation Only", default=False)
    import_curves: BoolProperty(name="Import Curves", default=True)

    def get_props(self) -> dict[str, Any]:
        props = {}

        for key in self.__annotations__.keys():
            props[key] = getattr(self, key)

        return props
