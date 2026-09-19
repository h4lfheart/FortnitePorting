using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace FortnitePorting.Models.Nodes;

public abstract partial class BaseNode(string expressionName, bool isEngineNode = true) : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName)), NotifyPropertyChangedFor(nameof(ExpressionDisplayName))] private string _expressionName = expressionName;
    public string ExpressionDisplayName => isEngineNode ? ExpressionName.SubstringBefore("_") : ExpressionName.Replace("_", " ");
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName))] private string _label = expressionName;
    public string DisplayName => Label.Equals(ExpressionName) && isEngineNode ? ExpressionName.Replace(ExpressionPrefix, string.Empty).SubstringBefore("_") : Label;
    
    [ObservableProperty] private Point _location;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BorderBrush))] private bool _isSelected;
    public SolidColorBrush BorderBrush => new(IsSelected ? Color.Parse("#d77601") : Color.Parse("#99121212"));
    
    [ObservableProperty] private ObservableCollection<NodeProperty> _properties = [];

    protected abstract string ExpressionPrefix { get; }
}

public abstract partial class Node(string expressionName = "", bool isExpressionName = true) : BaseNode(expressionName, isExpressionName)
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HeaderBrush))] private Color? _headerColor;
    
    public Brush HeaderBrush => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops = 
        [
            new GradientStop(HeaderColor.Value, 0),
            new GradientStop(new Color(255 / 4, HeaderColor.Value.R, HeaderColor.Value.G, HeaderColor.Value.B), 1),
        ]
    };
    
    [ObservableProperty] private Brush _backgroundBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops = 
        [
            new GradientStop(Color.Parse("#9D111111"), 0),
            new GradientStop(Color.Parse("#82212121"), 1),
        ]
    };
    
    [ObservableProperty] private object? _content;
    [ObservableProperty] private object? _footerContent;

    [ObservableProperty] private ObservableCollection<NodeSocket> _inputs = [];
    [ObservableProperty] private ObservableCollection<NodeSocket> _outputs = [];

    public FPackageIndex? Package;
    public NodeTree? Subgraph;
    public Node? LinkedNode;
    
    public NodeSocket AddInput(NodeSocket socket)
    {
        socket.Parent = this;
        Inputs.Add(socket);
        return socket;
    }
    
    public NodeSocket AddOutput(NodeSocket socket)
    {
        socket.Parent = this;
        Outputs.Add(socket);
        return socket;
    }
    
    public NodeSocket AddInput(string socketName)
    {
        return AddInput(new NodeSocket(socketName));
    }
    
    public NodeSocket AddOutput(string socketName)
    {
        return AddOutput(new NodeSocket(socketName));
    }
    
    public NodeSocket? GetInput(string socketName)
    {
        return Inputs.FirstOrDefault(input => input.Name.Equals(socketName, StringComparison.OrdinalIgnoreCase));
    }
    
    public NodeSocket? GetOutput(string socketName)
    {
        return Outputs.FirstOrDefault(output => output.Name.Equals(socketName, StringComparison.OrdinalIgnoreCase));
    }
}

public partial class NodeComment(string expressionName, bool isExpressionName = true) : BaseNode(expressionName, isExpressionName)
{
    [ObservableProperty] private Size _size;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderBrush))]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    private Color? _commentColor;

    public Brush HeaderBrush => new SolidColorBrush(CommentColor is { } color
        ? new Color(0xC1, color.R, color.G, color.B)
        : Color.Parse("#C1B7B7B7"));
    
    public Brush BackgroundBrush => new SolidColorBrush(CommentColor is { } color
        ? new Color(0x50, color.R, color.G, color.B)
        : Color.Parse("#50B7B7B7"));

    protected override string ExpressionPrefix => string.Empty;
}

public partial class NodeProperty : ObservableObject
{
    [ObservableProperty] private string _key;
    [ObservableProperty] private object _value;
}
