using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class PluginView : ViewBase<PluginViewModel>
{
    public PluginView() : base(AppSettings.Current.Plugin)
    {
        InitializeComponent();
    }
}