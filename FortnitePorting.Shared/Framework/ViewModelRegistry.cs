using FortnitePorting.Shared.Services;

namespace FortnitePorting.Shared.Framework;

public class ViewModelRegistry
{
    private static readonly Dictionary<Type, ViewModelBase> Registry = new();

    public static T? Get<T>() where T : ViewModelBase
    {
        var type = typeof(T);
        return Registry.TryGetValue(type, out var value) ? (T) value : null;
    }

    public static T New<T>(bool initialize = false) where T : ViewModelBase, new()
    {
        var newViewModel = new T();
        if (initialize) TaskService.Run(newViewModel.Initialize);
        Registry[typeof(T)] = newViewModel;
        return newViewModel;
    }
    
    public static T Register<T>(ViewModelBase existing) where T : ViewModelBase, new()
    {
        Registry[typeof(T)] = existing;
        return (T) existing;
    }

    public static bool Unregister<T>() where T : ViewModelBase
    {
        return Registry.Remove(typeof(T));
    }
    
    public static void Reset<T>() where T : ViewModelBase, new()
    {
        Unregister<T>();
        New<T>();
    }

    public static ViewModelBase[] All()
    {
        return Registry.Values.ToArray();
    }
}