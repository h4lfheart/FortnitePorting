using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels;

public partial class PluginViewModel : ViewModelBase
{
    [ObservableProperty] private BlenderPluginViewModel blender = new();
}

