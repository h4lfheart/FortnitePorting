from ...utils import *


def get_param(source, name):
    found = first(source, lambda param: param.get("Name").casefold() == name.casefold())
    if found is None:
        return None
    return found.get("Value") or found.get("Texture")


def get_vector_param(source, name):
    found = first(source, lambda param: param.get("Name").casefold() == name.casefold())
    if found is None:
        return None
    found_value = found.get("Value")
    return Vector((found_value.get('R'), found_value.get('G'), found_value.get('B')))

def get_param_multiple(source, names):
    found = first(source, lambda param: param.get("Name").casefold() in [name.casefold() for name in names])
    if found is None:
        return None
    return found.get("Value") or found.get("Texture")


def get_param_info(source, name):
    found = first(source, lambda param: param.get("Name").casefold() == name.casefold())
    if found is None:
        return None
    return found

def get_params(source, names):
    return [info.get("Value") for info in where(source, lambda param: param.get("Name").casefold() in [name.casefold() for name in names])]


def get_socket_pos(node, index):
    start_y = -100
    offset_y = -25
    return node.location.x, node.location.y + start_y + offset_y * index

def replace_or_add_parameter(items, replace_item):
    if replace_item is None:
        return

    name = replace_item.get("Name")

    for index, item in enumerate(items):
        if item is None:
            continue
        if item.get("Name") == name:
            items[index] = replace_item
            return

    items.append(replace_item)
    
def replace_or_add_parameter_from_texture(items, name, tex):
      replace_or_add_parameter(items, {
          "Name": name,
          "Texture": tex
      })
        
def get_node(shader_node, name):
    input = shader_node.inputs.get(name)
    if input is None:
        return None
        
    links = input.links
    if links is None or len(links) == 0:
        return None

    return links[0].from_node

# Surround texture in closure and connect to socket
def setup_closure(texture_node, socket_x, socket_y, slot_name, target_node, nodes, links):
    closure_input = nodes.new("NodeClosureInput")
    closure_input.width = 100
    closure_input.hide = True

    closure_output = nodes.new("NodeClosureOutput")
    closure_output.width = 100
    closure_output.hide = True
    closure_output.input_items.new('VECTOR', "Vector")
    closure_output.output_items.new('RGBA', "Color")
    closure_output.output_items.new('FLOAT', "Alpha")

    closure_input.pair_with_output(closure_output)
    closure_input.outputs[1].hide = True
    closure_output.inputs[2].hide = True

    closure_input.location = socket_x - 510, socket_y
    texture_node.location = socket_x - 405, socket_y
    closure_output.location = socket_x - 150, socket_y

    links.new(closure_input.outputs[0], texture_node.inputs[0])
    links.new(texture_node.outputs[0], closure_output.inputs[0])
    links.new(texture_node.outputs[1], closure_output.inputs[1])

    links.new(closure_output.outputs[0], target_node.inputs[slot_name])