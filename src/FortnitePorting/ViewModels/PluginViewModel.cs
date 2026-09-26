using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Plugin;

namespace FortnitePorting.ViewModels;

public partial class PluginViewModel : ViewModelBase
{
    [ObservableProperty] private BlenderPluginViewModel _blender = new();
    [ObservableProperty] private UnrealPluginViewModel _unreal = new();
}

public enum EPluginStatusType
{
    [Description("Latest")]
    Newest,
    
    [Description("Update Available")]
    UpdateAvailable,
    
    [Description("Failed to Install")]
    Failed,
    
    [Description("Modifying")]
    Modifying
}