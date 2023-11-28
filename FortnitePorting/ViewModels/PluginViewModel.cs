using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels;

public partial class PluginViewModel : ViewModelBase
{
    [ObservableProperty] private BlenderPluginViewModel blender = new();

    public override async Task Initialize()
    {
        if (Blender.AutomaticUpdate)
        {
            await Blender.SyncAll(true);
        }
    }
}

