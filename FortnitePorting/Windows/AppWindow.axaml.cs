using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
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
        //WindowModel.TimeWaster = new TimeWasterView();
    }

    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
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
}