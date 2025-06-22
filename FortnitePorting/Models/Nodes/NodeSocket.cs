using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Nodes;

public partial class NodeSocket(string name) : ObservableObject
{
    [ObservableProperty] private string _name = name;
    [ObservableProperty] private Point _anchor;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SocketBrush))] private Color _socketColor = Colors.LightGray;

    public SolidColorBrush SocketBrush => new(SocketColor);

    public BaseNode Parent;
}