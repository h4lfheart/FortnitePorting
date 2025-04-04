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
using FortnitePorting.Framework;
using FortnitePorting.Models.TimeWaster;
using FortnitePorting.Models.TimeWaster.Actors;
using FortnitePorting.Models.TimeWaster.Audio;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using ScottPlot.Palettes;
using Serilog;

namespace FortnitePorting.Views;

public partial class TimeWasterView : ViewBase<TimeWasterViewModel>
{
    public TimeWasterView(bool game = true) : base(initializeViewModel: false)
    {
        InitializeComponent();

        KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, handledEventsToo: true);
        AppWM.ToggleVisibility(false);
        ViewModel.IsGame = game;
        TaskService.RunDispatcher(ViewModel.Initialize);
    }
    
    private void OnPointerMove(object? sender, PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(GameGrid);
        var normalizedPoint = point.Position - new Point(GameGrid.Width / 2, GameGrid.Height / 2);
        ViewModel.Player.Target.X = normalizedPoint.X;
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.ShootProjectile();
    }
    
    private List<Key> KonamiKeyPresses = [];
    private List<Key> KonamiSequence = [Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A];

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!KonamiSequence.Contains(e.Key)) return; // im not keylogging you smh
        
        KonamiKeyPresses.Add(e.Key);

        if (!ViewModel.IsGame && KonamiKeyPresses[^Math.Min(KonamiKeyPresses.Count, KonamiSequence.Count)..].SequenceEqual(KonamiSequence))
        {
            ViewModel.IsGame = true;
            await ViewModel.InitializeGame();
            KonamiKeyPresses.Clear();
        }

    }
}