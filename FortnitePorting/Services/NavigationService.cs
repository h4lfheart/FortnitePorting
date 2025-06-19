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
    public readonly NavigatorContext Setup = new();
    public readonly NavigatorContext Plugin = new();
    public readonly NavigatorContext Settings = new();
    public readonly NavigatorContext ExportSettings = new();
    public readonly NavigatorContext Leaderboard = new();
}

public class NavigatorContext
{
    private NavigationView? NavigationView;
    private Frame ContentFrame = null!;

    private Dictionary<Type, Func<object, Type?>> TypeResolvers = new();

    public void Initialize(NavigationView navigationView)
    {
        NavigationView = navigationView;
        ContentFrame = navigationView.GetLogicalDescendants().OfType<Frame>().First();
    }
    
    public void Initialize(Frame contentFrame)
    {
        ContentFrame = contentFrame;
    }

    public void AddTypeResolver<T>(Func<T, Type?> resolver)
    {
        TypeResolvers[typeof(T)] = obj => resolver.Invoke((T) obj);
    }

    public void Open<T>()
    {
        Open(typeof(T));
    }
    
    public void Open(object? obj)
    {
        if (obj is null) return;

        var viewType = obj switch
        {
            Type type => type,
            _ => TypeResolvers.GetValueOrDefault(obj.GetType())?.Invoke(obj)
        };
        
        if (viewType is null) return;
        
        if (IsTabOpen(viewType)) return;
        
        ContentFrame.Navigate(viewType, null, AppSettings.Application.Transition);

        if (NavigationView is not null)
        {
            NavigationView.SelectedItem = NavigationView.MenuItems
                .Concat(NavigationView.FooterMenuItems)
                .OfType<NavigationViewItem>()
                .FirstOrDefault(item => item.Tag?.Equals(obj) ?? false);
        }
    }

    public bool IsTabOpen<T>()
    {
        return IsTabOpen(typeof(T));
    }
    
    public bool IsTabOpen(object? obj)
    {
        if (obj is null) return false;

        var viewType = obj switch
        {
            Type type => type,
            _ => TypeResolvers.GetValueOrDefault(obj.GetType())?.Invoke(obj)
        };
        
        if (viewType is null) return false;
        
        return ContentFrame.CurrentSourcePageType == viewType;
    }
}