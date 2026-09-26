using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class ManifestRequest
{
    [JsonProperty("appName")]  public string AppName { get; private set; }
    [JsonProperty("labelName")]  public string LabelName { get; private set; }
    [JsonProperty("versionName")]  public string VersionName { get; private set; }
    [JsonProperty("downloadUrl")] public string DownloadUrl { get; private set; }
}