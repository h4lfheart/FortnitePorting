using System;

namespace FortnitePorting.Extensions;

public static class EnumExtensions
{
    public static Type? SettingsViewType(this EExportLocation location)
    {
        var name = location.ToString();
        var viewName = $"FortnitePorting.Views.Settings.{name}SettingsView";
        
        var type = Type.GetType(viewName);
        return type;
    }
    
    public static Type? PluginViewType(this EExportLocation location)
    {
        var name = location.ToString();
        var viewName = $"FortnitePorting.Views.Plugin.{name}PluginView";
        
        var type = Type.GetType(viewName);
        return type;
    }
}

