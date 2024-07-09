using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Plugin;

namespace FortnitePorting.Views.Plugin;

public partial class BlenderPluginView : ViewBase<BlenderPluginViewModel>
{
    public BlenderPluginView() : base(AppSettings.Current.Plugin.Blender)
    {
        InitializeComponent();
    }
}