using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Controls.Navigation.Sidebar;

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
        Setup = RegisterContext("Setup");
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

    private NavigatorContext RegisterContext(string name, NavigationTransitionInfo? transitionInfo = null)
    {
        var context = new NavigatorContext(name, transitionInfo);
        _contexts.Add(context);
        return context;
    }
}

public class NavigatorContext(string name, NavigationTransitionInfo? transitionInfo = null)
{
    public string Name = name;
    public NavigationTransitionInfo TransitionInfo = transitionInfo ?? new EntranceNavigationTransitionInfo();
    
    private Sidebar? Sidebar;
    private Frame? ContentFrame;

    private readonly Dictionary<Type, Func<object, Type?>> _typeResolvers = new();
    private readonly Dictionary<Type, Action<object>> _behaviorResolvers = new();

    public void Initialize(Sidebar sidebar, Frame? contentFrame = null)
    {
        Sidebar = sidebar;
        ContentFrame = contentFrame;
        
        AddTypeResolver<string>(name =>
        {
            var targetMenuItem = sidebar.Items.OfType<SidebarItemButton>()
                .FirstOrDefault(item => item.Text.Replace(" ", string.Empty).Equals(name, StringComparison.OrdinalIgnoreCase));
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
            if (ContentFrame is null) return;

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
        
            ContentFrame.Navigate(viewType, null, AppSettings.Application.UseTabTransitions
                ? TransitionInfo
                : new SuppressNavigationTransitionInfo());

            Sidebar?.SelectButton(Sidebar.Items.OfType<SidebarItemButton>().FirstOrDefault(item => item.Tag?.Equals(obj) ?? false));
        });
    }

    public bool IsTabOpen<T>()
    {
        return IsTabOpen(typeof(T));
    }
    
    public bool IsTabOpen(object? obj)
    {
        if (obj is null) return false;
        if (ContentFrame is null) return false;

        var viewType = obj switch
        {
            Type type => type,
            _ => _typeResolvers.GetValueOrDefault(obj.GetType())?.Invoke(obj)
        };
        
        if (viewType is null) return false;

        var isOpen = false;
        TaskService.RunDispatcher(() =>
        {
            isOpen = ContentFrame.CurrentSourcePageType == viewType;
        });

        return isOpen;
    }
}