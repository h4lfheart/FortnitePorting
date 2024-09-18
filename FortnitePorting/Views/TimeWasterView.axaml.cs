using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using FortnitePorting.Models.TimeWaster;
using FortnitePorting.Models.TimeWaster.Actors;
using FortnitePorting.Models.TimeWaster.Audio;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using ScottPlot.Palettes;
using Serilog;

namespace FortnitePorting.Views;

public partial class TimeWasterView : ViewBase<TimeWasterViewModel>
{
    public TimeWasterView()
    {
        InitializeComponent();
    }
    
    private void OnPointerMove(object? sender, PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(null);
        var normalizedPoint = point.Position - new Point(Bounds.Width / 2, Bounds.Height / 2);
        ViewModel.Player.Target.X = normalizedPoint.X;
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.ShootProjectile();
    }
    
}