using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.ViewModels;

public class WindowModelBase : ViewModelBase
{
    public ThemeSettingsViewModel Theme => AppSettings.Current.Theme;
}