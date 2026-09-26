using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Nodes;

public partial class NodeConnection(NodeSocket from, NodeSocket to) : ObservableObject
{
    [ObservableProperty] private NodeSocket _from = from;
    [ObservableProperty] private NodeSocket _to = to;

    public override string ToString()
    {
        return $"{From.Name} ({From.Parent.ExpressionName}) -> {To.Name} ({To.Parent.ExpressionName})";
    }
}