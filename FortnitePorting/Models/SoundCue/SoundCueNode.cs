using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Models.SoundCue;

namespace FortnitePorting.Models.SoundCue;

public partial class SoundCueNodeBase(string expressionName, bool isExpressionName = true) : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName)), NotifyPropertyChangedFor(nameof(ExpressionDisplayName))] private string _expressionName = expressionName;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName))] private string _label = expressionName;
    public string DisplayName => Label.Equals(ExpressionName) && isExpressionName ? Label.Replace("SoundNode", string.Empty).SubstringBefore("_") : Label;
    public string ExpressionDisplayName => isExpressionName ? ExpressionName.SubstringBefore("_") : ExpressionName.Replace("_", " ");
    
    [ObservableProperty] private Point _location;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BorderBrush))] private bool _isSelected;
    public SolidColorBrush BorderBrush => new(IsSelected ? Color.Parse("#d77601") : Color.Parse("#99121212"));
    
    [ObservableProperty] private ObservableCollection<SoundCueNodeProperty> _properties = [];
}

public partial class SoundCueNode(string expressionName = "", bool isExpressionName = true) : SoundCueNodeBase(expressionName, isExpressionName)
{

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HeaderBrush))] private Color _headerColor = Color.Parse("#748394");
    public Brush HeaderBrush => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops = 
        [
            new GradientStop(HeaderColor, 0),
            new GradientStop(new Color(255 / 4, HeaderColor.R, HeaderColor.G, HeaderColor.B), 1),
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

    [ObservableProperty] private ObservableCollection<SoundCueNodeSocket> _inputs = [];
    [ObservableProperty] private ObservableCollection<SoundCueNodeSocket> _outputs = [];

    public FPackageIndex? Package;
    public SoundCueData? Subgraph;
    public SoundCueNode? LinkedNode;
    
    public SoundCueNodeSocket AddInput(SoundCueNodeSocket socket)
    {
        socket.Parent = this;
        Inputs.Add(socket);
        return socket;
    }
    
    public SoundCueNodeSocket AddOutput(SoundCueNodeSocket socket)
    {
        socket.Parent = this;
        Outputs.Add(socket);
        return socket;
    }
    
    public SoundCueNodeSocket AddInput(string socketName)
    {
        return AddInput(new SoundCueNodeSocket(socketName));
    }
    
    public SoundCueNodeSocket AddOutput(string socketName)
    {
        return AddOutput(new SoundCueNodeSocket(socketName));
    }
    
    public SoundCueNodeSocket? GetInput(string socketName)
    {
        return Inputs.FirstOrDefault(input => input.Name.Equals(socketName, StringComparison.OrdinalIgnoreCase));
    }
    
    public SoundCueNodeSocket? GetOutput(string socketName)
    {
        return Outputs.FirstOrDefault(output => output.Name.Equals(socketName, StringComparison.OrdinalIgnoreCase));
    }
}


public partial class SoundCueNodeProperty : ObservableObject
{
    [ObservableProperty] private string _key;
    [ObservableProperty] private object _value;
}

public partial class NodePropertyJsonContainer : ObservableObject
{
    [ObservableProperty] private string _jsonData;
}

