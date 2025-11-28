using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels.Settings;

public partial class ThemeSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TransparencyHints))]
    private bool _useMica = false;
    public ObservableCollection<WindowTransparencyLevel> TransparencyHints => UseMica ? [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur] : [WindowTransparencyLevel.AcrylicBlur];
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BackgroundBrush))] private Color _windowBackgroundColor = Color.Parse("#1C1C26");
    public SolidColorBrush BackgroundBrush => new(new Color(0xDB, WindowBackgroundColor.R, WindowBackgroundColor.G, WindowBackgroundColor.B));
    
    public bool IsWindows11 => Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build >= 22000;
}