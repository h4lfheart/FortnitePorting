using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels.Settings;

public class RequiresRestartAttribute : Attribute;

public class SettingsViewModelBase : ViewModelBase
{
    private readonly string[] _restartProperties;

    protected SettingsViewModelBase()
    {
        _restartProperties = GetType()
            .GetProperties()
            .Where(prop => prop.GetCustomAttribute<RequiresRestartAttribute>() is not null)
            .Select(prop => prop.Name)
            .ToArray();
    }
    

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        if (_restartProperties.Contains(e.PropertyName))
        {
            AppSettings.NotifyRestartPropertyChanged();
        }
    }
}