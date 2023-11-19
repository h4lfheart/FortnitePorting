using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class SettingsView : ViewBase<SettingsViewModel>
{
    public SettingsView() : base(AppSettings.Current)
    {
        InitializeComponent();
    }
}