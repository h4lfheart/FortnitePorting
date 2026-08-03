from ..context.material_context import create_texture_node, create_scalar_node, create_color_node

UNSUPPORTED_COLOR = (1.0, 0.0, 1.0, 1.0)


class BuiltNode:
    """Wraps whatever bpy node(s) a single UE expression became, exposing its pins
    in the same terms the exported graph uses: outputs keyed by UE output index,
    inputs keyed by UE input pin name. `extra_links` are internal links between the
    node(s) this builder created (e.g. Clamp's Max feeding into Min) that graph_builder
    should wire up alongside the graph's own connections."""

    def __init__(self, node, outputs=None, inputs=None, extra_links=None):
        self.node = node
        self.outputs = outputs if outputs is not None else ({0: node.outputs[0]} if node.outputs else {})
        self.inputs = inputs if inputs is not None else {}
        self.extra_links = extra_links or []


def _color_outputs(nodes, node, include_rgba=False):
    # Mirrors ExportContext.MaterialGraph.cs's AddColorOutputs ordering: RGB, R, G, B, A, (RGBA).
    # Blender's RGB/TexImage nodes only expose a combined "Color" (+ "Alpha" for TexImage) socket,
    # not separate R/G/B ones, so synthesize those via a SeparateColor node fed from Color.
    color_socket = node.outputs["Color"]

    separate = nodes.new(type="ShaderNodeSeparateColor")
    separate.mode = "RGB"
    separate.hide = True

    outputs = {0: color_socket, 1: separate.outputs["Red"], 2: separate.outputs["Green"], 3: separate.outputs["Blue"]}
    extra_links = [(color_socket, separate.inputs["Color"])]

    if "Alpha" in node.outputs:
        outputs[4] = node.outputs["Alpha"]
        if include_rgba:
            outputs[5] = color_socket

    return outputs, extra_links


def build_texture_sample(ctx, nodes, node_data):
    properties = node_data.get("Properties", {})
    texture = properties.get("Texture")
    label = properties.get("ParameterName") or node_data.get("Id")

    if texture is None:
        return build_unsupported(ctx, nodes, node_data)

    image = ctx.import_image(texture.get("Path"))
    node = create_texture_node(nodes, label, image, texture.get("sRGB"))
    outputs, extra_links = _color_outputs(nodes, node, include_rgba=True)

    return BuiltNode(node, outputs=outputs, inputs={"Coordinates": node.inputs[0]}, extra_links=extra_links)


def build_texture_coordinate(ctx, nodes, node_data):
    properties = node_data.get("Properties", {})
    uv_node = nodes.new(type="ShaderNodeUVMap")

    u_tiling = properties.get("UTiling", 1.0)
    v_tiling = properties.get("VTiling", 1.0)
    if u_tiling == 1.0 and v_tiling == 1.0:
        return BuiltNode(uv_node)

    mapping = nodes.new(type="ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (u_tiling, v_tiling, 1.0)
    return BuiltNode(mapping, outputs={0: mapping.outputs[0]}, extra_links=[(uv_node.outputs[0], mapping.inputs["Vector"])])


def build_constant(ctx, nodes, node_data):
    value = node_data.get("Properties", {}).get("R", 0.0)
    node = create_scalar_node(nodes, node_data.get("Id"), value)
    return BuiltNode(node)


def build_constant2vector(ctx, nodes, node_data):
    properties = node_data.get("Properties", {})
    value = {"R": properties.get("R", 0.0), "G": properties.get("G", 0.0), "B": 0.0, "A": 1.0}
    node = create_color_node(nodes, node_data.get("Id"), value)
    outputs, extra_links = _color_outputs(nodes, node)
    return BuiltNode(node, outputs=outputs, extra_links=extra_links)


def build_color_constant(ctx, nodes, node_data):
    properties = node_data.get("Properties", {})
    label = properties.get("ParameterName") or node_data.get("Id")
    value = {
        "R": properties.get("R", 0.0),
        "G": properties.get("G", 0.0),
        "B": properties.get("B", 0.0),
        "A": properties.get("A", 1.0),
    }
    node = create_color_node(nodes, label, value)
    outputs, extra_links = _color_outputs(nodes, node)
    return BuiltNode(node, outputs=outputs, extra_links=extra_links)


def build_scalar_parameter(ctx, nodes, node_data):
    properties = node_data.get("Properties", {})
    label = properties.get("ParameterName") or node_data.get("Id")
    node = create_scalar_node(nodes, label, properties.get("DefaultValue", 0.0))
    return BuiltNode(node)


def build_static_bool_parameter(ctx, nodes, node_data):
    properties = node_data.get("Properties", {})
    label = properties.get("ParameterName") or node_data.get("Id")
    node = create_scalar_node(nodes, label, 1.0 if properties.get("DefaultValue") else 0.0)
    return BuiltNode(node)


def build_vertex_color(ctx, nodes, node_data):
    node = nodes.new(type="ShaderNodeVertexColor")
    outputs, extra_links = _color_outputs(nodes, node)
    return BuiltNode(node, outputs=outputs, extra_links=extra_links)


def build_component_mask(ctx, nodes, node_data):
    properties = node_data.get("Properties", {})
    separate = nodes.new(type="ShaderNodeSeparateColor")
    separate.mode = "RGB"

    selected = [channel for channel in ("R", "G", "B") if properties.get(channel)]
    socket_name = {"R": "Red", "G": "Green", "B": "Blue"}

    if len(selected) <= 1:
        channel = selected[0] if selected else "R"
        return BuiltNode(separate, outputs={0: separate.outputs[socket_name[channel]]}, inputs={"": separate.inputs["Color"]})

    combine = nodes.new(type="ShaderNodeCombineColor")
    combine.mode = "RGB"
    extra_links = [(separate.outputs[socket_name[channel]], combine.inputs[socket_name[channel]]) for channel in selected]
    return BuiltNode(combine, outputs={0: combine.outputs["Color"]}, inputs={"": separate.inputs["Color"]}, extra_links=extra_links)


def build_math(operation, input_names=("A", "B")):
    def _build(ctx, nodes, node_data):
        node = nodes.new(type="ShaderNodeMath")
        node.operation = operation
        inputs = {name: node.inputs[index] for index, name in enumerate(input_names) if index < len(node.inputs)}
        return BuiltNode(node, inputs=inputs)

    return _build


def build_one_minus(ctx, nodes, node_data):
    node = nodes.new(type="ShaderNodeMath")
    node.operation = "SUBTRACT"
    node.inputs[0].default_value = 1.0
    return BuiltNode(node, inputs={"": node.inputs[1]})


def build_power(ctx, nodes, node_data):
    node = nodes.new(type="ShaderNodeMath")
    node.operation = "POWER"
    node.inputs[1].default_value = node_data.get("Properties", {}).get("ConstExponent", 2.0)
    return BuiltNode(node, inputs={"Base": node.inputs[0], "Exponent": node.inputs[1]})


def build_clamp(ctx, nodes, node_data):
    max_node = nodes.new(type="ShaderNodeMath")
    max_node.operation = "MAXIMUM"
    min_node = nodes.new(type="ShaderNodeMath")
    min_node.operation = "MINIMUM"
    min_node.location = max_node.location.x + 200, max_node.location.y

    inputs = {"": max_node.inputs[0], "Min": max_node.inputs[1], "Max": min_node.inputs[1]}
    return BuiltNode(min_node, inputs=inputs, extra_links=[(max_node.outputs[0], min_node.inputs[0])])


def build_normalize(ctx, nodes, node_data):
    node = nodes.new(type="ShaderNodeVectorMath")
    node.operation = "NORMALIZE"
    return BuiltNode(node, inputs={"VectorInput": node.inputs[0]})


def build_lerp(ctx, nodes, node_data):
    node = nodes.new(type="ShaderNodeMix")
    node.data_type = "RGBA"
    return BuiltNode(node, outputs={0: node.outputs["Result"]}, inputs={"A": node.inputs["A"], "B": node.inputs["B"], "Alpha": node.inputs["Factor"]})


def build_dot_product(ctx, nodes, node_data):
    node = nodes.new(type="ShaderNodeVectorMath")
    node.operation = "DOT_PRODUCT"
    return BuiltNode(node, outputs={0: node.outputs["Value"]}, inputs={"A": node.inputs[0], "B": node.inputs[1]})


def build_append_vector(ctx, nodes, node_data):
    node = nodes.new(type="ShaderNodeCombineXYZ")
    return BuiltNode(node, inputs={"A": node.inputs["X"], "B": node.inputs["Y"]})


def build_fresnel(ctx, nodes, node_data):
    node = nodes.new(type="ShaderNodeFresnel")
    return BuiltNode(node, inputs={"Normal": node.inputs["Normal"]})


def build_panner(ctx, nodes, node_data):
    properties = node_data.get("Properties", {})
    node = nodes.new(type="ShaderNodeMapping")
    node.inputs["Location"].default_value = (properties.get("SpeedX", 0.0), properties.get("SpeedY", 0.0), 0.0)
    return BuiltNode(node, inputs={"Coordinate": node.inputs["Vector"]})


def build_reroute(ctx, nodes, node_data):
    node = nodes.new(type="NodeReroute")
    return BuiltNode(node, inputs={"": node.inputs[0]})


def build_unsupported(ctx, nodes, node_data):
    node = nodes.new(type="ShaderNodeRGB")
    node.outputs[0].default_value = UNSUPPORTED_COLOR
    node.label = f"Unsupported: {node_data.get('Type')}"
    return BuiltNode(node)


UE_NODE_BUILDERS = {
    "TextureSample": build_texture_sample,
    "TextureSampleParameter2D": build_texture_sample,
    "TextureObjectParameter": build_texture_sample,
    "TextureCoordinate": build_texture_coordinate,
    "Constant": build_constant,
    "Constant2Vector": build_constant2vector,
    "Constant3Vector": build_color_constant,
    "Constant4Vector": build_color_constant,
    "VectorParameter": build_color_constant,
    "ScalarParameter": build_scalar_parameter,
    "StaticBoolParameter": build_static_bool_parameter,
    "VertexColor": build_vertex_color,
    "ParticleColor": build_vertex_color,
    "ComponentMask": build_component_mask,
    "Add": build_math("ADD"),
    "Subtract": build_math("SUBTRACT"),
    "Multiply": build_math("MULTIPLY"),
    "Divide": build_math("DIVIDE"),
    "Abs": build_math("ABSOLUTE", input_names=("",)),
    "Min": build_math("MINIMUM"),
    "Max": build_math("MAXIMUM"),
    "OneMinus": build_one_minus,
    "Power": build_power,
    "Clamp": build_clamp,
    "LinearInterpolate": build_lerp,
    "Normalize": build_normalize,
    "DotProduct": build_dot_product,
    "AppendVector": build_append_vector,
    "Fresnel": build_fresnel,
    "Panner": build_panner,
    "Reroute": build_reroute,
}
