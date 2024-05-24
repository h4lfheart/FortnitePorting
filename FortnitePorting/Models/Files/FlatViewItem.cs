using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.IO.Objects;

namespace FortnitePorting.Models.Files;

public partial class FlatViewItem : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _path;

    public FlatViewItem(int id, string path)
    {
        Id = id;
        Path = path;
    }

    public async Task CopyPath()
    {
        await Clipboard.SetTextAsync(Path);
    }
}