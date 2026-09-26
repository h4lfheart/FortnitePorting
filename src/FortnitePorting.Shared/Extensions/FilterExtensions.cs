namespace FortnitePorting.Shared.Extensions;

public static class FilterExtensions
{
    public static bool Filter(string input, string filter)
    {
        var filters = filter.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return filters.All(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    public static bool FilterAll(string input, IEnumerable<string> filters)
    {
        return filters.All(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    public static bool FilterAny(string input, IEnumerable<string> filters)
    {
        return filters.Any(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
}
