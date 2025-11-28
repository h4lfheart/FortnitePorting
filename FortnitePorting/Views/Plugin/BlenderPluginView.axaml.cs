using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Plugin;

namespace FortnitePorting.Views.Plugin;

public partial class BlenderPluginView : ViewBase<BlenderPluginViewModel>
{
    public BlenderPluginView() : base(AppSettings.Plugin.Blender)
    {
        InitializeComponent();
    }
}