using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Tools;

namespace FortnitePorting.ViewModels;

public partial class HeightmapViewModel : ObservableObject
{
    [RelayCommand]
    public async Task Export()
    {
        await Task.Run(HeightmapExporter.Export);
    } 
}