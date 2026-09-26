namespace FortnitePorting.Shared.Extensions;

public static class LazyExtensions
{
    extension<T>(Lazy<T> lazy)
    {
        public T CreateValue()
        {
            return lazy.Value;
        }
    }
}
