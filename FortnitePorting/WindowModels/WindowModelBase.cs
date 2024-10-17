using System.Threading.Tasks;
using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.WindowModels;

public class WindowModelBase : ViewModelBase
{
    public ThemeSettingsViewModel Theme => AppSettings.Current.Theme;
}