using System;
using System.Collections.Generic;

namespace FortnitePorting.Framework;

public static class ViewModelRegistry
{
    private static readonly Dictionary<Type, ViewModelBase> Registry = new();
    
    public static T? Get<T>() where T : ViewModelBase
    {
        var type = typeof(T);
        if (!Registry.ContainsKey(type)) return null;
        return (T) Registry[type];
    }
    
    public static T Register<T>() where T : ViewModelBase, new()
    {
        var newViewModel = new T();
        Registry[typeof(T)] = newViewModel;
        return newViewModel;
    }
    
    public static bool Unregister<T>() where T : ViewModelBase
    {
        return Registry.Remove(typeof(T));
    }
}