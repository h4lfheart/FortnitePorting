using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class TimeWasterView : ViewBase<TimeWasterViewModel>
{
    public TimeWasterView(bool game = true) : base(initializeViewModel: false)
    {
        InitializeComponent();

        KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, handledEventsToo: true);
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