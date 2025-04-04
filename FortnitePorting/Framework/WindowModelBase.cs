using Avalonia.Controls;
using FortnitePorting.Application;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Framework;

public class WindowModelBase : ViewModelBase
{
    public ThemeSettingsViewModel Theme => AppSettings.Current.Theme;

    public Window Window;
}