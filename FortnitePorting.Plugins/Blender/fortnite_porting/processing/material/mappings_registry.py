from .enums import ENodeType
from ...utils import *

class MappingCollection:
    node_name = ""
    type = None
    order = 99
    node_spacing = 500
    textures = ()
    scalars = ()
    colors = ()
    vectors = ()
    switches = ()
    component_masks = ()
    build_node = "FPv4 Material Build"
    surface_render_method = None
    show_transparent_back = True


    @classmethod
    def meets_criteria(self, material_data):
        # Placeholder for criteria checking logic
        # TODO: Default = any_slots_match()?
        def matches(name, slots):
            return any(slots, lambda x: x.name.casefold() == name.casefold())

        match_tex = any(material_data.get("Textures"), lambda tex: matches(tex.get("Name"), self.textures))
        match_scal = any(material_data.get("Scalars"), lambda scal: matches(scal.get("Name"), self.scalars))
        match_col = any(material_data.get("Vectors"), lambda vec: matches(vec.get("Name"), self.colors))
        match_vec = any(material_data.get("Vectors"), lambda vec: matches(vec.get("Name"), self.vectors))
        match_switch = any(material_data.get("Switches"), lambda switch: matches(switch.get("Name"), self.switches))
        match_comp = any(material_data.get("ComponentMasks"), lambda comp: matches(comp.get("Name"), self.component_masks))


        return match_tex or match_scal or match_col or match_vec or match_switch or match_comp

    @classmethod
    def meets_criteria_dynamic(self, material_data, index):
        # Placeholder for dynamic criteria checking logic
        return False


class LayerMappingsTemplate():
    node_name=""
    type=ENodeType.NT_Layer

    @classmethod
    def meets_criteria_dynamic(self, material_data, index):
        return False

    LAYER_TEXTURE_TEMPLATES = ()
    LAYER_SCALARS_TEMPLATES = ()
    LAYER_COLORS_TEMPLATES = ()
    LAYER_VECTORS_TEMPLATES = ()
    LAYER_SWITCH_TEMPLATES = ()
    LAYER_COMPONENT_MASKS_TEMPLATES = ()

    @classmethod
    def textures(self, index):
        return create_layer_slots(self.LAYER_TEXTURE_TEMPLATES, index)

    @classmethod
    def scalars(self, index):
        return create_layer_slots(self.LAYER_SWITCH_TEMPLATES, index)

    @classmethod
    def colors(self, index):
        return create_layer_slots(self.LAYER_COLORS_TEMPLATES, index)

    @classmethod
    def vectors(self, index):
        return create_layer_slots(self.LAYER_VECTORS_TEMPLATES, index)

    @classmethod
    def switches(self, index):
        return create_layer_slots(self.LAYER_SWITCH_TEMPLATES, index)

    @classmethod
    def component_masks(self, index):
        return create_layer_slots(self.LAYER_COMPONENT_MASKS_TEMPLATES, index)


class SlotMapping:
    def __init__(self, name, slot=None, alpha_slot=None, switch_slot=None, value_func=None, coords="UV0", default=None, closure=False):
        self.name = name
        self.slot = name if slot is None else slot
        self.alpha_slot = alpha_slot
        self.switch_slot = switch_slot
        self.value_func = value_func
        self.coords = coords
        self.default = default
        self.closure = closure


class DefaultTexture:
    def __init__(self, name, sRGB=True):
        self.name = name
        self.sRGB = sRGB


class MappingRegistry:
    def __init__(self):
        self.mappings = []


    def register(self, mapping):
        self.mappings.append(mapping)
        return mapping


    def get_all_mappings(self):
        return self.mappings


    def get_mappings_for_type(self, node_type):
        mappings = [m for m in self.get_all_mappings() if m.type == node_type]
        return sorted(mappings, key=lambda mapping: mapping.order)


    def find_all_matching_mappings(self, material_data, type):
        matches = []
        all_mappings = self.get_mappings_for_type(type) if type is not None else self.get_all_mappings()
        for mappings in all_mappings:
            if mappings.meets_criteria(material_data):
                matches.append(mappings)
        return sorted(matches, key=lambda mapping: (mapping.type, mapping.order), reverse=True)


# Global registry instance
registry = MappingRegistry()


def get_all_mappings():
    return registry.get_all_mappings()


def get_mappings_for_type(node_type):
    return registry.get_mappings_for_type(node_type)


def find_all_matching_mappings(material_data, type=None):
    return registry.find_all_matching_mappings(material_data, type=type)


# Factory function to create slots from templates
def create_layer_slots(templates, layer_num):
    result = []
    for template in templates:
        def replace_hash(s):
            return s.replace("#", str(layer_num)) if s and "#" in s else s

        result.append(SlotMapping(
            name=replace_hash(template.name),
            slot=replace_hash(template.slot),
            alpha_slot=replace_hash(template.alpha_slot),
            switch_slot=replace_hash(template.switch_slot),
            value_func=template.value_func,
            coords=template.coords,
            default=template.default,
            closure=template.closure
        ))
    return tuple(result)


# Factory function to create dynamic layer mappings from parent class
def create_layer_mappings(parent_class, class_name_prefix, min_layer=2, max_layer=6):
    for layer_index in range(min_layer, max_layer + 1):
        layer_class = type(
            f'{class_name_prefix}Layer{layer_index}Mappings',
            (MappingCollection,),
            {
                'node_name': parent_class.node_name,
                'type': parent_class.type,
                'order': layer_index,
                'textures': parent_class.textures(layer_index),
                'scalars': parent_class.scalars(layer_index),
                'colors': parent_class.colors(layer_index),
                'vectors': parent_class.vectors(layer_index),
                'switches': parent_class.switches(layer_index),
                'component_masks': parent_class.component_masks(layer_index),
                'meets_criteria': classmethod(
                    lambda cls, material_data, num=layer_index:
                        parent_class.meets_criteria_dynamic(material_data, num)
                )
            }
        )
        registry.register(layer_class)