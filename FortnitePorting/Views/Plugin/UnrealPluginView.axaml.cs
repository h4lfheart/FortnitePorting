using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Plugin;

namespace FortnitePorting.Views.Plugin;

public partial class UnrealPluginView : ViewBase<UnrealPluginViewModel>
{
    public UnrealPluginView() : base(AppSettings.Current.Plugin.Unreal)
    {
        InitializeComponent();
    }
}