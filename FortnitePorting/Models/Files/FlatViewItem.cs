using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.IO.Objects;

namespace FortnitePorting.Models.Files;

public partial class FlatViewItem : ObservableObject
{
    [ObservableProperty] private FPackageId _id;
    [ObservableProperty] private string _path;

    public FlatViewItem(FPackageId id, string path)
    {
        Id = id;
        Path = path;
    }

    public async Task CopyPath()
    {
        await Clipboard.SetTextAsync(Path);
    }
}