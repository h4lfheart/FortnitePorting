using System.Text.RegularExpressions;

namespace FortnitePorting.Shared.Extensions;

public static class FilterExtensions
{
    private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromMilliseconds(250);

    public static bool RegexFilter(string input, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return true;

        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.None, RegexMatchTimeout);
        }
        catch (ArgumentException)
        {
            return true;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
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
