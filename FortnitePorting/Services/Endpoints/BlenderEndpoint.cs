using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public class BlenderEndpoint : EndpointBase
{
    private const string RELEASES_URL = "https://wiki.blender.org/wiki/Reference/Release_Notes";
    
    public BlenderEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<double[]?> GetReleasesAsync()
    {
        var html = await ExecuteAsync(RELEASES_URL);
        if (html.Content is null) return Array.Empty<double>();
        
        var matches = Regex.Matches(html.Content, "Blender [0-9].[0-9]");
        return matches.Select(x =>
        {
            double.TryParse(x.Value.Replace("Blender ", string.Empty), NumberStyles.Any, new NumberFormatInfo { NumberDecimalSeparator = "." }, out var value);
            return value;
        }).Distinct().ToArray();
    }

    public double[]? GetReleases() => GetReleasesAsync().GetAwaiter().GetResult();
}