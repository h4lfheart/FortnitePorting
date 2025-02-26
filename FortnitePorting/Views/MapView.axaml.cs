using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.Map;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;
using Microsoft.VisualBasic.Logging;
using Log = Serilog.Log;

namespace FortnitePorting.Views;

public partial class MapView : ViewBase<MapViewModel>
{
    private Point _lastPointerPosition;
    
    public MapView()
    {
        InitializeComponent();
        ViewModel.GridsControl = GridsControl;
    }

    private void OnCellPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not WorldPartitionGrid grid) return;
        
        var currentPoint = e.GetCurrentPoint(this);
        if (!currentPoint.Properties.IsLeftButtonPressed) return;
        
        grid.IsSelected = !grid.IsSelected;

        if (grid.IsSelected)
        {
            ViewModel.SelectedMap.SelectedMaps.AddRange(grid.Maps);
        }
        else
        {
            foreach (var map in grid.Maps)
            {
                ViewModel.SelectedMap.SelectedMaps.Remove(map);
            }
        }
    }

    private async void OnMapPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not WorldPartitionGridMap map) return;

        await map.CopyID();
    }

    private void OnScrollWheel(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not Control control) return;

        var scaleFactor = e.Delta.Y > 0 ? 1.1 : 1.0 / 1.1;
        var position = e.GetPosition(control) - new Point(control.Bounds.Width / 2, control.Bounds.Height / 2);

        var newMatrix = ScaleMatrixAtPoint(ViewModel.MapMatrix, scaleFactor, position.X, position.Y);
        if (newMatrix.ScaleX() <= 1 || newMatrix.ScaleY() <= 1)
        {
            ViewModel.MapMatrix = Matrix.Identity;
        }
        else
        {
            ViewModel.MapMatrix = ClampBounds(newMatrix, control.Bounds.Width, control.Bounds.Height);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control control) return;

        var currentPoint = e.GetCurrentPoint(this);
        if (!currentPoint.Properties.IsRightButtonPressed)
        {
            _lastPointerPosition = currentPoint.Position;
            return;
        }

        var delta = currentPoint.Position - _lastPointerPosition;
        var translatedMatrix = ViewModel.MapMatrix * Matrix.CreateTranslation(delta.X, delta.Y);

        ViewModel.MapMatrix = ClampBounds(translatedMatrix, control.Bounds.Width, control.Bounds.Height);
        _lastPointerPosition = currentPoint.Position;
    }

    private static Matrix ClampBounds(Matrix matrix, double width, double height)
    {
        var scaledWidth = width * matrix.ScaleX();
        var scaledHeight = height * matrix.ScaleY();

        var maxTranslateX = Math.Max(0, (scaledWidth - width) / 2);
        var maxTranslateY = Math.Max(0, (scaledHeight - height) / 2);

        var translationX = matrix.OffsetX();
        var translationY = matrix.OffsetY();

        var clampedTranslateX = scaledWidth > width ? Math.Clamp(translationX, -maxTranslateX, maxTranslateX) : 0;
        var clampedTranslateY = scaledHeight > height ? Math.Clamp(translationY, -maxTranslateY, maxTranslateY) : 0;

        return Matrix.CreateTranslation(clampedTranslateX - translationX, clampedTranslateY - translationY) * matrix;
    }

    private static Matrix ScaleMatrixAtPoint(Matrix matrix, double scale, double centerX, double centerY)
    {
        return Matrix.CreateTranslation(-centerX, -centerY) *
               Matrix.CreateScale(scale, scale) *
               Matrix.CreateTranslation(centerX, centerY) *
               matrix;
    }
}