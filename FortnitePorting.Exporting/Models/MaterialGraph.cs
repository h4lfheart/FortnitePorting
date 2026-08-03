using System.Collections.Generic;

namespace FortnitePorting.Exporting.Models;

public record MaterialGraph
{
    public List<MaterialGraphNode> Nodes = [];
    public List<MaterialGraphOutput> Outputs = [];
}

public record MaterialGraphNode
{
    public string Id = string.Empty;
    public string Type = string.Empty;
    public Dictionary<string, object?> Properties = [];
    public List<MaterialGraphInput> Inputs = [];
    public List<string> Outputs = [];
}

public record MaterialGraphInput
{
    public string Name = string.Empty;
    public string? SourceNodeId;
    public int SourceOutputIndex;
    public object? DefaultValue;

    // Inline component-mask carried directly on the connection (UE lets any wire swizzle without a dedicated node)
    public int Mask;
    public int MaskR;
    public int MaskG;
    public int MaskB;
    public int MaskA;
}

public record MaterialGraphOutput
{
    public string PropertyName = string.Empty;
    public string? SourceNodeId;
    public int SourceOutputIndex;
    public object? DefaultValue;
}
