using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Framework.ViewModels;

public partial class ThemedViewModelBase : ViewModelBase
{
    public ObservableCollection<WindowTransparencyLevel> TransparencyLevels => UseMicaBackground ? [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur] : [WindowTransparencyLevel.AcrylicBlur];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TransparencyLevels))] private bool useMicaBackground = true;
    [ObservableProperty] private Color backgroundColor = Color.Parse("#1a0038");
}