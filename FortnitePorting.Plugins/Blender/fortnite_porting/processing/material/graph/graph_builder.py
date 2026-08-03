from .node_builders import UE_NODE_BUILDERS, build_unsupported

# Maps UE top-level material output property names to Blender Principled BSDF input socket
# names. Blender 4.2+ socket names (addon targets blender_version_min = "4.2.0" per
# blender_manifest.toml, so no legacy Blender socket-name branching needed).
PRINCIPLED_OUTPUT_MAP = {
    "BaseColor": "Base Color",
    "Metallic": "Metallic",
    "Roughness": "Roughness",
    "EmissiveColor": "Emission Color",
    "Opacity": "Alpha",
    "OpacityMask": "Alpha",
    "Specular": "Specular IOR Level",
}

NORMAL_OUTPUT_NAME = "Normal"


def build(ctx, nodes, links, graph_data, output_socket):
    """Builds a Blender node graph equivalent to the exported UE material expression graph
    and wires it into `output_socket` (a Material Output node's Surface input). Nodes not
    reachable from any top-level output are never built. Returns True if anything was wired."""

    if not graph_data or not graph_data.get("Nodes"):
        return False

    node_lookup = {node_data["Id"]: node_data for node_data in graph_data["Nodes"]}
    built_cache = {}
    in_progress = set()

    def get_built(node_id):
        if node_id in built_cache:
            return built_cache[node_id]
        if node_id in in_progress:
            return None  # defensive cycle guard; UE material graphs should be acyclic

        node_data = node_lookup.get(node_id)
        if node_data is None:
            return None

        in_progress.add(node_id)
        builder = UE_NODE_BUILDERS.get(node_data.get("Type"), build_unsupported)
        built = builder(ctx, nodes, node_data)
        built_cache[node_id] = built
        in_progress.discard(node_id)

        for from_socket, to_socket in built.extra_links:
            links.new(from_socket, to_socket)

        for node_input in node_data.get("Inputs", []):
            wire_input(built, node_input)

        return built

    def resolve_source(connection):
        source_id = connection.get("SourceNodeId")
        if source_id is None:
            return None

        source_built = get_built(source_id)
        if source_built is None:
            return None

        index = connection.get("SourceOutputIndex", 0)
        return source_built.outputs.get(index, source_built.outputs.get(0))

    def apply_mask(source_socket, connection):
        if not connection.get("Mask") or getattr(source_socket, "type", None) != "RGBA":
            return source_socket

        selected = [name for name, key in (("Red", "MaskR"), ("Green", "MaskG"), ("Blue", "MaskB")) if connection.get(key)]
        if len(selected) != 1:
            return source_socket

        separate = nodes.new(type="ShaderNodeSeparateColor")
        separate.mode = "RGB"
        separate.hide = True
        links.new(source_socket, separate.inputs["Color"])
        return separate.outputs[selected[0]]

    def wire_input(built, node_input):
        consumer = built.inputs.get(node_input.get("Name", ""))
        if consumer is None:
            return

        source_socket = resolve_source(node_input)
        if source_socket is None:
            default_value = node_input.get("DefaultValue")
            if default_value is not None:
                apply_default_value(consumer, default_value)
            return

        links.new(apply_mask(source_socket, node_input), consumer)

    principled = nodes.new(type="ShaderNodeBsdfPrincipled")
    links.new(principled.outputs["BSDF"], output_socket)

    wired_any = False
    for graph_output in graph_data.get("Outputs", []):
        property_name = graph_output.get("PropertyName")
        is_normal = property_name == NORMAL_OUTPUT_NAME
        socket_name = PRINCIPLED_OUTPUT_MAP.get(property_name)

        if socket_name is None and not is_normal:
            continue  # no Principled-compatible slot this phase (WorldPositionOffset, Refraction, ...)

        source_socket = resolve_source(graph_output)
        if source_socket is None:
            default_value = graph_output.get("DefaultValue")
            if default_value is not None and not is_normal:
                apply_default_value(principled.inputs[socket_name], default_value)
                wired_any = True
            continue

        source_socket = apply_mask(source_socket, graph_output)

        if is_normal:
            normal_map = nodes.new(type="ShaderNodeNormalMap")
            links.new(source_socket, normal_map.inputs["Color"])
            links.new(normal_map.outputs["Normal"], principled.inputs["Normal"])
        else:
            links.new(source_socket, principled.inputs[socket_name])
            if property_name == "EmissiveColor":
                principled.inputs["Emission Strength"].default_value = 1.0

        wired_any = True

    layout_nodes(node_lookup, built_cache, graph_data, principled)

    return wired_any


def apply_default_value(socket, value):
    try:
        if isinstance(value, (list, tuple)):
            if hasattr(socket.default_value, "__len__"):
                target_length = len(socket.default_value)
                if target_length == 4:
                    socket.default_value = (value[0], value[1], value[2], value[3] if len(value) > 3 else 1.0)
                elif target_length == 3:
                    socket.default_value = tuple(value[:3])
        else:
            socket.default_value = value
    except (TypeError, ValueError):
        pass  # socket type didn't accept the literal; leave its own default


def layout_nodes(node_lookup, built_cache, graph_data, principled):
    """Places built nodes in columns by longest distance from the material outputs, so the
    graph reads left-to-right upstream. Not pixel-parity with Unreal's own layout."""
    spacing_x = 260
    spacing_y = 220

    depths = {}
    visiting = set()

    def compute_depth(node_id, depth):
        if node_id not in built_cache or node_id in visiting:
            return
        if depths.get(node_id, -1) >= depth:
            return

        depths[node_id] = depth
        visiting.add(node_id)

        for node_input in node_lookup.get(node_id, {}).get("Inputs", []):
            source_id = node_input.get("SourceNodeId")
            if source_id:
                compute_depth(source_id, depth + 1)

        visiting.discard(node_id)

    for graph_output in graph_data.get("Outputs", []):
        source_id = graph_output.get("SourceNodeId")
        if source_id:
            compute_depth(source_id, 1)

    columns = {}
    for node_id, depth in depths.items():
        columns.setdefault(depth, []).append(node_id)

    principled.location = (0, 0)
    for depth, node_ids in columns.items():
        x = -depth * spacing_x
        for row, node_id in enumerate(node_ids):
            built_cache[node_id].node.location = (x, -row * spacing_y)
