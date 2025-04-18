using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Material;

public partial class MaterialNodeConnection(MaterialNodeSocket from, MaterialNodeSocket to) : ObservableObject
{
    [ObservableProperty] private MaterialNodeSocket _from = from;
    [ObservableProperty] private MaterialNodeSocket _to = to;

    public override string ToString()
    {
        return $"{From.Name} ({From.Parent.ExpressionName}) -> {To.Name} ({to.Parent.ExpressionName})";
    }
}