using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Controls;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views;
using AppWindowModel = FortnitePorting.WindowModels.AppWindowModel;

namespace FortnitePorting.Windows;

public partial class AppWindow : WindowBase<AppWindowModel>
{
    public AppWindow() : base(initializeWindowModel: false)
    {
        InitializeComponent();
        DataContext = WindowModel;
        WindowModel.ContentFrame = ContentFrame;
        WindowModel.NavigationView = NavigationView;
        
        KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, handledEventsToo: true);
        
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var angle = 0f;
        var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Background, (sender, args) =>
        {
            angle += 1;
            
            SpinningGlow.RenderTransform = new TransformGroup
            {
                Children =
                [
                    new ScaleTransform(4, 5),
                    new RotateTransform(angle, 0.5, 0.5)
                ]
            };

            SpinningBurger.RenderTransform = new TransformGroup
            {
                Children =
                [
                    new ScaleTransform(1.2, 1.2),
                    new RotateTransform(angle * -0.6, 0.5, 0.5)
                ]
            };
        });
        
        timer.Start();
    }


    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag.Equals("TimeWaster"))
        {
            WindowModel.TimeWasterOpen = true;
            WindowModel.TimeWaster = new TimeWasterView();
            KonamiKeyPresses.Clear();
            return;
        }
        
        var viewName = $"FortnitePorting.Views.{e.InvokedItemContainer.Tag}View";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        WindowModel.Navigate(type);
    }

    private async void OnUpdatePressed(object? sender, PointerPressedEventArgs e)
    {
        await WindowModel.CheckForUpdate();
    }

    private List<Key> KonamiKeyPresses = [];
    private List<Key> KonamiSequence = [Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A];

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (AppWM is null) return; // happens when focused on other tabs for some reason ?
        if (e.Key == Key.Escape && AppWM.TimeWasterOpen)
        {
            AppWM.ToggleVisibility(true);
            AppWM.TimeWasterOpen = false;
            AppWM.TimeWaster = null;
            KonamiKeyPresses.Clear();
            return;
        }
        
        if (AppWM.TimeWasterOpen) return;
        if (!KonamiSequence.Contains(e.Key)) return; // im not keylogging you smh
        
        KonamiKeyPresses.Add(e.Key);

        if (KonamiKeyPresses[^Math.Min(KonamiKeyPresses.Count, KonamiSequence.Count)..].SequenceEqual(KonamiSequence))
        {
            WindowModel.TimeWasterOpen = true;
            WindowModel.TimeWaster = new TimeWasterView();
            KonamiKeyPresses.Clear();
        }

    }

    private void OnPointerPressedUpperBar(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnSupplyDropPressed(object? sender, PointerPressedEventArgs e)
    {
        WindowModel.SupplyDrop.Open();
    }
}