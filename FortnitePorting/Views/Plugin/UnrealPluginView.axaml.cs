using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Plugin;

namespace FortnitePorting.Views.Plugin;

public partial class UnrealPluginView : ViewBase<UnrealPluginViewModel>
{
    public UnrealPluginView() : base(AppSettings.Plugin.Unreal)
    {
        InitializeComponent();
    }
}