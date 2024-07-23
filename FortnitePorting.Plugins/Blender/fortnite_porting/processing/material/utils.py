from ...utils import *


def get_param(source, name):
    found = first(source, lambda param: param.get("Name").casefold() == name.casefold())
    if found is None:
        return None
    return found.get("Value")


def get_param_multiple(source, names):
    found = first(source, lambda param: param.get("Name") in names)
    if found is None:
        return None
    return found.get("Value")


def get_param_info(source, name):
    found = first(source, lambda param: param.get("Name") == name)
    if found is None:
        return None
    return found


def get_socket_pos(node, index):
    start_y = -80
    offset_y = -22
    return node.location.x, node.location.y + start_y + offset_y * index

def replace_or_add_parameter(list, replace_item):
    if replace_item is None:
        return
    for index, item in enumerate(list):
        if item is None:
            continue

        if item.get("Name") == replace_item.get("Name"):
            list[index] = replace_item

    if not any(list, lambda x: x.get("Name") == replace_item.get("Name")):
        list.append(replace_item)
