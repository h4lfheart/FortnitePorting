using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Models.Map;


public partial class WorldPartitionGrid : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ToolTipNames))] private List<WorldPartitionGridMap> _maps = [];
    public string ToolTipNames => string.Join("\n", Maps.Select(map => map.Name));
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OffsetMargin))] private FVector _position;
    [ObservableProperty] private FVector _originalPosition;
    public Thickness OffsetMargin => new(Position.X * MapInfo.Scale + MapInfo.XOffset, Position.Y * MapInfo.Scale + MapInfo.YOffset, 0, 0);

    [ObservableProperty] private int _cellSize;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(GridBrush))] private bool _isSelected;

    public SolidColorBrush GridBrush => IsSelected ? SolidColorBrush.Parse("#00CFFF"): SolidColorBrush.Parse("#808080");
    

    public MapInfo MapInfo;

    public WorldPartitionGrid(FVector position, MapInfo mapInfo)
    {
        OriginalPosition = position;

        var rotatedPosition = mapInfo.RotateGrid ? RotateAboutOrigin(new Vector2(position.X, position.Y), Vector2.Zero) : new Vector2(position.X, position.Y);
        Position = new FVector(rotatedPosition.X, rotatedPosition.Y, 0);
        MapInfo = mapInfo;
        CellSize = mapInfo.CellSize;
    }
    
    public Vector2 RotateAboutOrigin(Vector2 point, Vector2 origin)
    {
        return Vector2.Transform(point - origin, Matrix3x2.CreateRotation(-MathF.PI/2f)) + origin;
    } 
    
    
}