using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Models.Place;

public partial class CanvasPixel : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OffsetMargin))] private ushort _x;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OffsetMargin))] private ushort _y;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OffsetMargin))] private ushort _size = 15;
    [ObservableProperty] private string _name;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Brush))] private Color _color;

    public SolidColorBrush Brush => new(_color);
    public Thickness OffsetMargin => new(X * Size, Y * Size, 0, 0);

}