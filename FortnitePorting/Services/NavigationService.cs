using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using Serilog;

namespace FortnitePorting.Services;

public class NavigationService : IService
{
    public readonly NavigatorContext App;
    public readonly NavigatorContext Setup;
    public readonly NavigatorContext Assets;
    public readonly NavigatorContext Plugin;
    public readonly NavigatorContext Settings;
    public readonly NavigatorContext ExportSettings;
    public readonly NavigatorContext Leaderboard;
    
    private readonly List<NavigatorContext> _contexts = [];

    public NavigationService()
    {
        App = RegisterContext(string.Empty);
        Setup = RegisterContext("Setup"); // TODO do we need content frame route navigation?
        Assets = RegisterContext("Assets");
        Plugin = RegisterContext("Plugin");
        Settings = RegisterContext("Settings");
        ExportSettings = RegisterContext("Export");
        Leaderboard = RegisterContext("Leaderboard");
    }

    public void OpenRoute(string routePath)
    {
        var routes = routePath.Split("/");
        var buildPath = string.Empty;
        foreach (var route in routes)
        {
            var targetContext = _contexts.FirstOrDefault(context => context.Name.Equals(buildPath.SubstringBeforeLast("/"), StringComparison.OrdinalIgnoreCase)) ?? App;
            targetContext.Open(route);

            buildPath += $"{route}/";
        }
    }

    private NavigatorContext RegisterContext(string name)
    {
        var context = new NavigatorContext(name);
        _contexts.Add(context);
        return context;
    }
}

public class NavigatorContext(string name)
{
    public string Name = name;
    private NavigationView? NavigationView;
    private Frame ContentFrame = null!;

    private readonly Dictionary<Type, Func<object, Type?>> _typeResolvers = new();
    private readonly Dictionary<Type, Action<object>> _behaviorResolvers = new();

    public void Initialize(NavigationView navigationView)
    {
        NavigationView = navigationView;
        ContentFrame = navigationView.GetLogicalDescendants().OfType<Frame>().First();
        
        AddTypeResolver<string>(name =>
        {
            var targetMenuItem = NavigationView.MenuItems
                .Concat(NavigationView.FooterMenuItems)
                .OfType<NavigationViewItem>()
                .FirstOrDefault(item => item.Content?.ToString()?.Replace(" ", string.Empty).Equals(name, StringComparison.OrdinalIgnoreCase) ?? false);
            if (targetMenuItem is null) return null;

            if (!targetMenuItem.IsEffectivelyEnabled) return null;
            
            var tag = targetMenuItem.Tag;
            return tag switch
            {
                Type itemType => itemType,
                null => null,
                _ => _typeResolvers.GetValueOrDefault(tag.GetType())?.Invoke(tag)
            };
        });
    }
    
    public void Initialize(Frame contentFrame)
    {
        ContentFrame = contentFrame;
    }

    public void AddTypeResolver<T>(Func<T, Type?> resolver)
    {
        _typeResolvers[typeof(T)] = obj => resolver.Invoke((T) obj);
    }
    
    public void AddBehaviorResolver<T>(Action<T> resolver)
    {
        _behaviorResolvers[typeof(T)] = obj =>
        {
            resolver.Invoke((T) obj);
        };
    }

    public void Open<T>()
    {
        Open(typeof(T));
    }
    
    public void Open(object? obj)
    {
        TaskService.RunDispatcher(() =>
        {
            if (obj is null) return;

            if (_behaviorResolvers.TryGetValue(obj.GetType(), out var behaviorResolver))
            {
                behaviorResolver.Invoke(obj);
                return;
            }

            var viewType = obj switch
            {
                Type type => type,
                _ => _typeResolvers.GetValueOrDefault(obj.GetType())?.Invoke(obj)
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
        });
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
            _ => _typeResolvers.GetValueOrDefault(obj.GetType())?.Invoke(obj)
        };
        
        if (viewType is null) return false;
        
        return ContentFrame.CurrentSourcePageType == viewType;
    }
}