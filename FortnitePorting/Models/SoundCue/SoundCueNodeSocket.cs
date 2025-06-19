using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Models.SoundCue;

namespace FortnitePorting.Models.SoundCue;

public partial class SoundCueNodeSocket(string name) : ObservableObject
{
    [ObservableProperty] private string _name = name;
    [ObservableProperty] private Point _anchor;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SocketBrush))] private Color _socketColor = Colors.LightGray;

    public SolidColorBrush SocketBrush => new(SocketColor);

    public SoundCueNode Parent;
}