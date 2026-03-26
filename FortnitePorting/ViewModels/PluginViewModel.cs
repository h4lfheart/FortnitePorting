using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Plugin;

namespace FortnitePorting.ViewModels;

public partial class PluginViewModel : ViewModelBase
{
    [ObservableProperty] private BlenderPluginViewModel _blender = new();
    [ObservableProperty] private UnrealPluginViewModel _unreal = new();

    public override async Task OnViewOpened()
    {
        AppWM.UpdateChippy([
            "can't wait for the unity plugin that is never releasing!!", "they should add exporting to my cat food bowl",
            "please don’t break everything… again…",
            "i believe in the plugin. and you."
        ]);
    }
}

public enum EPluginStatusType
{
    [Description("Latest")] Newest,

    [Description("Update Available")] UpdateAvailable,

    [Description("Failed to Install")] Failed,

    [Description("Modifying")] Modifying
}