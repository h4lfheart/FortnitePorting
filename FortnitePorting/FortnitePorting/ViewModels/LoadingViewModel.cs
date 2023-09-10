using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels;

public partial class LoadingViewModel : ViewModelBase
{
    [ObservableProperty] private string loadingText;
    [ObservableProperty] private float loadingPercentage;
    [ObservableProperty] private LinearGradientBrush loadingOpacityMask = new();

    public LoadingViewModel()
    {
        Update("Loading Application", 0.0f);
    }

    public void Update(string text, float percentage)
    {
        LoadingText = text;
        LoadingPercentage = percentage;

        var pos = 1 - LoadingPercentage;

        LoadingOpacityMask = new LinearGradientBrush // only way to get gradient stop offset to update
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1.0, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Colors.Transparent, pos),
                new(Colors.White, pos)
            }
        };
    }
}