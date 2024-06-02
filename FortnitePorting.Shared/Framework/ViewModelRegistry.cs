namespace FortnitePorting.Shared.Framework;

public class ViewModelRegistry
{
    private static readonly Dictionary<Type, ViewModelBase> Registry = new();

    public static T? Get<T>() where T : ViewModelBase
    {
        var type = typeof(T);
        return Registry.TryGetValue(type, out var value) ? (T) value : null;
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
    
    public static void Reset<T>() where T : ViewModelBase, new()
    {
        Unregister<T>();
        Register<T>();
    }

    public static ViewModelBase[] All()
    {
        return Registry.Values.ToArray();
    }
}