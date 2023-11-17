using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Tools;

namespace FortnitePorting.ViewModels;

public partial class HeightmapViewModel : ObservableObject
{
    [ObservableProperty] private string mapPath = "Rufus/Game/Athena/Maps/Athena_Terrain";
    [ObservableProperty] private bool exportHeightmap = true;
    [ObservableProperty] private bool exportNormalmap = true;
    [ObservableProperty] private bool exportWeightmap = true;
    [ObservableProperty] private BitmapSource imageSource;

    [RelayCommand]
    public async Task Export()
    {
        await Task.Run(HeightmapExporter.Export);
    }
}