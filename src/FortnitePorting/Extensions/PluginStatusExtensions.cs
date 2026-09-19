using Avalonia.Media;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Extensions;

public static class PluginStatusExtensions
{
    extension(EPluginStatusType status)
    {
        public SolidColorBrush Brush => status switch
        {
            EPluginStatusType.Newest => SolidColorBrush.Parse("#17854F"),
            EPluginStatusType.UpdateAvailable => SolidColorBrush.Parse("#E0A100"),
            EPluginStatusType.Failed => SolidColorBrush.Parse("#A61717"),
            EPluginStatusType.Modifying => SolidColorBrush.Parse("#6F6F75"),
            _ => SolidColorBrush.Parse("#6F6F75")
        };
    }
}
