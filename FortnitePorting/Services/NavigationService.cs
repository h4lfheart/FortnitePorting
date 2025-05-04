using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;

namespace FortnitePorting.Services;

public class NavigationService : IService
{
    public readonly NavigatorContext App = new();
    public readonly NavigatorContext Plugin = new();
    public readonly NavigatorContext Settings = new();
    public readonly NavigatorContext ExportSettings = new();
}

public class NavigatorContext
{
    private NavigationView? NavigationView;
    private Frame ContentFrame = null!;

    public void Initialize(NavigationView navigationView)
    {
        NavigationView = navigationView;
        ContentFrame = navigationView.GetLogicalDescendants().OfType<Frame>().First();
    }

    public void Open<T>()
    {
        Open(typeof(T));
    }
    
    public void Open(Type? type)
    {
        if (type is null) return;
        if (NavigationView is null) return;
        
        ContentFrame.Navigate(type, null, AppSettings.Application.Transition);

        NavigationView.SelectedItem = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => (Type) item.Tag! == type);
    }
}