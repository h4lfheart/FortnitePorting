using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.Styling;
using FortnitePorting.Models.TimeWaster.Audio;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Validators;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Settings;

public partial class ThemeSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TransparencyHints))]
    private bool _useMica = false;
    public ObservableCollection<WindowTransparencyLevel> TransparencyHints => UseMica ? [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur] : [WindowTransparencyLevel.AcrylicBlur];
    
    [ObservableProperty] private Color _backgroundColor = Color.Parse("#3A2F52");
    [ObservableProperty] private Color _accentColor = Color.Parse("#9B8AFF");
    
    [ObservableProperty] private bool _useWinter = true;
    [ObservableProperty] private bool _useWinterBGM = true;
    
    public bool IsWindows11 => Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build >= 22000;
    

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
       
        switch (e.PropertyName)
        {
            case nameof(AccentColor):
            {
                var faTheme = Avalonia.Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
                faTheme.CustomAccentColor = AccentColor;
                break;
            }
            case nameof(UseWinterBGM):
            {
                if (UseWinterBGM)
                {
                    AudioSystem.Instance.PlaySound("WinterBGM");
                }
                else
                {
                    AudioSystem.Instance.StopSound("WinterBGM");
                }
                
                break;
            }
        }
    }
}