using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.Services.Endpoints.Models;

public class ReleaseResponse
{
    [J] public string Version;
    [J] public string DownloadURL;
    [J] public string Changelog;
}